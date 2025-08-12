using Microsoft.AspNetCore.Mvc;
using ConnectedRoot.Services;
using ConnectedRoot.Models;
using ConnectedRoot.ViewModels;
using MongoDB.Driver;
using MongoDB.Bson;

namespace ConnectedRoot.Controllers
{
    public class ZonaController : Controller
    {
        private readonly MongoDbService _mongoService;

        public ZonaController(MongoDbService mongoService)
        {
            _mongoService = mongoService;
        }

        // GET: Zona - MÉTODO ACTUALIZADO CON FILTROS Y PAGINACIÓN
        public async Task<IActionResult> Index(string? busquedaTexto, string? filtroTipoZona, 
            string? filtroHuerto, int pagina = 1)
        {
            try
            {
                const int zonasPorPagina = 12;
                
                // Crear el filtro base
                var filterBuilder = Builders<Zona>.Filter;
                var filtros = new List<FilterDefinition<Zona>>();

                // Filtro por texto (nombre de zona o descripción)
                if (!string.IsNullOrWhiteSpace(busquedaTexto))
                {
                    var textoFiltro = filterBuilder.Or(
                        filterBuilder.Regex(z => z.NombreZona, new BsonRegularExpression(busquedaTexto, "i")),
                        filterBuilder.Regex(z => z.Descripcion, new BsonRegularExpression(busquedaTexto, "i"))
                    );
                    filtros.Add(textoFiltro);
                }

                // Filtro por tipo de zona
                if (!string.IsNullOrWhiteSpace(filtroTipoZona) && filtroTipoZona != "Todos")
                {
                    filtros.Add(filterBuilder.Regex(z => z.TipoZona, new BsonRegularExpression($"^{filtroTipoZona}$", "i")));
                }

                // Filtro por huerto
                if (!string.IsNullOrWhiteSpace(filtroHuerto) && filtroHuerto != "Todos")
                {
                    filtros.Add(filterBuilder.Eq(z => z.HuertoId, filtroHuerto));
                }

                // Combinar todos los filtros
                var filtroFinal = filtros.Count > 0 
                    ? filterBuilder.And(filtros) 
                    : filterBuilder.Empty;

                // Contar total de zonas que cumplen el filtro
                var totalZonas = await _mongoService.Zonas.CountDocumentsAsync(filtroFinal);

                // Calcular paginación
                var totalPaginas = (int)Math.Ceiling((double)totalZonas / zonasPorPagina);
                var zonasAOmitir = (pagina - 1) * zonasPorPagina;

                // Obtener zonas de la página actual (ordenar por nombre en lugar de fecha)
                var zonas = await _mongoService.Zonas
                    .Find(filtroFinal)
                    .Sort(Builders<Zona>.Sort.Ascending(z => z.NombreZona))
                    .Skip(zonasAOmitir)
                    .Limit(zonasPorPagina)
                    .ToListAsync();

                // Obtener todos los huertos para hacer el lookup manual
                var huertos = await _mongoService.Huertos.Find(_ => true).ToListAsync();
                
                // Crear diccionarios para búsquedas rápidas
                var huertosDict = huertos
                    .Where(h => !string.IsNullOrEmpty(h.Id))
                    .ToDictionary(h => h.Id!, h => h);
                
                var nombresHuertos = huertos
                    .Where(h => !string.IsNullOrEmpty(h.Id))
                    .ToDictionary(h => h.Id!, h => h.NombreHuerto ?? "Sin nombre");

                var huertosDisponibles = new Dictionary<string, string> { { "Todos", "Todos los huertos" } };
                foreach (var huerto in huertos.Where(h => !string.IsNullOrEmpty(h.Id)))
                {
                    huertosDisponibles[huerto.Id!] = huerto.NombreHuerto ?? "Sin nombre";
                }
                
                // Asignar huertos a cada zona
                foreach (var zona in zonas)
                {
                    if (!string.IsNullOrEmpty(zona.HuertoId) && 
                        huertosDict.TryGetValue(zona.HuertoId, out var huerto))
                    {
                        zona.Huerto = huerto;
                    }
                }

                // Crear el ViewModel
                var viewModel = new ZonaPaginadoViewModel
                {
                    Zonas = zonas,
                    PaginaActual = pagina,
                    TotalPaginas = totalPaginas,
                    TotalZonas = (int)totalZonas,
                    ZonasPorPagina = zonasPorPagina,
                    BusquedaTexto = busquedaTexto,
                    FiltroTipoZona = filtroTipoZona ?? "Todos",
                    FiltroHuerto = filtroHuerto ?? "Todos",
                    HuertosDisponibles = huertosDisponibles,
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
                return View(new ZonaPaginadoViewModel());
            }
        }

        // GET: Zona/Create
        public async Task<IActionResult> Create(string? huertoId = null)
        {
            // Obtener huertos activos para el dropdown
            var huertos = await _mongoService.Huertos.Find(h => h.Estado == "activo").ToListAsync();
            ViewBag.Huertos = huertos;
            
            // Si viene de un huerto específico, pre-seleccionarlo
            if (!string.IsNullOrEmpty(huertoId))
            {
                ViewBag.HuertoSeleccionado = huertoId;
                var huerto = huertos.FirstOrDefault(h => h.Id == huertoId);
                ViewBag.NombreHuerto = huerto?.NombreHuerto;
            }

            return View();
        }

        // POST: Zona/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Zona zona)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var huertos = await _mongoService.Huertos.Find(h => h.Estado == "activo").ToListAsync();
                    ViewBag.Huertos = huertos;
                    return View(zona);
                }

