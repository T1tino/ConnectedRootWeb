using Microsoft.AspNetCore.Mvc;
using ConnectedRoot.Services;
using ConnectedRoot.Models;
using MongoDB.Driver;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;

namespace ConnectedRoot.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [AllowAnonymous]
    public class SimuladorController : ControllerBase
    {
        private readonly MongoDbService _mongoService;
        private readonly ILogger<SimuladorController> _logger;
        private readonly IConfiguration _configuration;

        // CAMBIO CRÍTICO: Usar CancellationTokenSource en lugar de Timer
        private static readonly Dictionary<string, CancellationTokenSource> _cancellationTokens = new();
        private static readonly Dictionary<string, Task> _simuladorTasks = new();
        private static readonly Dictionary<string, bool> _simuladorEstado = new();
        private static readonly Random _random = new();
        private static readonly object _lock = new();

        public SimuladorController(MongoDbService mongoService, ILogger<SimuladorController> logger, IConfiguration configuration)
        {
            _mongoService = mongoService;
            _logger = logger;
            _configuration = configuration;
        }

        // POST: api/simulador/iniciar
        [HttpPost("iniciar")]
        public async Task<IActionResult> IniciarSimulador([FromBody] IniciarSimuladorRequest request)
        {
            try
            {
                _logger.LogInformation("🚀 Iniciando simulador para sensor: {SensorId}", request.SensorId);

                var sensorId = request.SensorId;

                if (string.IsNullOrEmpty(sensorId))
                {
                    return BadRequest(new { error = "SensorId es requerido", success = false });
                }

                // Verificar que el sensor existe
                var sensor = await _mongoService.Sensores.Find(s => s.Id == sensorId).FirstOrDefaultAsync();
                if (sensor == null)
                {
                    _logger.LogWarning("❌ Sensor no encontrado: {SensorId}", sensorId);
                    return NotFound(new { error = "Sensor no encontrado", success = false, sensorId });
                }

                lock (_lock)
                {
                    // Detener simulador existente si lo hay
                    if (_cancellationTokens.ContainsKey(sensorId))
                    {
                        _logger.LogInformation("🔄 Deteniendo simulador existente para sensor: {SensorId}", sensorId);
                        _cancellationTokens[sensorId].Cancel();
                        _cancellationTokens.Remove(sensorId);
                        if (_simuladorTasks.ContainsKey(sensorId))
                        {
                            _simuladorTasks.Remove(sensorId);
                        }
                    }

                    // Crear nuevo token de cancelación
                    var cts = new CancellationTokenSource();
                    _cancellationTokens[sensorId] = cts;
                    _simuladorEstado[sensorId] = true;

                    // SOLUCIÓN CRÍTICA: Usar Task.Run en lugar de Timer
                    var task = Task.Run(async () => await EjecutarSimulacionContinua(sensorId, cts.Token));
                    _simuladorTasks[sensorId] = task;
                }

                _logger.LogInformation("✅ Simulador iniciado exitosamente para sensor: {SensorId}", sensorId);

                return Ok(new
                {
                    success = true,
                    message = $"Simulador iniciado para sensor {sensorId}",
                    sensorId = sensorId,
                    intervalo = $"{_configuration.GetValue<int>("SimuladorSettings:IntervaloActualizacion", 10)} segundos",
                    timestamp = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error al iniciar simulador para sensor: {SensorId}", request.SensorId);
                return StatusCode(500, new
                {
                    error = $"Error al iniciar simulador: {ex.Message}",
                    success = false,
                    sensorId = request.SensorId
                });
            }
        }

        // NUEVO MÉTODO: Ejecutar simulación continua de forma segura
        private async Task EjecutarSimulacionContinua(string sensorId, CancellationToken cancellationToken)
        {
            var intervaloSegundos = _configuration.GetValue<int>("SimuladorSettings:IntervaloActualizacion", 10);
            _logger.LogInformation("🎯 Iniciando simulación continua para sensor: {SensorId}, intervalo: {Intervalo}s", sensorId, intervaloSegundos);

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        // GENERAR LECTURA INMEDIATAMENTE
                        await GenerarYGuardarLectura(sensorId);
                        
                        // Esperar el intervalo antes de la próxima lectura
                        await Task.Delay(TimeSpan.FromSeconds(intervaloSegundos), cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        _logger.LogInformation("🛑 Simulación cancelada para sensor: {SensorId}", sensorId);
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "❌ Error en ciclo de simulación para sensor: {SensorId}", sensorId);
                        // Continuar después de un breve delay para evitar spam de errores
                        try
                        {
                            await Task.Delay(2000, cancellationToken);
                        }
                        catch (OperationCanceledException)
                        {
                            break;
                        }
                    }
                }
            }
            finally
            {
                lock (_lock)
                {
                    _simuladorEstado[sensorId] = false;
                    _cancellationTokens.Remove(sensorId);
                    _simuladorTasks.Remove(sensorId);
                }
                _logger.LogInformation("🏁 Simulación terminada para sensor: {SensorId}", sensorId);
            }
        }

        // MÉTODO CRÍTICO MEJORADO: Generar y guardar lectura con manejo robusto de errores
        private async Task GenerarYGuardarLectura(string sensorId)
        {
            try
            {
                _logger.LogDebug("📊 Generando lectura para sensor: {SensorId}", sensorId);

                // Verificar que el simulador sigue activo
                if (!_simuladorEstado.ContainsKey(sensorId) || !_simuladorEstado[sensorId])
                {
                    _logger.LogWarning("⚠️ Simulador ya no está activo para sensor: {SensorId}", sensorId);
                    return;
                }

                // Obtener información del sensor CON TIMEOUT
                Sensor sensor;
                try
                {
                    using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                    sensor = await _mongoService.Sensores.Find(s => s.Id == sensorId).FirstOrDefaultAsync(timeoutCts.Token);
                    
                    if (sensor == null)
                    {
                        _logger.LogError("❌ No se pudo encontrar el sensor: {SensorId}", sensorId);
                        return;
                    }
                    
                    _logger.LogDebug("✅ Sensor encontrado: {SensorId}, Tipo: {Tipo}", sensorId, sensor.Tipo);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Error al obtener sensor {SensorId} desde MongoDB", sensorId);
                    return;
                }

                // Generar la lectura
                Lectura lectura = GenerarLecturaPorTipo(sensor, sensorId);
                _logger.LogDebug("📝 Lectura generada: {Tipo} = {Valor}{Unidad}", lectura.Tipo, lectura.Valor, lectura.Unidad);

                // GUARDAR EN BASE DE DATOS CON TIMEOUT Y VERIFICACIÓN
                try
                {
                    using var insertTimeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                    
                    _logger.LogDebug("💾 Intentando guardar lectura en MongoDB...");
                    await _mongoService.Lecturas.InsertOneAsync(lectura, cancellationToken: insertTimeoutCts.Token);
                    
                    _logger.LogInformation("✅ LECTURA GUARDADA EXITOSAMENTE: {Tipo} = {Valor}{Unidad} (Sensor: {SensorId})", 
                        lectura.Tipo, lectura.Valor, lectura.Unidad, sensorId);

                    // VERIFICACIÓN ADICIONAL: Confirmar que se guardó
                    try
                    {
                        var verificacionCount = await _mongoService.Lecturas
                            .CountDocumentsAsync(l => l.SensorId == sensorId && l.FechaHora >= DateTime.Now.AddMinutes(-2));
                        
                        _logger.LogDebug("📈 Total de lecturas recientes para sensor {SensorId}: {Count}", sensorId, verificacionCount);
                    }
                    catch (Exception verEx)
                    {
                        _logger.LogWarning(verEx, "⚠️ No se pudo verificar el guardado, pero la inserción fue exitosa");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ ERROR CRÍTICO: No se pudo guardar la lectura en MongoDB para sensor: {SensorId}. Error: {Error}", sensorId, ex.Message);
                    _logger.LogError("Stack trace: {StackTrace}", ex.StackTrace);
                    
                    // Intentar diagnosticar el problema
                    try
                    {
                        var testConnection = await _mongoService.Sensores.CountDocumentsAsync(FilterDefinition<Sensor>.Empty);
                        _logger.LogError("🔍 Test de conexión: se pueden contar {Count} sensores", testConnection);
                    }
                    catch (Exception connEx)
                    {
                        _logger.LogError(connEx, "❌ ERROR DE CONEXIÓN A MONGODB DETECTADO");
                    }
                    
                    throw; // Re-lanzar para que se maneje en el nivel superior
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ ERROR GENERAL en GenerarYGuardarLectura para sensor: {SensorId}", sensorId);
            }
        }

        private Lectura GenerarLecturaPorTipo(Sensor sensor, string sensorId)
        {
            if (sensor.Tipo.ToLower().Contains("temperatura"))
            {
                return GenerarLecturaTemperatura(sensorId);
            }
            else if (sensor.Tipo.ToLower().Contains("humedad"))
            {
                return GenerarLecturaHumedad(sensorId);
            }
            else
            {
                // Para sensores mixtos, alternar entre tipos
                var tipoAleatorio = _random.Next(2);
                return tipoAleatorio == 0 ? 
                    GenerarLecturaTemperatura(sensorId) : 
                    GenerarLecturaHumedad(sensorId);
            }
        }

        // POST: api/simulador/detener
        [HttpPost("detener")]
        public async Task<IActionResult> DetenerSimulador([FromBody] DetenerSimuladorRequest request)
        {
            try
            {
                _logger.LogInformation("⏹️ Deteniendo simulador para sensor: {SensorId}", request.SensorId);

                var sensorId = request.SensorId;

                if (string.IsNullOrEmpty(sensorId))
                {
                    return BadRequest(new { error = "SensorId es requerido", success = false });
                }

                lock (_lock)
                {
                    if (_cancellationTokens.ContainsKey(sensorId))
                    {
                        _cancellationTokens[sensorId].Cancel();
                        _cancellationTokens.Remove(sensorId);
                    }
                    _simuladorEstado[sensorId] = false;
                }

                // Esperar a que la tarea termine
                if (_simuladorTasks.ContainsKey(sensorId))
                {
                    try
                    {
                        await _simuladorTasks[sensorId].WaitAsync(TimeSpan.FromSeconds(5));
                        _logger.LogInformation("✅ Tarea de simulación terminada correctamente para sensor: {SensorId}", sensorId);
                    }
                    catch (TimeoutException)
                    {
                        _logger.LogWarning("⏰ Timeout esperando que termine la simulación para sensor: {SensorId}", sensorId);
                    }
                    finally
                    {
                        lock (_lock)
                        {
                            _simuladorTasks.Remove(sensorId);
                        }
                    }
                }

                _logger.LogInformation("✅ Simulador detenido exitosamente para sensor: {SensorId}", sensorId);

                return Ok(new
                {
                    success = true,
                    message = $"Simulador detenido para sensor {sensorId}",
                    sensorId = sensorId,
                    timestamp = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error al detener simulador para sensor: {SensorId}", request.SensorId);
                return StatusCode(500, new
                {
                    error = $"Error al detener simulador: {ex.Message}",
                    success = false
                });
            }
        }

        // MÉTODO DE DIAGNÓSTICO: Probar conexión a MongoDB
        [HttpGet("test-conexion")]
        public async Task<IActionResult> TestConexionMongoDB()
        {
            try
            {
                _logger.LogInformation("🔍 Probando conexión a MongoDB...");

                var sensoresCount = await _mongoService.Sensores.CountDocumentsAsync(FilterDefinition<Sensor>.Empty);
                var lecturasCount = await _mongoService.Lecturas.CountDocumentsAsync(FilterDefinition<Lectura>.Empty);

                var resultado = new
                {
                    success = true,
                    sensores = sensoresCount,
                    lecturas = lecturasCount,
                    timestamp = DateTime.Now,
                    mensaje = "Conexión a MongoDB exitosa"
                };

                _logger.LogInformation("✅ Test de conexión exitoso: {Sensores} sensores, {Lecturas} lecturas", 
                    sensoresCount, lecturasCount);

                return Ok(resultado);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ ERROR DE CONEXIÓN A MONGODB");
                return StatusCode(500, new
                {
                    success = false,
                    error = ex.Message,
                    tipo = "ConexionMongoDB",
                    timestamp = DateTime.Now
                });
            }
        }

        // MÉTODO DE DIAGNÓSTICO: Generar una lectura de prueba
        [HttpPost("test-lectura/{sensorId}")]
        public async Task<IActionResult> GenerarLecturaPrueba(string sensorId)
        {
            try
            {
                _logger.LogInformation("🧪 Generando lectura de prueba para sensor: {SensorId}", sensorId);

                var sensor = await _mongoService.Sensores.Find(s => s.Id == sensorId).FirstOrDefaultAsync();
                if (sensor == null)
                {
                    return NotFound(new { error = "Sensor no encontrado", success = false });
                }

                var lectura = GenerarLecturaPorTipo(sensor, sensorId);
                
                await _mongoService.Lecturas.InsertOneAsync(lectura);
                
                _logger.LogInformation("✅ Lectura de prueba guardada exitosamente");

                return Ok(new
                {
                    success = true,
                    lectura = new
                    {
                        lectura.SensorId,
                        lectura.Tipo,
                        lectura.Valor,
                        lectura.Unidad,
                        lectura.FechaHora
                    },
                    mensaje = "Lectura de prueba generada y guardada exitosamente"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error al generar lectura de prueba");
                return StatusCode(500, new
                {
                    success = false,
                    error = ex.Message,
                    sensorId = sensorId
                });
            }
        }

        // Resto de métodos originales (iniciar-todos, detener-todos, estado, etc.)
        [HttpPost("iniciar-todos")]
        public async Task<IActionResult> IniciarTodosLosSimuladores()
        {
            try
            {
                _logger.LogInformation("🚀 Iniciando simulador para todos los sensores activos");

                var sensores = await _mongoService.Sensores
                    .Find(s => s.Estado == "activo")
                    .ToListAsync();

                if (!sensores.Any())
                {
                    return NotFound(new { error = "No se encontraron sensores activos", success = false });
                }

                var resultados = new List<object>();

                foreach (var sensor in sensores.Where(s => !string.IsNullOrEmpty(s.Id)))
                {
                    try
                    {
                        var request = new IniciarSimuladorRequest { SensorId = sensor.Id! };
                        var resultado = await IniciarSimulador(request);
                        resultados.Add(new { sensorId = sensor.Id, success = resultado is OkObjectResult });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error al iniciar simulador para sensor {SensorId}", sensor.Id);
                        resultados.Add(new { sensorId = sensor.Id, success = false, error = ex.Message });
                    }
                }

                return Ok(new
                {
                    success = true,
                    message = "Proceso de inicio completado",
                    resultados = resultados,
                    totalSensores = sensores.Count,
                    exitosos = resultados.Count(r => (bool)r.GetType().GetProperty("success")!.GetValue(r)!)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error al iniciar todos los simuladores");
                return StatusCode(500, new { error = ex.Message, success = false });
            }
        }

        [HttpPost("detener-todos")]
        public IActionResult DetenerTodosLosSimuladores()
        {
            try
            {
                _logger.LogInformation("⏹️ Deteniendo todos los simuladores");

                lock (_lock)
                {
                    foreach (var cts in _cancellationTokens.Values)
                    {
                        cts.Cancel();
                    }
                    _cancellationTokens.Clear();
                    _simuladorTasks.Clear();
                    _simuladorEstado.Clear();
                }

                _logger.LogInformation("✅ Todos los simuladores detenidos");

                return Ok(new
                {
                    success = true,
                    message = "Todos los simuladores han sido detenidos",
                    timestamp = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error al detener todos los simuladores");
                return StatusCode(500, new { error = ex.Message, success = false });
            }
        }

        [HttpGet("estado/{sensorId}")]
        public IActionResult ObtenerEstadoSimulador(string sensorId)
        {
            try
            {
                var activo = _simuladorEstado.ContainsKey(sensorId) && _simuladorEstado[sensorId];
                var tieneTask = _simuladorTasks.ContainsKey(sensorId);

                return Ok(new
                {
                    success = true,
                    sensorId = sensorId,
                    activo = activo,
                    tieneTask = tieneTask,
                    timestamp = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error al obtener estado del simulador para sensor: {SensorId}", sensorId);
                return StatusCode(500, new { error = ex.Message, success = false });
            }
        }

        [HttpGet("sensores")]
        public async Task<IActionResult> ObtenerSensoresDisponibles()
        {
            try
            {
                _logger.LogInformation("📡 Obteniendo sensores disponibles");

                var sensores = await _mongoService.Sensores
                    .Find(s => s.Estado == "activo")
                    .ToListAsync();

                var sensoresConEstado = sensores.Select(s => new
                {
                    Id = s.Id,
                    Tipo = s.Tipo,
                    Modelo = s.Modelo,
                    ZonaId = s.ZonaId,
                    Estado = s.Estado,
                    FechaInstalacion = s.FechaInstalacion,
                    SimuladorActivo = !string.IsNullOrEmpty(s.Id) &&
                                     _simuladorEstado.ContainsKey(s.Id) &&
                                     _simuladorEstado[s.Id]
                }).ToList();

                return Ok(new
                {
                    success = true,
                    sensores = sensoresConEstado,
                    total = sensoresConEstado.Count,
                    activos = sensoresConEstado.Count(s => s.SimuladorActivo),
                    timestamp = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error al obtener sensores disponibles");
                return StatusCode(500, new
                {
                    error = $"Error al obtener sensores: {ex.Message}",
                    success = false
                });
            }
        }

        // Métodos de generación de lecturas (sin cambios)
        private Lectura GenerarLecturaTemperatura(string sensorId)
        {
            double temperatura;
            var random = _random.NextDouble() * 100;

            var rangoOptimo = _configuration.GetSection("SimuladorSettings:RangoTemperatura:Optimo");
            var minOptimo = rangoOptimo.GetValue<double>("Min", 20);
            var maxOptimo = rangoOptimo.GetValue<double>("Max", 23);

            if (random <= 70)
            {
                temperatura = minOptimo + (_random.NextDouble() * (maxOptimo - minOptimo));
            }
            else if (random <= 85)
            {
                temperatura = maxOptimo + (_random.NextDouble() * 4);
            }
            else if (random <= 95)
            {
                temperatura = (minOptimo - 5) + (_random.NextDouble() * 5);
            }
            else if (random <= 98)
            {
                temperatura = 27 + (_random.NextDouble() * 8);
            }
            else
            {
                temperatura = 5 + (_random.NextDouble() * 10);
            }

            return new Lectura
            {
                SensorId = sensorId,
                FechaHora = DateTime.Now,
                Tipo = "temperatura",
                Valor = Math.Round(temperatura, 1),
                Unidad = "°C"
            };
        }

        private Lectura GenerarLecturaHumedad(string sensorId)
        {
            double humedad;
            var random = _random.NextDouble() * 100;

            var rangoOptimo = _configuration.GetSection("SimuladorSettings:RangoHumedad:Optimo");
            var minOptimo = rangoOptimo.GetValue<double>("Min", 40);
            var maxOptimo = rangoOptimo.GetValue<double>("Max", 70);

            if (random <= 60)
            {
                humedad = minOptimo + (_random.NextDouble() * (maxOptimo - minOptimo));
            }
            else if (random <= 80)
            {
                var esBaja = _random.NextDouble() < 0.5;
                humedad = esBaja ?
                    (minOptimo - 10) + (_random.NextDouble() * 10) :
                    maxOptimo + (_random.NextDouble() * 10);
            }
            else if (random <= 95)
            {
                var esBaja = _random.NextDouble() < 0.5;
                humedad = esBaja ?
                    20 + (_random.NextDouble() * 10) :
                    80 + (_random.NextDouble() * 10);
            }
            else
            {
                var esBaja = _random.NextDouble() < 0.5;
                humedad = esBaja ?
                    _random.NextDouble() * 20 :
                    90 + (_random.NextDouble() * 10);
            }

            return new Lectura
            {
                SensorId = sensorId,
                FechaHora = DateTime.Now,
                Tipo = "humedad",
                Valor = Math.Round(humedad, 1),
                Unidad = "%"
            };
        }
    }

    // DTOs
    public class IniciarSimuladorRequest
    {
        public string SensorId { get; set; } = string.Empty;
    }

    public class DetenerSimuladorRequest
    {
        public string SensorId { get; set; } = string.Empty;
    }
}