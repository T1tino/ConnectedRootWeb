using Microsoft.AspNetCore.Mvc;
using ConnectedRoot.Services;
using ConnectedRoot.Models;
using MongoDB.Driver;

namespace ConnectedRoot.Controllers
{
    public class PlantaController : Controller
    {
        private readonly MongoDbService _mongoService;

        public PlantaController(MongoDbService mongoService)
        {
            _mongoService = mongoService;
        }

        // GET: Planta
        public async Task<IActionResult> Index(string? zonaId = null, string? tipoCultivo = null, string? estado = null)
        {
            try
            {
                // Construir filtro
                var filter = Builders<Planta>.Filter.Empty;

                if (!string.IsNullOrEmpty(zonaId))
                {
                    filter &= Builders<Planta>.Filter.Eq(p => p.ZonaId, zonaId);
                }

                if (!string.IsNullOrEmpty(tipoCultivo))
                {
                    filter &= Builders<Planta>.Filter.Eq(p => p.TipoCultivo, tipoCultivo);
                }

                if (!string.IsNullOrEmpty(estado))
                {
                    filter &= Builders<Planta>.Filter.Eq(p => p.Estado, estado);
                }

                var plantas = await _mongoService.Plantas
                    .Find(filter)
                    .SortByDescending(p => p.FechaSiembra)
                    .ToListAsync();

                // Obtener información de zonas y huertos
                var zonas = await _mongoService.Zonas.Find(_ => true).ToListAsync();
                var huertos = await _mongoService.Huertos.Find(_ => true).ToListAsync();

                ViewBag.Zonas = zonas.Where(z => z.Id != null).ToDictionary(z => z.Id!, z => z.NombreZona ?? "Sin nombre");
                ViewBag.Huertos = huertos.Where(h => h.Id != null).ToDictionary(h => h.Id!, h => h.NombreHuerto ?? "Sin nombre");

                // Para filtros
                ViewBag.ZonaSeleccionada = zonaId;
                ViewBag.TipoCultivoSeleccionado = tipoCultivo;
                ViewBag.EstadoSeleccionado = estado;
                ViewBag.TodasZonas = zonas;

                // Estadísticas
                var totalPlantas = plantas.Count;
                var plantasGerminando = plantas.Count(p => p.Estado == "Germinando");
                var plantasCreciendo = plantas.Count(p => p.Estado == "Crecimiento");
                var plantasFloreciendo = plantas.Count(p => p.Estado == "Floración");
                var plantasCosechando = plantas.Count(p => p.Estado == "Cosecha");

                ViewBag.TotalPlantas = totalPlantas;
                ViewBag.PlantasGerminando = plantasGerminando;
                ViewBag.PlantasCreciendo = plantasCreciendo;
                ViewBag.PlantasFloreciendo = plantasFloreciendo;
                ViewBag.PlantasCosechando = plantasCosechando;

                // Si viene de una zona específica
                if (!string.IsNullOrEmpty(zonaId))
                {
                    var zona = zonas.FirstOrDefault(z => z.Id == zonaId);
                    var huerto = zona != null ? huertos.FirstOrDefault(h => h.Id == zona.HuertoId) : null;
                    ViewBag.Zona = zona;
                    ViewBag.Huerto = huerto;
                    ViewBag.Title = $"Plantas de: {zona?.NombreZona}";
                }

                return View(plantas);
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Error de conexión: {ex.Message}";
                return View(new List<Planta>());
            }
        }

        // GET: Planta/Create
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

        // POST: Planta/Create
        [HttpPost]
        public async Task<IActionResult> Create(Planta planta)
        {
            try
            {
                await _mongoService.Plantas.InsertOneAsync(planta);
                return RedirectToAction("Index", new { zonaId = planta.ZonaId });
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Error al registrar planta: {ex.Message}";

                // Recargar datos
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
                return View(planta);
            }
        }

        // GET: Planta/Details/5
        public async Task<IActionResult> Details(string id)
        {
            try
            {
                var planta = await _mongoService.Plantas.Find(p => p.Id == id).FirstOrDefaultAsync();
                if (planta == null)
                {
                    return NotFound();
                }

                // Obtener información de zona y huerto
                var zona = await _mongoService.Zonas.Find(z => z.Id == planta.ZonaId).FirstOrDefaultAsync();
                var huerto = zona != null ? await _mongoService.Huertos.Find(h => h.Id == zona.HuertoId).FirstOrDefaultAsync() : null;

                ViewBag.Zona = zona;
                ViewBag.Huerto = huerto;

                // Calcular días desde siembra
                var diasDesdeSiembra = (DateTime.Now - planta.FechaSiembra).Days;
                ViewBag.DiasDesdeSiembra = diasDesdeSiembra;

                // Obtener sensores de la zona para monitoreo
                var sensores = await _mongoService.Sensores.Find(s => s.ZonaId == planta.ZonaId).ToListAsync();
                ViewBag.Sensores = sensores;

                // Obtener últimas lecturas de la zona
                if (sensores.Any())
                {
                    var sensorIds = sensores.Select(s => s.Id).ToList();
                    var lecturas = await _mongoService.Lecturas
                        .Find(l => sensorIds.Contains(l.SensorId))
                        .SortByDescending(l => l.FechaHora)
                        .Limit(5)
                        .ToListAsync();

                    ViewBag.UltimasLecturas = lecturas;
                }

                return View(planta);
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Error: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        // GET: Planta/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            try
            {
                var planta = await _mongoService.Plantas.Find(p => p.Id == id).FirstOrDefaultAsync();
                if (planta == null)
                {
                    return NotFound();
                }

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

                return View(planta);
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Error: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        // POST: Planta/Edit/5
        [HttpPost]
        public async Task<IActionResult> Edit(string id, Planta planta)
        {
            try
            {
                if (id != planta.Id)
                {
                    return BadRequest();
                }

                var filter = Builders<Planta>.Filter.Eq(p => p.Id, id);
                await _mongoService.Plantas.ReplaceOneAsync(filter, planta);

                return RedirectToAction("Details", new { id = id });
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Error al actualizar planta: {ex.Message}";

                // Recargar datos
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
                return View(planta);
            }
        }

        // GET: Planta/ByZona/5
        public IActionResult ByZona(string id)
        {
            return RedirectToAction("Index", new { zonaId = id });
        }

        // GET: Planta/Calendario
        public async Task<IActionResult> Calendario()
        {
            try
            {
                var plantas = await _mongoService.Plantas.Find(_ => true).ToListAsync();

                // Agrupar por mes de siembra
                var plantasPorMes = plantas
                    .GroupBy(p => p.FechaSiembra.ToString("yyyy-MM"))
                    .OrderBy(g => g.Key)
                    .ToList();

                ViewBag.PlantasPorMes = plantasPorMes;

                return View(plantas);
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Error: {ex.Message}";
                return View(new List<Planta>());
            }
        }
    }
}