                // Validar que el nombre de la zona no exista en el mismo huerto
                var existeNombre = await _mongoService.Zonas
                    .Find(z => z.NombreZona.ToLower() == zona.NombreZona.ToLower() && z.HuertoId == zona.HuertoId)
                    .FirstOrDefaultAsync();

                if (existeNombre != null)
                {
                    ModelState.AddModelError("NombreZona", "Ya existe una zona con este nombre en el huerto seleccionado.");
                    var huertos = await _mongoService.Huertos.Find(h => h.Estado == "activo").ToListAsync();
                    ViewBag.Huertos = huertos;
                    return View(zona);
                }

                // Validar y limpiar los datos antes de guardar
                ValidarZona(zona);
                
                await _mongoService.Zonas.InsertOneAsync(zona);
                TempData["Success"] = "Zona creada exitosamente.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Error al crear zona: {ex.Message}";
                var huertos = await _mongoService.Huertos.Find(h => h.Estado == "activo").ToListAsync();
                ViewBag.Huertos = huertos;
                return View(zona);
            }
        }

        // GET: Zona/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            try
            {
                var zona = await _mongoService.Zonas.Find(z => z.Id == id).FirstOrDefaultAsync();
                if (zona == null)
                {
                    TempData["Error"] = "Zona no encontrada.";
                    return RedirectToAction("Index");
                }

                // Obtener huertos activos para el dropdown
                var huertos = await _mongoService.Huertos.Find(h => h.Estado == "activo").ToListAsync();
                ViewBag.Huertos = huertos;
                
                return View(zona);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al obtener zona: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        // POST: Zona/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, Zona zona)
        {
            if (id != zona.Id)
            {
                return NotFound();
            }

            try
            {
                if (!ModelState.IsValid)
                {
                    var huertos = await _mongoService.Huertos.Find(h => h.Estado == "activo").ToListAsync();
                    ViewBag.Huertos = huertos;
                    return View(zona);
                }

                // Obtener la zona actual ANTES de las validaciones
                var zonaActual = await _mongoService.Zonas.Find(z => z.Id == id).FirstOrDefaultAsync();
                if (zonaActual == null)
                {
                    TempData["Error"] = "Zona no encontrada.";
                    return RedirectToAction("Index");
                }

                // Validar que el nombre de la zona no exista en otra zona del mismo huerto
                if (zona.NombreZona.ToLower() != zonaActual.NombreZona.ToLower() || zona.HuertoId != zonaActual.HuertoId)
                {
                    var existeNombre = await _mongoService.Zonas
                        .Find(z => z.NombreZona.ToLower() == zona.NombreZona.ToLower() && 
                                  z.HuertoId == zona.HuertoId && z.Id != id)
                        .FirstOrDefaultAsync();

                    if (existeNombre != null)
                    {
                        ModelState.AddModelError("NombreZona", "Ya existe otra zona con este nombre en el huerto seleccionado.");
                        var huertos = await _mongoService.Huertos.Find(h => h.Estado == "activo").ToListAsync();
                        ViewBag.Huertos = huertos;
                        return View(zona);
                    }
                }

                ValidarZona(zona);

                var filter = Builders<Zona>.Filter.Eq(z => z.Id, id);
                await _mongoService.Zonas.ReplaceOneAsync(filter, zona);
                
                TempData["Success"] = "Zona actualizada exitosamente.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Error al actualizar zona: {ex.Message}";
                var huertos = await _mongoService.Huertos.Find(h => h.Estado == "activo").ToListAsync();
                ViewBag.Huertos = huertos;
                return View(zona);
            }
        }

        // POST: Zona/ToggleStatus/5
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
                var zona = await _mongoService.Zonas.Find(z => z.Id == id).FirstOrDefaultAsync();
                if (zona == null)
                {
                    TempData["Error"] = "Zona no encontrada.";
                    return RedirectToAction("Index");
                }

                // Cambiar el estado
                zona.Estado = zona.Estado == "activa" ? "inactiva" : "activa";

                var filter = Builders<Zona>.Filter.Eq(z => z.Id, id);
                var update = Builders<Zona>.Update.Set(z => z.Estado, zona.Estado);
                
                await _mongoService.Zonas.UpdateOneAsync(filter, update);

                string mensaje = zona.Estado == "activa" ? "Zona activada exitosamente." : "Zona desactivada exitosamente.";
                TempData["Success"] = mensaje;
                
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al cambiar estado de la zona: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        // GET: Zona/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            try
            {
                var zona = await _mongoService.Zonas.Find(z => z.Id == id).FirstOrDefaultAsync();
                if (zona == null)
                {
                    TempData["Error"] = "Zona no encontrada.";
                    return RedirectToAction("Index");
                }

                // Obtener información del huerto
                var huerto = await _mongoService.Huertos.Find(h => h.Id == zona.HuertoId).FirstOrDefaultAsync();
                ViewBag.Huerto = huerto;

                // Obtener responsable del huerto
                Usuario? responsable = null;
                if (huerto != null && !string.IsNullOrEmpty(huerto.ResponsableId))
                {
                    responsable = await _mongoService.Usuarios.Find(u => u.Id == huerto.ResponsableId).FirstOrDefaultAsync();
                }
                ViewBag.Responsable = responsable;

                // Obtener sensores de la zona
                var sensores = await _mongoService.Sensores.Find(s => s.ZonaId == id).ToListAsync();
                ViewBag.Sensores = sensores;

                // Obtener plantas de la zona
                var plantas = await _mongoService.Plantas.Find(p => p.ZonaId == id).ToListAsync();
                ViewBag.Plantas = plantas;

                // Obtener estadísticas adicionales
                ViewBag.SensoresActivos = sensores.Count(s => s.Estado == "activo");
                ViewBag.TotalLecturas = 0; // Calcular según tus necesidades
                ViewBag.AlertasPendientes = 0; // Calcular según tus necesidades

                return View(zona);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al obtener detalles de la zona: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        // GET: Zona/ByHuerto/5
        public async Task<IActionResult> ByHuerto(string id)
        {
            try
            {
                var huerto = await _mongoService.Huertos.Find(h => h.Id == id).FirstOrDefaultAsync();
                if (huerto == null)
                {
                    return NotFound();
                }

                // Redirigir al Index con filtro de huerto
                return RedirectToAction("Index", new { filtroHuerto = id });
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        // Método privado para validar y limpiar datos de la zona
        private void ValidarZona(Zona zona)
        {
            zona.NombreZona = string.IsNullOrWhiteSpace(zona.NombreZona) ? string.Empty : zona.NombreZona.Trim();
            zona.Descripcion = string.IsNullOrWhiteSpace(zona.Descripcion) ? string.Empty : zona.Descripcion.Trim();
            zona.TipoZona = string.IsNullOrWhiteSpace(zona.TipoZona) ? string.Empty : zona.TipoZona.Trim();
            zona.Estado = string.IsNullOrWhiteSpace(zona.Estado) ? "activa" : zona.Estado.Trim().ToLower();

            // Validar coordenadas si existen
            if (zona.Coordenadas != null)
            {
                if (zona.Coordenadas.Latitud == 0 && zona.Coordenadas.Longitud == 0)
                {
                    zona.Coordenadas = null;
                }
            }
        }
    }
}