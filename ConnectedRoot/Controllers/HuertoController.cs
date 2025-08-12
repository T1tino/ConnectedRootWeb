using Microsoft.AspNetCore.Mvc;
using ConnectedRoot.Services;
using ConnectedRoot.Models;
using ConnectedRoot.ViewModels;
using MongoDB.Driver;
using MongoDB.Bson;

namespace ConnectedRoot.Controllers
{
    public class HuertoController : Controller
    {
        private readonly MongoDbService _mongoService;

        public HuertoController(MongoDbService mongoService)
        {
            _mongoService = mongoService;
        }

        // GET: Huerto - MÉTODO ACTUALIZADO CON FILTROS Y PAGINACIÓN
        public async Task<IActionResult> Index(string? busquedaTexto, string? filtroEstado, 
            string? filtroFechaDesde, string? filtroFechaHasta, int pagina = 1)
        {
            try
            {
                const int huertosPorPagina = 12;
                
                // Obtener todos los huertos
                var filterBuilder = Builders<Huerto>.Filter;
                var filtros = new List<FilterDefinition<Huerto>>();

                // Filtro por texto (nombre del huerto o ubicación)
                if (!string.IsNullOrWhiteSpace(busquedaTexto))
                {
                    var textoFiltro = filterBuilder.Or(
                        filterBuilder.Regex(h => h.NombreHuerto, new BsonRegularExpression(busquedaTexto, "i")),
                        filterBuilder.Regex(h => h.Ubicacion, new BsonRegularExpression(busquedaTexto, "i"))
                    );
                    filtros.Add(textoFiltro);
                }

                // Filtro por estado
                if (!string.IsNullOrWhiteSpace(filtroEstado) && filtroEstado != "Todos")
                {
                    string estadoBuscado = filtroEstado == "Activos" ? "activo" : "inactivo";
                    filtros.Add(filterBuilder.Regex(h => h.Estado, new BsonRegularExpression($"^{estadoBuscado}$", "i")));
                }

                // Filtro por fecha desde
                if (!string.IsNullOrWhiteSpace(filtroFechaDesde) && DateTime.TryParse(filtroFechaDesde, out var fechaDesde))
                {
                    filtros.Add(filterBuilder.Gte(h => h.FechaRegistro, fechaDesde.Date));
                }

                // Filtro por fecha hasta
                if (!string.IsNullOrWhiteSpace(filtroFechaHasta) && DateTime.TryParse(filtroFechaHasta, out var fechaHasta))
                {
                    filtros.Add(filterBuilder.Lte(h => h.FechaRegistro, fechaHasta.Date.AddDays(1).AddSeconds(-1)));
                }

                // Combinar todos los filtros
                var filtroFinal = filtros.Count > 0 
                    ? filterBuilder.And(filtros) 
                    : filterBuilder.Empty;

                // Contar total de huertos que cumplen el filtro
                var totalHuertos = await _mongoService.Huertos.CountDocumentsAsync(filtroFinal);

                // Calcular paginación
                var totalPaginas = (int)Math.Ceiling((double)totalHuertos / huertosPorPagina);
                var huertosAOmitir = (pagina - 1) * huertosPorPagina;

                // Obtener huertos de la página actual
                var huertos = await _mongoService.Huertos
                    .Find(filtroFinal)
                    .Sort(Builders<Huerto>.Sort.Descending(h => h.FechaRegistro))
                    .Skip(huertosAOmitir)
                    .Limit(huertosPorPagina)
                    .ToListAsync();

                // Obtener todos los usuarios para hacer el lookup manual
                var usuarios = await _mongoService.Usuarios.Find(_ => true).ToListAsync();
                
                // Crear un diccionario para búsquedas rápidas (filtrar usuarios con Id no nulo)
                var usuariosDict = usuarios
                    .Where(u => !string.IsNullOrEmpty(u.Id))
                    .ToDictionary(u => u.Id!, u => u);
                
                // Asignar responsables a cada huerto
                foreach (var huerto in huertos)
                {
                    if (!string.IsNullOrEmpty(huerto.ResponsableId) && 
                        usuariosDict.TryGetValue(huerto.ResponsableId, out var responsable))
                    {
                        huerto.Responsable = responsable;
                    }
                }

                // Crear el ViewModel
                var viewModel = new HuertoPaginadoViewModel
                {
                    Huertos = huertos,
                    PaginaActual = pagina,
                    TotalPaginas = totalPaginas,
                    TotalHuertos = (int)totalHuertos,
                    HuertosPorPagina = huertosPorPagina,
                    BusquedaTexto = busquedaTexto,
                    FiltroEstado = filtroEstado ?? "Todos",
                    FiltroFechaDesde = filtroFechaDesde,
                    FiltroFechaHasta = filtroFechaHasta
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
                return View(new HuertoPaginadoViewModel());
            }
        }

        // GET: Huerto/Create
        public async Task<IActionResult> Create()
        {
            // Obtener usuarios activos para el dropdown de responsables
            var usuarios = await _mongoService.Usuarios.Find(u => u.Activo == true).ToListAsync();
            ViewBag.Usuarios = usuarios;
            return View();
        }

        // POST: Huerto/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Huerto huerto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var usuarios = await _mongoService.Usuarios.Find(u => u.Activo == true).ToListAsync();
                    ViewBag.Usuarios = usuarios;
                    return View(huerto);
                }

