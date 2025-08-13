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
    [AllowAnonymous] // ✅ AGREGADO: Permitir acceso sin autenticación en desarrollo
    public class SimuladorController : ControllerBase
    {
        private readonly MongoDbService _mongoService;
        private readonly ILogger<SimuladorController> _logger;
        private readonly IConfiguration _configuration;
        
        // Diccionarios estáticos para mantener el estado entre requests
        private static readonly Dictionary<string, Timer> _timers = new();
        private static readonly Dictionary<string, bool> _simuladorEstado = new();
        private static readonly Random _random = new();
        private static readonly object _lock = new(); // ✅ AGREGADO: Lock para thread safety

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
                
                // ✅ MEJORADO: Validación más robusta
                if (string.IsNullOrEmpty(sensorId))
                {
                    return BadRequest(new { error = "SensorId es requerido", success = false });
                }

                // Verificar que el sensor existe (con mejor manejo de errores)
                var sensor = await _mongoService.Sensores.Find(s => s.Id == sensorId).FirstOrDefaultAsync();
                if (sensor == null)
                {
                    _logger.LogWarning("❌ Sensor no encontrado: {SensorId}", sensorId);
                    return NotFound(new { error = "Sensor no encontrado", success = false, sensorId });
                }

                lock (_lock) // ✅ AGREGADO: Thread safety
                {
                    // Detener timer existente si lo hay
                    if (_timers.ContainsKey(sensorId))
                    {
                        _timers[sensorId].Dispose();
                        _timers.Remove(sensorId);
                        _logger.LogInformation("🔄 Timer existente detenido para sensor: {SensorId}", sensorId);
                    }

                    // Marcar como activo
                    _simuladorEstado[sensorId] = true;

                    // ✅ MEJORADO: Leer intervalo desde configuración
                    var intervaloSegundos = _configuration.GetValue<int>("SimuladorSettings:IntervaloActualizacion", 10);
                    
                    // Crear nuevo timer que ejecute cada X segundos
                    var timer = new Timer(async _ => await GenerarLecturaAutomatica(sensorId), 
                                        null, TimeSpan.Zero, TimeSpan.FromSeconds(intervaloSegundos));
                    
                    _timers[sensorId] = timer;
                }

                _logger.LogInformation("✅ Simulador iniciado exitosamente para sensor: {SensorId}", sensorId);

                return Ok(new { 
                    success = true, 
                    message = $"Simulador iniciado para sensor {sensorId}",
                    sensorId = sensorId,
                    intervalo = $"{_configuration.GetValue<int>("SimuladorSettings:IntervaloActualizacion", 10)} segundos",
                    sensorTipo = sensor.Tipo,
                    timestamp = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error al iniciar simulador para sensor: {SensorId}", request.SensorId);
                return StatusCode(500, new { 
                    error = $"Error al iniciar simulador: {ex.Message}", 
                    success = false,
                    sensorId = request.SensorId 
                });
            }
        }

        // POST: api/simulador/detener
        [HttpPost("detener")]
        public IActionResult DetenerSimulador([FromBody] DetenerSimuladorRequest request)
        {
            try
            {
                _logger.LogInformation("⏹️ Deteniendo simulador para sensor: {SensorId}", request.SensorId);
                
                var sensorId = request.SensorId;

                // ✅ MEJORADO: Validación
                if (string.IsNullOrEmpty(sensorId))
                {
                    return BadRequest(new { error = "SensorId es requerido", success = false });
                }

                lock (_lock) // ✅ AGREGADO: Thread safety
                {
                    if (_timers.ContainsKey(sensorId))
                    {
                        _timers[sensorId].Dispose();
                        _timers.Remove(sensorId);
                    }

                    _simuladorEstado[sensorId] = false;
                }

                _logger.LogInformation("✅ Simulador detenido exitosamente para sensor: {SensorId}", sensorId);

                return Ok(new { 
                    success = true, 
                    message = $"Simulador detenido para sensor {sensorId}",
                    sensorId = sensorId,
                    timestamp = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error al detener simulador para sensor: {SensorId}", request.SensorId);
                return StatusCode(500, new { 
                    error = $"Error al detener simulador: {ex.Message}", 
                    success = false 
                });
            }
        }

        // ✅ NUEVO: POST para iniciar todos los sensores
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

                return Ok(new {
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

        // ✅ NUEVO: POST para detener todos los sensores
        [HttpPost("detener-todos")]
        public IActionResult DetenerTodosLosSimuladores()
        {
            try
            {
                _logger.LogInformation("⏹️ Deteniendo todos los simuladores");

                lock (_lock)
                {
                    foreach (var timer in _timers.Values)
                    {
                        timer.Dispose();
                    }
                    _timers.Clear();
                    _simuladorEstado.Clear();
                }

                _logger.LogInformation("✅ Todos los simuladores detenidos");

                return Ok(new { 
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

        // GET: api/simulador/estado/{sensorId}
        [HttpGet("estado/{sensorId}")]
        public IActionResult ObtenerEstadoSimulador(string sensorId)
        {
            try
            {
                var activo = _simuladorEstado.ContainsKey(sensorId) && _simuladorEstado[sensorId];
                var tieneTimer = _timers.ContainsKey(sensorId);
                
                return Ok(new { 
                    success = true,
                    sensorId = sensorId,
                    activo = activo,
                    tieneTimer = tieneTimer,
                    timestamp = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error al obtener estado del simulador para sensor: {SensorId}", sensorId);
                return StatusCode(500, new { error = ex.Message, success = false });
            }
        }

        // ✅ NUEVO: GET para obtener estado de todos los simuladores
        [HttpGet("estado")]
        public IActionResult ObtenerEstadoTodosLosSimuladores()
        {
            try
            {
                lock (_lock)
                {
                    var estados = _simuladorEstado.Select(kvp => new
                    {
                        sensorId = kvp.Key,
                        activo = kvp.Value,
                        tieneTimer = _timers.ContainsKey(kvp.Key)
                    }).ToList();

                    return Ok(new
                    {
                        success = true,
                        simuladoresActivos = estados.Count(e => e.activo),
                        totalSimuladores = estados.Count,
                        estados = estados,
                        timestamp = DateTime.Now
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error al obtener estado de todos los simuladores");
                return StatusCode(500, new { error = ex.Message, success = false });
            }
        }

        // GET: api/simulador/sensores
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

                return Ok(new { 
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
                return StatusCode(500, new { 
                    error = $"Error al obtener sensores: {ex.Message}", 
                    success = false 
                });
            }
        }

        // Método privado para generar lecturas automáticas
        private async Task GenerarLecturaAutomatica(string sensorId)
        {
            try
            {
                // Verificar si el simulador sigue activo
                if (!_simuladorEstado.ContainsKey(sensorId) || !_simuladorEstado[sensorId])
                {
                    return;
                }

                var sensor = await _mongoService.Sensores.Find(s => s.Id == sensorId).FirstOrDefaultAsync();
                if (sensor == null) return;

                // Generar lectura según el tipo de sensor
                Lectura lectura;
                
                if (sensor.Tipo.ToLower().Contains("temperatura"))
                {
                    lectura = GenerarLecturaTemperatura(sensorId);
                }
                else if (sensor.Tipo.ToLower().Contains("humedad"))
                {
                    lectura = GenerarLecturaHumedad(sensorId);
                }
                else
                {
                    // Generar ambos tipos aleatoriamente para sensores mixtos
                    var tipoAleatorio = _random.Next(2);
                    lectura = tipoAleatorio == 0 ? 
                        GenerarLecturaTemperatura(sensorId) : 
                        GenerarLecturaHumedad(sensorId);
                }

                await _mongoService.Lecturas.InsertOneAsync(lectura);
                
                _logger.LogInformation("📊 [SIMULADOR] Lectura generada: {Tipo} = {Valor}{Unidad} (Sensor: {SensorId})", 
                    lectura.Tipo, lectura.Valor, lectura.Unidad, sensorId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ [SIMULADOR] Error al generar lectura automática para sensor: {SensorId}", sensorId);
            }
        }

        private Lectura GenerarLecturaTemperatura(string sensorId)
        {
            double temperatura;
            var random = _random.NextDouble() * 100;

            // ✅ MEJORADO: Usar configuración desde appsettings
            var rangoOptimo = _configuration.GetSection("SimuladorSettings:RangoTemperatura:Optimo");
            var minOptimo = rangoOptimo.GetValue<double>("Min", 20);
            var maxOptimo = rangoOptimo.GetValue<double>("Max", 23);

            if (random <= 70) // 70% - Rango ideal
            {
                temperatura = minOptimo + (_random.NextDouble() * (maxOptimo - minOptimo));
            }
            else if (random <= 85) // 15% - Rango moderado alto
            {
                temperatura = maxOptimo + (_random.NextDouble() * 4);
            }
            else if (random <= 95) // 10% - Rango moderado bajo
            {
                temperatura = (minOptimo - 5) + (_random.NextDouble() * 5);
            }
            else if (random <= 98) // 3% - Rango extremo alto
            {
                temperatura = 27 + (_random.NextDouble() * 8);
            }
            else // 2% - Rango extremo bajo
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

            // ✅ MEJORADO: Usar configuración desde appsettings
            var rangoOptimo = _configuration.GetSection("SimuladorSettings:RangoHumedad:Optimo");
            var minOptimo = rangoOptimo.GetValue<double>("Min", 40);
            var maxOptimo = rangoOptimo.GetValue<double>("Max", 70);

            if (random <= 60) // 60% - Rango ideal
            {
                humedad = minOptimo + (_random.NextDouble() * (maxOptimo - minOptimo));
            }
            else if (random <= 80) // 20% - Rango moderado
            {
                var esBaja = _random.NextDouble() < 0.5;
                humedad = esBaja ? 
                    (minOptimo - 10) + (_random.NextDouble() * 10) : 
                    maxOptimo + (_random.NextDouble() * 10);
            }
            else if (random <= 95) // 15% - Rango subóptimo
            {
                var esBaja = _random.NextDouble() < 0.5;
                humedad = esBaja ? 
                    20 + (_random.NextDouble() * 10) : 
                    80 + (_random.NextDouble() * 10);
            }
            else // 5% - Rango crítico
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

        // Método para limpiar recursos al cerrar la aplicación
        [HttpPost("limpiar")]
        public IActionResult LimpiarRecursos()
        {
            try
            {
                _logger.LogInformation("🧹 Limpiando recursos del simulador");

                lock (_lock)
                {
                    foreach (var timer in _timers.Values)
                    {
                        timer.Dispose();
                    }
                    _timers.Clear();
                    _simuladorEstado.Clear();
                }

                _logger.LogInformation("✅ Recursos limpiados exitosamente");

                return Ok(new { 
                    success = true, 
                    message = "Recursos limpiados exitosamente",
                    timestamp = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error al limpiar recursos");
                return StatusCode(500, new { error = ex.Message, success = false });
            }
        }
    }

    // DTOs para las requests
    public class IniciarSimuladorRequest
    {
        public string SensorId { get; set; } = string.Empty;
    }

    public class DetenerSimuladorRequest
    {
        public string SensorId { get; set; } = string.Empty;
    }
}