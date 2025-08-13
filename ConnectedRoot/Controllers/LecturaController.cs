using Microsoft.AspNetCore.Mvc;
using ConnectedRoot.Services;
using ConnectedRoot.Models;
using MongoDB.Driver;
using MongoDB.Bson;
using Microsoft.AspNetCore.Authorization;

namespace ConnectedRoot.Controllers
{
    [ApiController]
    [Route("api/lecturas")] // ✅ CORREGIDO: Cambiar a plural para coincidir con frontend
    [AllowAnonymous] // Permitir acceso sin autenticación para el simulador
    public class LecturaController : ControllerBase
    {
        private readonly MongoDbService _mongoService;
        private readonly ILogger<LecturaController> _logger;

        public LecturaController(MongoDbService mongoService, ILogger<LecturaController> logger)
        {
            _mongoService = mongoService;
            _logger = logger;
        }

        // POST: api/lecturas
        [HttpPost]
        public async Task<IActionResult> CrearLectura([FromBody] Lectura lectura)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { 
                        success = false, 
                        error = "Datos inválidos", 
                        details = ModelState 
                    });
                }

                // Validaciones básicas
                if (string.IsNullOrEmpty(lectura.SensorId))
                {
                    return BadRequest(new { 
                        success = false, 
                        error = "SensorId es requerido" 
                    });
                }

                if (string.IsNullOrEmpty(lectura.Tipo))
                {
                    return BadRequest(new { 
                        success = false, 
                        error = "Tipo es requerido" 
                    });
                }

                // Validar rangos
                if (lectura.Tipo.ToLower().Contains("temperatura"))
                {
                    if (lectura.Valor < -50 || lectura.Valor > 70)
                    {
                        return BadRequest(new { 
                            success = false, 
                            error = "Valor de temperatura fuera de rango válido (-50°C a 70°C)" 
                        });
                    }
                }
                else if (lectura.Tipo.ToLower().Contains("humedad"))
                {
                    if (lectura.Valor < 0 || lectura.Valor > 100)
                    {
                        return BadRequest(new { 
                            success = false, 
                            error = "Valor de humedad fuera de rango válido (0% a 100%)" 
                        });
                    }
                }

                // Establecer fecha si no viene
                if (lectura.FechaHora == default)
                {
                    lectura.FechaHora = DateTime.Now;
                }

                await _mongoService.Lecturas.InsertOneAsync(lectura);
                
                _logger.LogInformation("📊 Lectura guardada: {Tipo} = {Valor}{Unidad} (Sensor: {SensorId})", 
                    lectura.Tipo, lectura.Valor, lectura.Unidad, lectura.SensorId);

                return CreatedAtAction(nameof(ObtenerLectura), new { id = lectura.Id }, new { 
                    success = true, 
                    data = lectura,
                    message = "Lectura guardada exitosamente"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error al guardar lectura");
                return StatusCode(500, new { 
                    success = false, 
                    error = "Error interno del servidor",
                    details = ex.Message 
                });
            }
        }

        // GET: api/lecturas/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> ObtenerLectura(string id)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    return BadRequest(new { success = false, error = "ID requerido" });
                }

                var lectura = await _mongoService.Lecturas.Find(l => l.Id == id).FirstOrDefaultAsync();
                
                if (lectura == null)
                {
                    return NotFound(new { success = false, error = "Lectura no encontrada" });
                }

                return Ok(new { success = true, data = lectura });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error al obtener lectura {Id}", id);
                return StatusCode(500, new { 
                    success = false, 
                    error = "Error interno del servidor" 
                });
            }
        }

        // GET: api/lecturas/ultimas/{sensorId}
        [HttpGet("ultimas/{sensorId}")]
        public async Task<IActionResult> ObtenerUltimasLecturas(string sensorId, [FromQuery] int limit = 10)
        {
            try
            {
                if (string.IsNullOrEmpty(sensorId))
                {
                    return BadRequest(new { success = false, error = "SensorId requerido" });
                }

                // Limitar el número máximo de resultados
                limit = Math.Min(Math.Max(limit, 1), 100);

                var lecturas = await _mongoService.Lecturas
                    .Find(l => l.SensorId == sensorId)
                    .SortByDescending(l => l.FechaHora)
                    .Limit(limit)
                    .ToListAsync();

                _logger.LogDebug("📋 Obtenidas {Count} lecturas para sensor {SensorId}", lecturas.Count, sensorId);

                return Ok(new { 
                    success = true, 
                    data = lecturas,
                    count = lecturas.Count,
                    sensorId = sensorId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error al obtener últimas lecturas para sensor {SensorId}", sensorId);
                return StatusCode(500, new { 
                    success = false, 
                    error = "Error interno del servidor" 
                });
            }
        }

        // GET: api/lecturas
        [HttpGet]
        public async Task<IActionResult> ObtenerLecturas(
            [FromQuery] string? sensorId = null,
            [FromQuery] string? tipo = null,
            [FromQuery] DateTime? fechaInicio = null,
            [FromQuery] DateTime? fechaFin = null,
            [FromQuery] int limit = 50,
            [FromQuery] int page = 1)
        {
            try
            {
                // Construir filtros
                var filterBuilder = Builders<Lectura>.Filter;
                var filtros = new List<FilterDefinition<Lectura>>();

                if (!string.IsNullOrEmpty(sensorId))
                {
                    filtros.Add(filterBuilder.Eq(l => l.SensorId, sensorId));
                }

                if (!string.IsNullOrEmpty(tipo))
                {
                    filtros.Add(filterBuilder.Eq(l => l.Tipo, tipo));
                }

                if (fechaInicio.HasValue || fechaFin.HasValue)
                {
                    var fechaFilter = filterBuilder.Empty;
                    if (fechaInicio.HasValue)
                    {
                        fechaFilter &= filterBuilder.Gte(l => l.FechaHora, fechaInicio.Value);
                    }
                    if (fechaFin.HasValue)
                    {
                        fechaFilter &= filterBuilder.Lte(l => l.FechaHora, fechaFin.Value);
                    }
                    filtros.Add(fechaFilter);
                }

                var filtroFinal = filtros.Count > 0 
                    ? filterBuilder.And(filtros) 
                    : filterBuilder.Empty;

                // Limitar resultados
                limit = Math.Min(Math.Max(limit, 1), 1000);
                var skip = (Math.Max(page, 1) - 1) * limit;

                var lecturas = await _mongoService.Lecturas
                    .Find(filtroFinal)
                    .Sort(Builders<Lectura>.Sort.Descending(l => l.FechaHora))
                    .Skip(skip)
                    .Limit(limit)
                    .ToListAsync();

                var total = await _mongoService.Lecturas.CountDocumentsAsync(filtroFinal);

                return Ok(new { 
                    success = true, 
                    data = lecturas,
                    pagination = new {
                        currentPage = page,
                        totalPages = (int)Math.Ceiling((double)total / limit),
                        totalRecords = total,
                        pageSize = limit
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error al obtener lecturas");
                return StatusCode(500, new { 
                    success = false, 
                    error = "Error interno del servidor" 
                });
            }
        }

        // GET: api/lecturas/estadisticas
        [HttpGet("estadisticas")]
        public async Task<IActionResult> ObtenerEstadisticas([FromQuery] string? sensorId = null)
        {
            try
            {
                var filterBuilder = Builders<Lectura>.Filter;
                var filtro = string.IsNullOrEmpty(sensorId) 
                    ? filterBuilder.Empty 
                    : filterBuilder.Eq(l => l.SensorId, sensorId);

                var estadisticas = await _mongoService.Lecturas.Aggregate()
                    .Match(filtro)
                    .Group(new BsonDocument
                    {
                        { "_id", new BsonDocument { { "sensorId", "$sensorId" }, { "tipo", "$tipo" } } },
                        { "ultimaLectura", new BsonDocument("$max", "$fechaHora") },
                        { "valorPromedio", new BsonDocument("$avg", "$valor") },
                        { "valorMinimo", new BsonDocument("$min", "$valor") },
                        { "valorMaximo", new BsonDocument("$max", "$valor") },
                        { "totalLecturas", new BsonDocument("$sum", 1) }
                    })
                    .Sort(new BsonDocument("ultimaLectura", -1))
                    .ToListAsync();

                return Ok(new { 
                    success = true, 
                    data = estadisticas 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error al obtener estadísticas");
                return StatusCode(500, new { 
                    success = false, 
                    error = "Error interno del servidor" 
                });
            }
        }

        // DELETE: api/lectura/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> EliminarLectura(string id)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    return BadRequest(new { success = false, error = "ID requerido" });
                }

                var resultado = await _mongoService.Lecturas.DeleteOneAsync(l => l.Id == id);
                
                if (resultado.DeletedCount == 0)
                {
                    return NotFound(new { success = false, error = "Lectura no encontrada" });
                }

                _logger.LogInformation("🗑️ Lectura eliminada: {Id}", id);

                return Ok(new { 
                    success = true, 
                    message = "Lectura eliminada exitosamente" 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error al eliminar lectura {Id}", id);
                return StatusCode(500, new { 
                    success = false, 
                    error = "Error interno del servidor" 
                });
            }
        }
    }
}