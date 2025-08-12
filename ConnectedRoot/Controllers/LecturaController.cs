using Microsoft.AspNetCore.Mvc;
using ConnectedRoot.Services;
using ConnectedRoot.Models;
using ConnectedRoot.ViewModels;
using MongoDB.Driver;
using MongoDB.Bson;

namespace ConnectedRoot.Controllers
{
    public class LecturaController : Controller
    {
        private readonly MongoDbService _mongoService;

        public LecturaController(MongoDbService mongoService)
        {
            _mongoService = mongoService;
        }

        // GET: Lectura - MÉTODO ACTUALIZADO CON FILTROS Y PAGINACIÓN
        public async Task<IActionResult> Index(string? busquedaTexto, string? filtroTipo, 
            string? filtroSensor, string? filtroFechaDesde, string? filtroFechaHasta, int pagina = 1)
        {
            try
            {
                const int lecturasPorPagina = 15;
                
                // Crear el filtro base
                var filterBuilder = Builders<Lectura>.Filter;
                var filtros = new List<FilterDefinition<Lectura>>();

                // Filtro por texto (tipo o valor)
                if (!string.IsNullOrWhiteSpace(busquedaTexto))
                {
                    var textoFiltro = filterBuilder.Or(
                        filterBuilder.Regex(l => l.Tipo, new BsonRegularExpression(busquedaTexto, "i")),
                        filterBuilder.Regex(l => l.Unidad, new BsonRegularExpression(busquedaTexto, "i"))
                    );
                    filtros.Add(textoFiltro);
                }

                // Filtro por tipo
                if (!string.IsNullOrWhiteSpace(filtroTipo) && filtroTipo != "Todos")
                {
                    filtros.Add(filterBuilder.Regex(l => l.Tipo, new BsonRegularExpression($"^{filtroTipo}$", "i")));
                }

                // Filtro por sensor
                if (!string.IsNullOrWhiteSpace(filtroSensor) && filtroSensor != "Todos")
                {
                    filtros.Add(filterBuilder.Eq(l => l.SensorId, filtroSensor));
                }

                // Filtro por fecha desde
                if (!string.IsNullOrWhiteSpace(filtroFechaDesde) && DateTime.TryParse(filtroFechaDesde, out var fechaDesde))
                {
                    filtros.Add(filterBuilder.Gte(l => l.FechaHora, fechaDesde));
                }

                // Filtro por fecha hasta
                if (!string.IsNullOrWhiteSpace(filtroFechaHasta) && DateTime.TryParse(filtroFechaHasta, out var fechaHasta))
                {
                    filtros.Add(filterBuilder.Lte(l => l.FechaHora, fechaHasta.AddDays(1).AddSeconds(-1)));
                }

                // Combinar todos los filtros
                var filtroFinal = filtros.Count > 0 
                    ? filterBuilder.And(filtros) 
                    : filterBuilder.Empty;

                // Contar total de lecturas que cumplen el filtro
                var totalLecturas = await _mongoService.Lecturas.CountDocumentsAsync(filtroFinal);

                // Calcular paginación
                var totalPaginas = (int)Math.Ceiling((double)totalLecturas / lecturasPorPagina);
                var lecturasAOmitir = (pagina - 1) * lecturasPorPagina;

                // Obtener lecturas de la página actual
                var lecturas = await _mongoService.Lecturas
                    .Find(filtroFinal)
                    .Sort(Builders<Lectura>.Sort.Descending(l => l.FechaHora))
                    .Skip(lecturasAOmitir)
                    .Limit(lecturasPorPagina)
                    .ToListAsync();

                // Obtener sensores, zonas y huertos para lookup
                var sensores = await _mongoService.Sensores.Find(_ => true).ToListAsync();
                var zonas = await _mongoService.Zonas.Find(_ => true).ToListAsync();
                var huertos = await _mongoService.Huertos.Find(_ => true).ToListAsync();
                
                // Crear diccionarios para búsquedas rápidas
                var sensoresDict = sensores
                    .Where(s => !string.IsNullOrEmpty(s.Id))
                    .ToDictionary(s => s.Id!, s => s);
                
                var zonasDict = zonas
                    .Where(z => !string.IsNullOrEmpty(z.Id))
                    .ToDictionary(z => z.Id!, z => z);
                
                var huertosDict = huertos
                    .Where(h => !string.IsNullOrEmpty(h.Id))
                    .ToDictionary(h => h.Id!, h => h);

                var nombresSensores = sensores
                    .Where(s => !string.IsNullOrEmpty(s.Id))
                    .ToDictionary(s => s.Id!, s => $"{s.Tipo} - {s.Modelo}");

                var nombresZonas = zonas
                    .Where(z => !string.IsNullOrEmpty(z.Id))
                    .ToDictionary(z => z.Id!, z => z.NombreZona ?? "Sin nombre");

                var nombresHuertos = huertos
                    .Where(h => !string.IsNullOrEmpty(h.Id))
                    .ToDictionary(h => h.Id!, h => h.NombreHuerto ?? "Sin nombre");

                var sensoresDisponibles = new Dictionary<string, string> { { "Todos", "Todos los sensores" } };
                foreach (var sensor in sensores.Where(s => !string.IsNullOrEmpty(s.Id)))
                {
                    var zona = zonasDict.TryGetValue(sensor.ZonaId ?? "", out var z) ? z : null;
                    var nombreZona = zona?.NombreZona ?? "Zona desconocida";
                    sensoresDisponibles[sensor.Id!] = $"{sensor.Tipo} - {nombreZona}";
                }

                // Crear el ViewModel
                var viewModel = new LecturaPaginadoViewModel
                {
                    Lecturas = lecturas,
                    PaginaActual = pagina,
                    TotalPaginas = totalPaginas,
                    TotalLecturas = (int)totalLecturas,
                    LecturasPorPagina = lecturasPorPagina,
                    BusquedaTexto = busquedaTexto,
                    FiltroTipo = filtroTipo ?? "Todos",
                    FiltroSensor = filtroSensor ?? "Todos",
                    FiltroFechaDesde = filtroFechaDesde,
                    FiltroFechaHasta = filtroFechaHasta,
                    SensoresDisponibles = sensoresDisponibles,
                    NombresSensores = nombresSensores,
                    NombresZonas = nombresZonas,
                    NombresHuertos = nombresHuertos
                };

                // Mostrar mensaje de éxito si existe
                if (TempData["Success"] != null)
                {
                    ViewBag.Success = TempData["Success"]!.ToString();
                }
                
                return View(viewModel);
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Error de conexión: {ex.Message}";
                return View(new LecturaPaginadoViewModel());
            }
        }