                // Validar que el nombre del huerto no exista
                var existeNombre = await _mongoService.Huertos
                    .Find(h => h.NombreHuerto.ToLower() == huerto.NombreHuerto.ToLower())
                    .FirstOrDefaultAsync();

                if (existeNombre != null)
                {
                    ModelState.AddModelError("NombreHuerto", "Ya existe un huerto con este nombre.");
                    var usuarios = await _mongoService.Usuarios.Find(u => u.Activo == true).ToListAsync();
                    ViewBag.Usuarios = usuarios;
                    return View(huerto);
                }

                // Validar y limpiar los datos antes de guardar
                ValidarHuerto(huerto);
                
                await _mongoService.Huertos.InsertOneAsync(huerto);
                TempData["Success"] = "Huerto creado exitosamente.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Error al crear huerto: {ex.Message}";
                var usuarios = await _mongoService.Usuarios.Find(u => u.Activo == true).ToListAsync();
                ViewBag.Usuarios = usuarios;
                return View(huerto);
            }
        }

        // GET: Huerto/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            try
            {
                var huerto = await _mongoService.Huertos.Find(h => h.Id == id).FirstOrDefaultAsync();
                if (huerto == null)
                {
                    TempData["Error"] = "Huerto no encontrado.";
                    return RedirectToAction("Index");
                }

                // Obtener usuarios activos para el dropdown de responsables
                var usuarios = await _mongoService.Usuarios.Find(u => u.Activo == true).ToListAsync();
                ViewBag.Usuarios = usuarios;
                
                return View(huerto);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al obtener huerto: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        // POST: Huerto/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, Huerto huerto)
        {
            if (id != huerto.Id)
            {
                return NotFound();
            }

            try
            {
                if (!ModelState.IsValid)
                {
                    var usuarios = await _mongoService.Usuarios.Find(u => u.Activo == true).ToListAsync();
                    ViewBag.Usuarios = usuarios;
                    return View(huerto);
                }

                // Obtener el huerto actual ANTES de las validaciones
                var huertoActual = await _mongoService.Huertos.Find(h => h.Id == id).FirstOrDefaultAsync();
                if (huertoActual == null)
                {
                    TempData["Error"] = "Huerto no encontrado.";
                    return RedirectToAction("Index");
                }

                // Validar que el nombre del huerto no exista en otro huerto (solo si el nombre cambió)
                if (huerto.NombreHuerto.ToLower() != huertoActual.NombreHuerto.ToLower())
                {
                    var existeNombre = await _mongoService.Huertos
                        .Find(h => h.NombreHuerto.ToLower() == huerto.NombreHuerto.ToLower() && h.Id != id)
                        .FirstOrDefaultAsync();

                    if (existeNombre != null)
                    {
                        ModelState.AddModelError("NombreHuerto", "Ya existe otro huerto con este nombre.");
                        var usuarios = await _mongoService.Usuarios.Find(u => u.Activo == true).ToListAsync();
                        ViewBag.Usuarios = usuarios;
                        return View(huerto);
                    }
                }

                ValidarHuerto(huerto);

                // Preservar la fecha de registro original
                huerto.FechaRegistro = huertoActual.FechaRegistro;

                var filter = Builders<Huerto>.Filter.Eq(h => h.Id, id);
                await _mongoService.Huertos.ReplaceOneAsync(filter, huerto);
                
                TempData["Success"] = "Huerto actualizado exitosamente.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Error al actualizar huerto: {ex.Message}";
                var usuarios = await _mongoService.Usuarios.Find(u => u.Activo == true).ToListAsync();
                ViewBag.Usuarios = usuarios;
                return View(huerto);
            }
        }

        // POST: Huerto/ToggleStatus/5
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
                var huerto = await _mongoService.Huertos.Find(h => h.Id == id).FirstOrDefaultAsync();
                if (huerto == null)
                {
                    TempData["Error"] = "Huerto no encontrado.";
                    return RedirectToAction("Index");
                }

                // Cambiar el estado
                huerto.Estado = huerto.Estado == "activo" ? "inactivo" : "activo";

                var filter = Builders<Huerto>.Filter.Eq(h => h.Id, id);
                var update = Builders<Huerto>.Update.Set(h => h.Estado, huerto.Estado);
                
                await _mongoService.Huertos.UpdateOneAsync(filter, update);

                string mensaje = huerto.Estado == "activo" ? "Huerto activado exitosamente." : "Huerto desactivado exitosamente.";
                TempData["Success"] = mensaje;
                
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al cambiar estado del huerto: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        // GET: Huerto/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            try
            {
                var huerto = await _mongoService.Huertos.Find(h => h.Id == id).FirstOrDefaultAsync();
                if (huerto == null)
                {
                    TempData["Error"] = "Huerto no encontrado.";
                    return RedirectToAction("Index");
                }

                // Obtener información del responsable
                Usuario? responsable = null;
                if (!string.IsNullOrEmpty(huerto.ResponsableId))
                {
                    responsable = await _mongoService.Usuarios.Find(u => u.Id == huerto.ResponsableId).FirstOrDefaultAsync();
                }
                ViewBag.Responsable = responsable;

                // Obtener zonas del huerto
                var zonas = await _mongoService.Zonas.Find(z => z.HuertoId == id).ToListAsync();
                ViewBag.Zonas = zonas;

                // Obtener sensores del huerto (a través de las zonas)
                var sensores = new List<Sensor>();
                if (zonas.Any())
                {
                    var zonasIds = zonas.Select(z => z.Id).ToList();
                    sensores = await _mongoService.Sensores
                        .Find(s => zonasIds.Contains(s.ZonaId))
                        .ToListAsync();
                }
                ViewBag.Sensores = sensores;

                // Obtener plantas del huerto (a través de las zonas)
                var plantas = new List<Planta>();
                if (zonas.Any())
                {
                    var zonasIds = zonas.Select(z => z.Id).ToList();
                    plantas = await _mongoService.Plantas
                        .Find(p => zonasIds.Contains(p.ZonaId))
                        .ToListAsync();
                }
                ViewBag.Plantas = plantas;

                // Obtener estadísticas adicionales si las necesitas
                ViewBag.TotalLecturas = 0; // Aquí puedes calcular las lecturas totales
                ViewBag.AlertasPendientes = 0; // Aquí puedes calcular alertas pendientes

                return View(huerto);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al obtener detalles del huerto: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        // Método privado para validar y limpiar datos del huerto
        private void ValidarHuerto(Huerto huerto)
        {
            huerto.NombreHuerto = string.IsNullOrWhiteSpace(huerto.NombreHuerto) ? string.Empty : huerto.NombreHuerto.Trim();
            huerto.Ubicacion = string.IsNullOrWhiteSpace(huerto.Ubicacion) ? string.Empty : huerto.Ubicacion.Trim();
            huerto.Estado = string.IsNullOrWhiteSpace(huerto.Estado) ? "activo" : huerto.Estado.Trim().ToLower();
            
            // Si es un nuevo huerto (sin Id), establecer fecha de registro
            if (string.IsNullOrEmpty(huerto.Id))
            {
                huerto.FechaRegistro = DateTime.Now;
            }
        }
    }
}