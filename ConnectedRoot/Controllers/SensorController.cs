using Microsoft.AspNetCore.Mvc;
using ConnectedRoot.Services;
using ConnectedRoot.Models;
using ConnectedRoot.ViewModels;
using MongoDB.Driver;
using MongoDB.Bson;

namespace ConnectedRoot.Controllers
{
    public class SensorController : Controller
    {
        private readonly MongoDbService _mongoService;

        public SensorController(MongoDbService mongoService)
        {
            _mongoService = mongoService;
        }

        // GET: Sensor - MÉTODO ACTUALIZADO CON FILTROS Y PAGINACIÓN
        // GET: Sensor - MÉTODO ACTUALIZADO CON FILTROS Y PAGINACIÓN
public async Task<IActionResult> Index(string? busquedaTexto, string? filtroTipoSensor, 
    string? filtroZona, string? filtroEstado, int pagina = 1)
{
    try
    {
        const int sensoresPorPagina = 12;
        
        // Crear el filtro base
        var filterBuilder = Builders<Sensor>.Filter;
        var filtros = new List<FilterDefinition<Sensor>>();

        // Filtro por texto (tipo o modelo)
        if (!string.IsNullOrWhiteSpace(busquedaTexto))
        {
            var textoFiltro = filterBuilder.Or(
                filterBuilder.Regex(s => s.Tipo, new BsonRegularExpression(busquedaTexto, "i")),
                filterBuilder.Regex(s => s.Modelo, new BsonRegularExpression(busquedaTexto, "i")),
                filterBuilder.Regex(s => s.Descripcion, new BsonRegularExpression(busquedaTexto, "i"))
            );
            filtros.Add(textoFiltro);
        }

        // Filtro por tipo de sensor
        if (!string.IsNullOrWhiteSpace(filtroTipoSensor) && filtroTipoSensor != "Todos")
        {
            filtros.Add(filterBuilder.Regex(s => s.Tipo, new BsonRegularExpression($"^{filtroTipoSensor}$", "i")));
        }

        // Filtro por zona
        if (!string.IsNullOrWhiteSpace(filtroZona) && filtroZona != "Todos")
        {
            filtros.Add(filterBuilder.Eq(s => s.ZonaId, filtroZona));
        }

        // Filtro por estado
        if (!string.IsNullOrWhiteSpace(filtroEstado) && filtroEstado != "Todos")
        {
            string estadoBuscado = filtroEstado switch
            {
                "Activos" => "activo",
                "Inactivos" => "inactivo",
                "Mantenimiento" => "mantenimiento",
                _ => filtroEstado.ToLower()
            };
            filtros.Add(filterBuilder.Regex(s => s.Estado, new BsonRegularExpression($"^{estadoBuscado}$", "i")));
        }

        // Combinar todos los filtros
        var filtroFinal = filtros.Count > 0 
            ? filterBuilder.And(filtros) 
            : filterBuilder.Empty;

        // Contar total de sensores que cumplen el filtro
        var totalSensores = await _mongoService.Sensores.CountDocumentsAsync(filtroFinal);

        // Calcular paginación
        var totalPaginas = (int)Math.Ceiling((double)totalSensores / sensoresPorPagina);
        var sensoresAOmitir = (pagina - 1) * sensoresPorPagina;

        // Obtener sensores de la página actual (ordenar por fecha de instalación)
        var sensores = await _mongoService.Sensores
            .Find(filtroFinal)
            .Sort(Builders<Sensor>.Sort.Descending(s => s.FechaInstalacion))
            .Skip(sensoresAOmitir)
            .Limit(sensoresPorPagina)
            .ToListAsync();

        // Obtener todas las zonas y huertos para hacer el lookup manual
        var zonas = await _mongoService.Zonas.Find(_ => true).ToListAsync();
        var huertos = await _mongoService.Huertos.Find(_ => true).ToListAsync();
        
        // Crear diccionarios para búsquedas rápidas
        var zonasDict = zonas
            .Where(z => !string.IsNullOrEmpty(z.Id))
            .ToDictionary(z => z.Id!, z => z);
        
        var huertosDict = huertos
            .Where(h => !string.IsNullOrEmpty(h.Id))
            .ToDictionary(h => h.Id!, h => h);
        
        var nombresZonas = zonas
            .Where(z => !string.IsNullOrEmpty(z.Id))
            .ToDictionary(z => z.Id!, z => z.NombreZona ?? "Sin nombre");

        var nombresHuertos = huertos
            .Where(h => !string.IsNullOrEmpty(h.Id))
            .ToDictionary(h => h.Id!, h => h.NombreHuerto ?? "Sin nombre");

        var zonasDisponibles = new Dictionary<string, string> { { "Todos", "Todas las zonas" } };
        foreach (var zona in zonas.Where(z => !string.IsNullOrEmpty(z.Id)))
        {
            var nombreHuerto = huertosDict.TryGetValue(zona.HuertoId ?? "", out var huerto) 
                ? huerto.NombreHuerto : "Huerto desconocido";
            zonasDisponibles[zona.Id!] = $"{zona.NombreZona} - {nombreHuerto}";
        }
        
        // Asignar zonas y huertos a cada sensor
        foreach (var sensor in sensores)
        {
            if (!string.IsNullOrEmpty(sensor.ZonaId) && 
                zonasDict.TryGetValue(sensor.ZonaId, out var zona))
            {
                sensor.Zona = zona;
                
                if (!string.IsNullOrEmpty(zona.HuertoId) &&
                    huertosDict.TryGetValue(zona.HuertoId, out var huerto))
                {
                    zona.Huerto = huerto;
                }
            }
        }

        // AGREGAR ESTO: Obtener tipos de sensores únicos de la base de datos
        var tiposUnicos = await _mongoService.Sensores
            .Find(_ => true)
            .Project(s => s.Tipo)
            .ToListAsync();

        var tiposDisponibles = new List<string> { "Todos" };
        if (tiposUnicos.Any())
        {
            tiposDisponibles.AddRange(tiposUnicos.Distinct().Where(t => !string.IsNullOrEmpty(t)).OrderBy(t => t));
        }

        // Crear el ViewModel
        var viewModel = new SensorPaginadoViewModel
        {
            Sensores = sensores,
            PaginaActual = pagina,
            TotalPaginas = totalPaginas,
            TotalSensores = (int)totalSensores,
            SensoresPorPagina = sensoresPorPagina,
            BusquedaTexto = busquedaTexto,
            FiltroTipoSensor = filtroTipoSensor ?? "Todos",
            FiltroZona = filtroZona ?? "Todos",
            FiltroEstado = filtroEstado ?? "Todos",
            ZonasDisponibles = zonasDisponibles,
            NombresZonas = nombresZonas,
            NombresHuertos = nombresHuertos,
            // AGREGAR ESTAS LÍNEAS:
            TiposSensorDisponibles = tiposDisponibles,
            EstadosDisponibles = new List<string>
            {
                "Todos",
                "Activos", 
                "Inactivos",
                "Mantenimiento"
            }
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
        return View(new SensorPaginadoViewModel());
    }
}

        // GET: Sensor/Create
        public async Task<IActionResult> Create(string? zonaId = null)
        {
            // Obtener zonas activas para el dropdown
            var zonas = await _mongoService.Zonas.Find(z => z.Estado == "activa").ToListAsync();
            var huertos = await _mongoService.Huertos.Find(_ => true).ToListAsync();
            
            // Crear lista combinada zona + huerto para mejor UX
            var zonasConHuerto = zonas.Select(z => new
            {
                ZonaId = z.Id,
                ZonaNombre = z.NombreZona,
                HuertoNombre = huertos.FirstOrDefault(h => h.Id == z.HuertoId)?.NombreHuerto ?? "Huerto desconocido",
                Display = $"{z.NombreZona} - {huertos.FirstOrDefault(h => h.Id == z.HuertoId)?.NombreHuerto}"
            }).ToList();

            ViewBag.ZonasConHuerto = zonasConHuerto;
            
            // Si viene de una zona específica, pre-seleccionarla
            if (!string.IsNullOrEmpty(zonaId))
            {
                ViewBag.ZonaSeleccionada = zonaId;
                var zona = zonas.FirstOrDefault(z => z.Id == zonaId);
                ViewBag.NombreZona = zona?.NombreZona;
            }

            return View();
        }

        // POST: Sensor/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Sensor sensor)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    await CargarDatosParaFormulario();
                    return View(sensor);
                }