        // GET: Lectura/Create
        public async Task<IActionResult> Create(string? sensorId = null)
        {
            // Obtener sensores activos
            var sensores = await _mongoService.Sensores.Find(s => s.Estado == "activo").ToListAsync();
            var zonas = await _mongoService.Zonas.Find(_ => true).ToListAsync();
            var huertos = await _mongoService.Huertos.Find(_ => true).ToListAsync();

            var sensoresConInfo = sensores.Select(s =>
            {
                var zona = zonas.FirstOrDefault(z => z.Id == s.ZonaId);
                var huerto = zona != null ? huertos.FirstOrDefault(h => h.Id == zona.HuertoId) : null;

                return new
                {
                    SensorId = s.Id,
                    Display = $"{s.Tipo} ({s.Modelo}) - {zona?.NombreZona} - {huerto?.NombreHuerto}",
                    Tipo = s.Tipo
                };
            }).ToList();

            ViewBag.SensoresConInfo = sensoresConInfo;

            // Si viene de un sensor específico
            if (!string.IsNullOrEmpty(sensorId))
            {
                ViewBag.SensorSeleccionado = sensorId;
                var sensor = sensores.FirstOrDefault(s => s.Id == sensorId);
                ViewBag.TipoSensor = sensor?.Tipo;
            }

            return View();
        }

        // POST: Lectura/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Lectura lectura)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    await CargarDatosParaFormulario();
                    return View(lectura);
                }

                // Validar que el sensor existe y está activo
                var sensor = await _mongoService.Sensores.Find(s => s.Id == lectura.SensorId && s.Estado == "activo").FirstOrDefaultAsync();
                if (sensor == null)
                {
                    ModelState.AddModelError("SensorId", "El sensor seleccionado no existe o está inactivo.");
                    await CargarDatosParaFormulario();
                    return View(lectura);
                }

                ValidarLectura(lectura);
                
