using Microsoft.AspNetCore.Mvc;
using ConnectedRoot.Services;
using ConnectedRoot.Models;
using ConnectedRoot.ViewModels;
using MongoDB.Driver;
using MongoDB.Bson;

namespace ConnectedRoot.Controllers
{
    public class AlertaController : Controller
    {
        private readonly MongoDbService _mongoService;

        public AlertaController(MongoDbService mongoService)
        {
            _mongoService = mongoService;
        }

        // GET: Alerta - MÉTODO ACTUALIZADO CON FILTROS Y PAGINACIÓN
        public async Task<IActionResult> Index(string? busquedaTexto, string? filtroTipo, 
            string? filtroEstado, string? filtroZona, string? filtroFechaDesde, string? filtroFechaHasta, int pagina = 1)
        {
            try
            {
                const int alertasPorPagina = 12;
                
                // Crear el filtro base
                var filterBuilder = Builders<Alerta>.Filter;
                var filtros = new List<FilterDefinition<Alerta>>();

                // Filtro por texto (tipo o mensaje)
                if (!string.IsNullOrWhiteSpace(busquedaTexto))
                {
                    var textoFiltro = filterBuilder.Or(
                        filterBuilder.Regex(a => a.Tipo, new BsonRegularExpression(busquedaTexto, "i")),
                        filterBuilder.Regex(a => a.Mensaje, new BsonRegularExpression(busquedaTexto, "i"))
                    );
                    filtros.Add(textoFiltro);
                }

                // Filtro por tipo
                if (!string.IsNullOrWhiteSpace(filtroTipo) && filtroTipo != "Todos")
                {
                    filtros.Add(filterBuilder.Regex(a => a.Tipo, new BsonRegularExpression($"^{filtroTipo}$", "i")));
                }

                // Filtro por estado
                if (!string.IsNullOrWhiteSpace(filtroEstado) && filtroEstado != "Todos")
                {
                    string estadoBuscado = filtroEstado switch
                    {
                        "Pendientes" => "pendiente",
                        "Resueltas" => "resuelta",
                        _ => filtroEstado.ToLower()
                    };
                    filtros.Add(filterBuilder.Regex(a => a.Estado, new BsonRegularExpression($"^{estadoBuscado}$", "i")));
                }

                // Filtro por zona
                if (!string.IsNullOrWhiteSpace(filtroZona) && filtroZona != "Todos")
                {
                    filtros.Add(filterBuilder.Eq(a => a.ZonaId, filtroZona));
                }

                // Filtro por fecha desde
                if (!string.IsNullOrWhiteSpace(filtroFechaDesde) && DateTime.TryParse(filtroFechaDesde, out var fechaDesde))
                {
                    filtros.Add(filterBuilder.Gte(a => a.FechaHora, fechaDesde));
                }

                // Filtro por fecha hasta
                if (!string.IsNullOrWhiteSpace(filtroFechaHasta) && DateTime.TryParse(filtroFechaHasta, out var fechaHasta))
                {
                    filtros.Add(filterBuilder.Lte(a => a.FechaHora, fechaHasta.AddDays(1).AddSeconds(-1)));
                }

                // Combinar todos los filtros
                var filtroFinal = filtros.Count > 0 
                    ? filterBuilder.And(filtros) 
                    : filterBuilder.Empty;

                // Contar total de alertas que cumplen el filtro
                var totalAlertas = await _mongoService.Alertas.CountDocumentsAsync(filtroFinal);

                // Calcular paginación
                var totalPaginas = (int)Math.Ceiling((double)totalAlertas / alertasPorPagina);
                var alertasAOmitir = (pagina - 1) * alertasPorPagina;

                // Obtener alertas de la página actual
                var alertas = await _mongoService.Alertas
                    .Find(filtroFinal)
                    .Sort(Builders<Alerta>.Sort.Descending(a => a.FechaHora))
                    .Skip(alertasAOmitir)
                    .Limit(alertasPorPagina)
                    .ToListAsync();

                // Obtener zonas y huertos para lookup
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

                // Crear el ViewModel
                var viewModel = new AlertaPaginadoViewModel
                {
                    Alertas = alertas,
                    PaginaActual = pagina,
                    TotalPaginas = totalPaginas,
                    TotalAlertas = (int)totalAlertas,
                    AlertasPorPagina = alertasPorPagina,
                    BusquedaTexto = busquedaTexto,
                    FiltroTipo = filtroTipo ?? "Todos",
                    FiltroEstado = filtroEstado ?? "Todos",
                    FiltroZona = filtroZona ?? "Todos",
                    FiltroFechaDesde = filtroFechaDesde,
                    FiltroFechaHasta = filtroFechaHasta,
                    ZonasDisponibles = zonasDisponibles,
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
                return View(new AlertaPaginadoViewModel());
            }
        }

        // GET: Alerta/Create
        public async Task<IActionResult> Create(string? zonaId = null)
        {
            // Obtener zonas para el dropdown
            var zonas = await _mongoService.Zonas.Find(_ => true).ToListAsync();
            var huertos = await _mongoService.Huertos.Find(_ => true).ToListAsync();

            var zonasConHuerto = zonas.Select(z => new
            {
                ZonaId = z.Id,
                ZonaNombre = z.NombreZona,
                HuertoNombre = huertos.FirstOrDefault(h => h.Id == z.HuertoId)?.NombreHuerto ?? "Huerto desconocido",
                Display = $"{z.NombreZona} - {huertos.FirstOrDefault(h => h.Id == z.HuertoId)?.NombreHuerto}"
            }).ToList();

            ViewBag.ZonasConHuerto = zonasConHuerto;

            // Si viene de una zona específica
            if (!string.IsNullOrEmpty(zonaId))
            {
                ViewBag.ZonaSeleccionada = zonaId;
                var zona = zonas.FirstOrDefault(z => z.Id == zonaId);
                ViewBag.NombreZona = zona?.NombreZona;
            }

            return View();
        }

        // POST: Alerta/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Alerta alerta)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    await CargarDatosParaFormulario();
                    return View(alerta);
                }