                // Validar y limpiar los datos antes de guardar
                ValidarSensor(sensor);
                
                await _mongoService.Sensores.InsertOneAsync(sensor);
                TempData["Success"] = "Sensor instalado exitosamente.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Error al instalar sensor: {ex.Message}";
                await CargarDatosParaFormulario();
                return View(sensor);
            }
        }

        // GET: Sensor/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            try
            {
                var sensor = await _mongoService.Sensores.Find(s => s.Id == id).FirstOrDefaultAsync();
                if (sensor == null)
                {
                    TempData["Error"] = "Sensor no encontrado.";
                    return RedirectToAction("Index");
                }

                await CargarDatosParaFormulario();
                return View(sensor);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al obtener sensor: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        // POST: Sensor/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, Sensor sensor)
        {
            if (id != sensor.Id)
            {
                return NotFound();
            }

            try
            {
                if (!ModelState.IsValid)
                {
                    await CargarDatosParaFormulario();
                    return View(sensor);
                }

                // Obtener el sensor actual ANTES de las validaciones
                var sensorActual = await _mongoService.Sensores.Find(s => s.Id == id).FirstOrDefaultAsync();
                if (sensorActual == null)
                {
                    TempData["Error"] = "Sensor no encontrado.";
                    return RedirectToAction("Index");
                }

                ValidarSensor(sensor);

                // Preservar la fecha de instalación original
                sensor.FechaInstalacion = sensorActual.FechaInstalacion;

                var filter = Builders<Sensor>.Filter.Eq(s => s.Id, id);
                await _mongoService.Sensores.ReplaceOneAsync(filter, sensor);
                
                TempData["Success"] = "Sensor actualizado exitosamente.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Error al actualizar sensor: {ex.Message}";
                await CargarDatosParaFormulario();
                return View(sensor);
            }
        }

        // POST: Sensor/ToggleStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            try
            {
                var sensor = await _mongoService.Sensores.Find(s => s.Id == id).FirstOrDefaultAsync();
                if (sensor == null)
                {
                    TempData["Error"] = "Sensor no encontrado.";
                    return RedirectToAction("Index");
                }

                // Cambiar el estado
                sensor.Estado = sensor.Estado == "activo" ? "inactivo" : "activo";

                var filter = Builders<Sensor>.Filter.Eq(s => s.Id, id);
                var update = Builders<Sensor>.Update.Set(s => s.Estado, sensor.Estado);
                
                await _mongoService.Sensores.UpdateOneAsync(filter, update);

                string mensaje = sensor.Estado == "activo" ? "Sensor activado exitosamente." : "Sensor desactivado exitosamente.";
                TempData["Success"] = mensaje;
                
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al cambiar estado del sensor: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        // GET: Sensor/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            try
            {
                var sensor = await _mongoService.Sensores.Find(s => s.Id == id).FirstOrDefaultAsync();
                if (sensor == null)
                {
                    TempData["Error"] = "Sensor no encontrado.";
                    return RedirectToAction("Index");
                }

                // Obtener información de zona y huerto
                var zona = await _mongoService.Zonas.Find(z => z.Id == sensor.ZonaId).FirstOrDefaultAsync();
                var huerto = zona != null ? await _mongoService.Huertos.Find(h => h.Id == zona.HuertoId).FirstOrDefaultAsync() : null;
                
                ViewBag.Zona = zona;
                ViewBag.Huerto = huerto;

                // Obtener responsable del huerto
                Usuario? responsable = null;
                if (huerto != null && !string.IsNullOrEmpty(huerto.ResponsableId))
                {
                    responsable = await _mongoService.Usuarios.Find(u => u.Id == huerto.ResponsableId).FirstOrDefaultAsync();
                }
                ViewBag.Responsable = responsable;

                // Obtener últimas lecturas del sensor
                var lecturas = await _mongoService.Lecturas
                    .Find(l => l.SensorId == id)
                    .SortByDescending(l => l.FechaHora)
                    .Limit(10)
                    .ToListAsync();
                
                ViewBag.UltimasLecturas = lecturas;

                // Obtener estadísticas adicionales
                ViewBag.TotalLecturas = await _mongoService.Lecturas.CountDocumentsAsync(l => l.SensorId == id);
                ViewBag.AlertasPendientes = 0; // Calcular según tus necesidades

                return View(sensor);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al obtener detalles del sensor: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        // GET: Sensor/ByZona/5
        public async Task<IActionResult> ByZona(string id)
        {
            try
            {
                var zona = await _mongoService.Zonas.Find(z => z.Id == id).FirstOrDefaultAsync();
                if (zona == null)
                {
                    return NotFound();
                }

                // Redirigir al Index con filtro de zona
                return RedirectToAction("Index", new { filtroZona = id });
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
            var zonas = await _mongoService.Zonas.Find(z => z.Estado == "activa").ToListAsync();
            var huertos = await _mongoService.Huertos.Find(_ => true).ToListAsync();
            
            var zonasConHuerto = zonas.Select(z => new
            {
                ZonaId = z.Id,
                ZonaNombre = z.NombreZona,
                HuertoNombre = huertos.FirstOrDefault(h => h.Id == z.HuertoId)?.NombreHuerto ?? "Huerto desconocido",
                Display = $"{z.NombreZona} - {huertos.FirstOrDefault(h => h.Id == z.HuertoId)?.NombreHuerto}"
            }).ToList();

            ViewBag.ZonasConHuerto = zonasConHuerto;
        }

        // Método privado para validar y limpiar datos del sensor
        private void ValidarSensor(Sensor sensor)
        {
            sensor.Tipo = string.IsNullOrWhiteSpace(sensor.Tipo) ? string.Empty : sensor.Tipo.Trim();
            sensor.Modelo = string.IsNullOrWhiteSpace(sensor.Modelo) ? string.Empty : sensor.Modelo.Trim();
            sensor.Descripcion = string.IsNullOrWhiteSpace(sensor.Descripcion) ? string.Empty : sensor.Descripcion.Trim();
            sensor.Estado = string.IsNullOrWhiteSpace(sensor.Estado) ? "activo" : sensor.Estado.Trim().ToLower();
            
            // Si es un nuevo sensor (sin Id), establecer fecha de instalación
            if (string.IsNullOrEmpty(sensor.Id))
            {
                sensor.FechaInstalacion = DateTime.Now;
            }
        }
    }
}