                await _mongoService.Lecturas.InsertOneAsync(lectura);
                TempData["Success"] = "Lectura registrada exitosamente.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Error al registrar lectura: {ex.Message}";
                await CargarDatosParaFormulario();
                return View(lectura);
            }
        }

        // GET: Lectura/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            try
            {
                var lectura = await _mongoService.Lecturas.Find(l => l.Id == id).FirstOrDefaultAsync();
                if (lectura == null)
                {
                    TempData["Error"] = "Lectura no encontrada.";
                    return RedirectToAction("Index");
                }

                await CargarDatosParaFormulario();
                return View(lectura);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al obtener lectura: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        // POST: Lectura/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, Lectura lectura)
        {
            if (id != lectura.Id)
            {
                return NotFound();
            }

            try
            {
                if (!ModelState.IsValid)
                {
                    await CargarDatosParaFormulario();
                    return View(lectura);
                }

                var lecturaActual = await _mongoService.Lecturas.Find(l => l.Id == id).FirstOrDefaultAsync();
                if (lecturaActual == null)
                {
                    TempData["Error"] = "Lectura no encontrada.";
                    return RedirectToAction("Index");
                }

                ValidarLectura(lectura);

                var filter = Builders<Lectura>.Filter.Eq(l => l.Id, id);
                await _mongoService.Lecturas.ReplaceOneAsync(filter, lectura);
                
                TempData["Success"] = "Lectura actualizada exitosamente.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Error al actualizar lectura: {ex.Message}";
                await CargarDatosParaFormulario();
                return View(lectura);
            }
        }

        // GET: Lectura/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            try
            {
                var lectura = await _mongoService.Lecturas.Find(l => l.Id == id).FirstOrDefaultAsync();
                if (lectura == null)
                {
                    TempData["Error"] = "Lectura no encontrada.";
                    return RedirectToAction("Index");
                }

                // Obtener información del sensor, zona y huerto
                var sensor = await _mongoService.Sensores.Find(s => s.Id == lectura.SensorId).FirstOrDefaultAsync();
                var zona = sensor != null ? await _mongoService.Zonas.Find(z => z.Id == sensor.ZonaId).FirstOrDefaultAsync() : null;
                var huerto = zona != null ? await _mongoService.Huertos.Find(h => h.Id == zona.HuertoId).FirstOrDefaultAsync() : null;
                
                ViewBag.Sensor = sensor;
                ViewBag.Zona = zona;
                ViewBag.Huerto = huerto;

                // Obtener lecturas relacionadas (del mismo sensor, cerca en tiempo)
                var lecturasRelacionadas = await _mongoService.Lecturas
                    .Find(l => l.SensorId == lectura.SensorId && l.Id != lectura.Id)
                    .Sort(Builders<Lectura>.Sort.Descending(l => l.FechaHora))
                    .Limit(5)
                    .ToListAsync();
                
                ViewBag.LecturasRelacionadas = lecturasRelacionadas;

                return View(lectura);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al obtener detalles de la lectura: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        // POST: Lectura/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            try
            {
                var lectura = await _mongoService.Lecturas.Find(l => l.Id == id).FirstOrDefaultAsync();
                if (lectura == null)
                {
                    TempData["Error"] = "Lectura no encontrada.";
                    return RedirectToAction("Index");
                }

                await _mongoService.Lecturas.DeleteOneAsync(l => l.Id == id);
                TempData["Success"] = "Lectura eliminada exitosamente.";
                
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al eliminar lectura: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        // GET: Lectura/BySensor/5
        public async Task<IActionResult> BySensor(string id)
        {
            try
            {
                var sensor = await _mongoService.Sensores.Find(s => s.Id == id).FirstOrDefaultAsync();
                if (sensor == null)
                {
                    return NotFound();
                }

                return RedirectToAction("Index", new { filtroSensor = id });
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        // Método privado para cargar datos para formularios
        private async Task CargarDatosParaFormulario()
        {
            var sensores = await _mongoService.Sensores.Find(s => s.Estado == "activo").ToListAsync();
            var zonas = await _mongoService.Zonas.Find(_ => true).ToListAsync();
            var huertos = await _mongoService.Huertos.Find(_ => true).ToListAsync();

            var sensoresConInfo = sensores.Select(s =>
            {
                var zona = zonas.FirstOrDefault(z => z.Id == s.ZonaId);
                var huerto = zona != null ? huertos.FirstOrDefault(h => h.Id == zona.HuertoId) : null;

                return new
                {
                    SensorId = s.Id,
                    Display = $"{s.Tipo} ({s.Modelo}) - {zona?.NombreZona} - {huerto?.NombreHuerto}",
                    Tipo = s.Tipo
                };
            }).ToList();

            ViewBag.SensoresConInfo = sensoresConInfo;
        }

        // Método privado para validar y limpiar datos de la lectura
        private void ValidarLectura(Lectura lectura)
        {
            lectura.Tipo = string.IsNullOrWhiteSpace(lectura.Tipo) ? string.Empty : lectura.Tipo.Trim();
            lectura.Unidad = string.IsNullOrWhiteSpace(lectura.Unidad) ? string.Empty : lectura.Unidad.Trim();
            
            // Validar rango de valores según el tipo
            if (lectura.Tipo.ToLower().Contains("temperatura"))
            {
                if (lectura.Valor < -50 || lectura.Valor > 100)
                {
                    throw new ArgumentException("Valor de temperatura fuera de rango válido (-50°C a 100°C)");
                }
            }
            else if (lectura.Tipo.ToLower().Contains("humedad"))
            {
                if (lectura.Valor < 0 || lectura.Valor > 100)
                {
                    throw new ArgumentException("Valor de humedad fuera de rango válido (0% a 100%)");
                }
            }
        }
    }
}