                ValidarAlerta(alerta);
                
                await _mongoService.Alertas.InsertOneAsync(alerta);
                TempData["Success"] = "Alerta creada exitosamente.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Error al crear alerta: {ex.Message}";
                await CargarDatosParaFormulario();
                return View(alerta);
            }
        }

        // GET: Alerta/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            try
            {
                var alerta = await _mongoService.Alertas.Find(a => a.Id == id).FirstOrDefaultAsync();
                if (alerta == null)
                {
                    TempData["Error"] = "Alerta no encontrada.";
                    return RedirectToAction("Index");
                }

                await CargarDatosParaFormulario();
                return View(alerta);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al obtener alerta: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        // POST: Alerta/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, Alerta alerta)
        {
            if (id != alerta.Id)
            {
                return NotFound();
            }

            try
            {
                if (!ModelState.IsValid)
                {
                    await CargarDatosParaFormulario();
                    return View(alerta);
                }

                var alertaActual = await _mongoService.Alertas.Find(a => a.Id == id).FirstOrDefaultAsync();
                if (alertaActual == null)
                {
                    TempData["Error"] = "Alerta no encontrada.";
                    return RedirectToAction("Index");
                }

                ValidarAlerta(alerta);

                var filter = Builders<Alerta>.Filter.Eq(a => a.Id, id);
                await _mongoService.Alertas.ReplaceOneAsync(filter, alerta);
                
                TempData["Success"] = "Alerta actualizada exitosamente.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Error al actualizar alerta: {ex.Message}";
                await CargarDatosParaFormulario();
                return View(alerta);
            }
        }

        // GET: Alerta/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            try
            {
                var alerta = await _mongoService.Alertas.Find(a => a.Id == id).FirstOrDefaultAsync();
                if (alerta == null)
                {
                    TempData["Error"] = "Alerta no encontrada.";
                    return RedirectToAction("Index");
                }

                // Obtener información de zona y huerto
                var zona = await _mongoService.Zonas.Find(z => z.Id == alerta.ZonaId).FirstOrDefaultAsync();
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

                // Obtener alertas relacionadas
                var alertasRelacionadas = await _mongoService.Alertas
                    .Find(a => a.ZonaId == alerta.ZonaId && a.Id != alerta.Id)
                    .Sort(Builders<Alerta>.Sort.Descending(a => a.FechaHora))
                    .Limit(5)
                    .ToListAsync();
                
                ViewBag.AlertasRelacionadas = alertasRelacionadas;

                return View(alerta);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al obtener detalles de la alerta: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        // POST: Alerta/ToggleStatus/5
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
                var alerta = await _mongoService.Alertas.Find(a => a.Id == id).FirstOrDefaultAsync();
                if (alerta == null)
                {
                    TempData["Error"] = "Alerta no encontrada.";
                    return RedirectToAction("Index");
                }

                // Cambiar el estado
                alerta.Estado = alerta.Estado == "pendiente" ? "resuelta" : "pendiente";

                var filter = Builders<Alerta>.Filter.Eq(a => a.Id, id);
                var update = Builders<Alerta>.Update.Set(a => a.Estado, alerta.Estado);
                
                await _mongoService.Alertas.UpdateOneAsync(filter, update);

                string mensaje = alerta.Estado == "resuelta" ? "Alerta resuelta exitosamente." : "Alerta marcada como pendiente.";
                TempData["Success"] = mensaje;
                
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al cambiar estado de la alerta: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        // GET: Alerta/Pendientes
        public IActionResult Pendientes()
        {
            return RedirectToAction("Index", new { filtroEstado = "Pendientes" });
        }

        // GET: Alerta/ByZona/5
        public async Task<IActionResult> ByZona(string id)
        {
            try
            {
                var zona = await _mongoService.Zonas.Find(z => z.Id == id).FirstOrDefaultAsync();
                if (zona == null)
                {
                    return NotFound();
                }

                return RedirectToAction("Index", new { filtroZona = id });
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        // GET: Alerta/Generar - Página para generar alertas automáticas
        public async Task<IActionResult> Generar()
        {
            try
            {
                // Obtener lecturas de las últimas 2 horas
                var hace2h = DateTime.Now.AddHours(-2);
                var lecturasRecientes = await _mongoService.Lecturas
                    .Find(l => l.FechaHora >= hace2h)
                    .ToListAsync();

                var alertasGeneradas = new List<string>();

                // Revisar temperaturas críticas
                var temperaturasAltas = lecturasRecientes
                    .Where(l => l.Tipo.ToLower().Contains("temperatura") && l.Valor > 35)
                    .ToList();

                foreach (var lectura in temperaturasAltas)
                {
                    var sensor = await _mongoService.Sensores.Find(s => s.Id == lectura.SensorId).FirstOrDefaultAsync();
                    if (sensor != null)
                    {
                        var nuevaAlerta = new Alerta
                        {
                            Tipo = "Temperatura Alta",
                            ZonaId = sensor.ZonaId,
                            ValorRegistrado = lectura.Valor,
                            UmbralMinimo = 15,
                            UmbralMaximo = 35,
                            ValorUmbralViolado = 35,
                            Mensaje = $"Temperatura crítica detectada: {lectura.Valor}°C. Se recomienda riego inmediato.",
                            Estado = "pendiente",
                            Enviada = false
                        };

                        await _mongoService.Alertas.InsertOneAsync(nuevaAlerta);
                        alertasGeneradas.Add($"Temperatura alta en sensor {sensor.Tipo}");
                    }
                }

                // Revisar humedad baja
                var humedadBaja = lecturasRecientes
                    .Where(l => l.Tipo.ToLower().Contains("humedad") && l.Valor < 30)
                    .ToList();

                foreach (var lectura in humedadBaja)
                {
                    var sensor = await _mongoService.Sensores.Find(s => s.Id == lectura.SensorId).FirstOrDefaultAsync();
                    if (sensor != null)
                    {
                        var nuevaAlerta = new Alerta
                        {
                            Tipo = "Humedad Baja",
                            ZonaId = sensor.ZonaId,
                            ValorRegistrado = lectura.Valor,
                            UmbralMinimo = 40,
                            UmbralMaximo = 90,
                            ValorUmbralViolado = 40,
                            Mensaje = $"Humedad crítica detectada: {lectura.Valor}%. Las plantas necesitan riego urgente.",
                            Estado = "pendiente",
                            Enviada = false
                        };

                        await _mongoService.Alertas.InsertOneAsync(nuevaAlerta);
                        alertasGeneradas.Add($"Humedad baja en sensor {sensor.Tipo}");
                    }
                }

                ViewBag.AlertasGeneradas = alertasGeneradas;
                ViewBag.TotalGeneradas = alertasGeneradas.Count;

                return View();
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Error al generar alertas: {ex.Message}";
                return View();
            }
        }

        // Método privado para cargar datos para formularios
        private async Task CargarDatosParaFormulario()
        {
            var zonas = await _mongoService.Zonas.Find(_ => true).ToListAsync();
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

        // Método privado para validar y limpiar datos de la alerta
        private void ValidarAlerta(Alerta alerta)
        {
            alerta.Tipo = string.IsNullOrWhiteSpace(alerta.Tipo) ? string.Empty : alerta.Tipo.Trim();
            alerta.Mensaje = string.IsNullOrWhiteSpace(alerta.Mensaje) ? string.Empty : alerta.Mensaje.Trim();
            alerta.Estado = string.IsNullOrWhiteSpace(alerta.Estado) ? "pendiente" : alerta.Estado.Trim().ToLower();

            // Validar umbrales
            if (alerta.UmbralMinimo > alerta.UmbralMaximo)
            {
                throw new ArgumentException("El umbral mínimo no puede ser mayor que el umbral máximo.");
            }
        }
    }
}