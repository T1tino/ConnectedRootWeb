using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ConnectedRoot.Models;
using ConnectedRoot.Services;
using ConnectedRoot.ViewModels;
using MongoDB.Driver;
using System.Security.Claims;

namespace ConnectedRoot.Controllers
{
    public class HomeController : Controller // QUITÉ EL [Authorize] de aquí
    {
        private readonly ILogger<HomeController> _logger;
        private readonly MongoDbService _mongoService;

        public HomeController(ILogger<HomeController> logger, MongoDbService mongoService)
        {
            _logger = logger;
            _mongoService = mongoService;
        }

        // OPCIÓN 1: Página pública que redirige al dashboard si está autenticado
        [AllowAnonymous]
        public IActionResult Index()
        {
            // Si está autenticado, redirigir al dashboard
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Dashboard");
            }
            
            // Si no está autenticado, redirigir al login
            return RedirectToAction("Login", "Auth");
        }

        // OPCIÓN 2: El dashboard real que requiere autenticación
        [Authorize] // Validaremos el rol manualmente por ahora
        public async Task<IActionResult> Dashboard()
        {
            // Validación manual del rol (case-insensitive)
            var userRoles = User.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value.ToLower()).ToList();
            if (!userRoles.Contains("administrador"))
            {
                return RedirectToAction("AccessDenied", "Auth");
            }

            try
            {
                var dashboard = await GenerarDashboardData();
                return View("Index", dashboard); // Usa la misma vista que tenías
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al generar dashboard para usuario {UserId}", 
                    User.FindFirst("UsuarioId")?.Value);
                ViewBag.Error = "Error al cargar el dashboard";
                return View("Index", new DashboardViewModel());
            }
        }

        private async Task<DashboardViewModel> GenerarDashboardData()
        {
            var dashboard = new DashboardViewModel();
            var fechaHoy = DateTime.Today;
            var hace7Dias = DateTime.Now.AddDays(-7);

            // Obtener datos base
            var huertos = await _mongoService.Huertos.Find(_ => true).ToListAsync();
            var zonas = await _mongoService.Zonas.Find(_ => true).ToListAsync();
            var sensores = await _mongoService.Sensores.Find(_ => true).ToListAsync();
            var alertas = await _mongoService.Alertas.Find(_ => true).ToListAsync();
            var plantas = await _mongoService.Plantas.Find(_ => true).ToListAsync();
            var lecturas = await _mongoService.Lecturas.Find(l => l.FechaHora >= hace7Dias).ToListAsync();

            // Crear diccionarios para lookups
            var huertosDict = huertos.ToDictionary(h => h.Id, h => h.NombreHuerto);
            var zonasDict = zonas.ToDictionary(z => z.Id, z => z.NombreZona);

            // 1. Resumen General
            dashboard.TotalHuertosActivos = huertos.Count(h => h.Estado == "activo");
            dashboard.TotalZonas = zonas.Count;
            dashboard.TotalSensores = sensores.Count;
            dashboard.SensoresFuncionando = sensores.Count(s => s.Estado == "activo");
            dashboard.AlertasPendientes = alertas.Count(a => a.Estado == "pendiente");

            // 2. Estado de Sensores (últimas lecturas)
            var lecturasRecientes = await _mongoService.Lecturas
                .Find(l => l.FechaHora >= DateTime.Now.AddHours(-2))
                .ToListAsync();

            var temperaturas = lecturasRecientes.Where(l => l.Tipo.ToLower().Contains("temperatura")).ToList();
            var humedades = lecturasRecientes.Where(l => l.Tipo.ToLower().Contains("humedad")).ToList();

            dashboard.TemperaturaPromedio = temperaturas.Any() ? Math.Round(temperaturas.Average(t => t.Valor), 1) : 0;
            dashboard.HumedadPromedio = humedades.Any() ? Math.Round(humedades.Average(h => h.Valor), 1) : 0;
            dashboard.SensoresOffline = dashboard.TotalSensores - dashboard.SensoresFuncionando;
            dashboard.UltimaActualizacion = lecturasRecientes.Any() ? lecturasRecientes.Max(l => l.FechaHora) : DateTime.Now;

            // 3. Alertas Críticas Hoy
            var alertasHoy = alertas.Where(a => a.FechaHora.Date == fechaHoy && a.Estado == "pendiente").ToList();
            dashboard.AlertasTemperaturaAlta = alertasHoy.Count(a => a.Tipo.ToLower().Contains("temperatura"));
            dashboard.AlertasHumedadBaja = alertasHoy.Count(a => a.Tipo.ToLower().Contains("humedad"));
            dashboard.AlertasSensoresSinDatos = alertasHoy.Count(a => a.Tipo.ToLower().Contains("sensor"));
            dashboard.AlertasCriticasHoy = alertasHoy.Take(5).ToList();

            // 4. Producción
            dashboard.PlantasEnCrecimiento = plantas.Count;
            dashboard.ProximasCosechas = plantas.Count(p => p.Estado.ToLower() == "cosecha");

            dashboard.PlantasPorEstado = plantas
                .GroupBy(p => p.Estado)
                .ToDictionary(g => g.Key, g => g.Count());

            // 5. Tendencias (últimos 7 días)
            dashboard.LecturasUltimos7Dias = lecturas.Count;
            dashboard.PromedioLecturasPorDia = dashboard.LecturasUltimos7Dias / 7.0;

            // Zona más activa
            var lecturasPorZona = lecturas
                .Join(sensores, l => l.SensorId, s => s.Id, (l, s) => new { Lectura = l, Sensor = s })
                .GroupBy(x => x.Sensor.ZonaId)
                .Select(g => new { ZonaId = g.Key, Cantidad = g.Count() })
                .OrderByDescending(x => x.Cantidad)
                .FirstOrDefault();

            if (lecturasPorZona != null)
            {
                dashboard.ZonaMasActiva = zonasDict.GetValueOrDefault(lecturasPorZona.ZonaId, "Zona desconocida");
                dashboard.LecturasZonaMasActiva = lecturasPorZona.Cantidad;
            }

            // 6. Rendimiento por Huerto
            dashboard.RendimientoPorHuerto = huertos.Select(h =>
            {
                var zonasHuerto = zonas.Where(z => z.HuertoId == h.Id).ToList();
                var sensoresHuerto = sensores.Where(s => zonasHuerto.Any(z => z.Id == s.ZonaId)).ToList();
                var alertasHuerto = alertas.Where(a => zonasHuerto.Any(z => z.Id == a.ZonaId) && a.Estado == "pendiente").Count();

                var porcentajeActivos = sensoresHuerto.Any()
                    ? (sensoresHuerto.Count(s => s.Estado == "activo") * 100.0 / sensoresHuerto.Count)
                    : 100;

                string estado = porcentajeActivos >= 95 && alertasHuerto == 0 ? "excelente" :
                               porcentajeActivos >= 80 && alertasHuerto <= 2 ? "bueno" :
                               porcentajeActivos >= 60 && alertasHuerto <= 5 ? "regular" : "critico";

                return new RendimientoHuerto
                {
                    HuertoId = h.Id,
                    NombreHuerto = h.NombreHuerto,
                    PorcentajeSensoresActivos = Math.Round(porcentajeActivos, 1),
                    TotalAlertas = alertasHuerto,
                    Estado = estado
                };
            }).ToList();

            // 7. Tareas Pendientes
            dashboard.SensoresMantenimiento = sensores.Count(s => s.Estado == "mantenimiento");
            dashboard.ZonasRiegoUrgente = alertasHoy.Count(a => a.Tipo.ToLower().Contains("humedad"));
            dashboard.CosechasProgramadas = plantas.Count(p => p.Estado.ToLower() == "cosecha");

            // 8. Datos para gráficas
            // Alertas por huerto (últimos 30 días)
            var hace30Dias = DateTime.Now.AddDays(-30);
            var alertasUltimos30Dias = alertas.Where(a => a.FechaHora >= hace30Dias).ToList();

            dashboard.AlertasPorHuertoData = huertos.Select(h =>
            {
                var zonasHuerto = zonas.Where(z => z.HuertoId == h.Id).Select(z => z.Id).ToList();
                var alertasHuerto = alertasUltimos30Dias.Count(a => zonasHuerto.Contains(a.ZonaId));

                return new AlertasPorHuerto
                {
                    NombreHuerto = h.NombreHuerto,
                    CantidadAlertas = alertasHuerto
                };
            }).ToList();

            // Tendencias de temperatura y humedad (últimos 7 días)
            dashboard.TendenciasTemperaturaHumedad = Enumerable.Range(0, 7)
                .Select(i => fechaHoy.AddDays(-6 + i))
                .Select(fecha =>
                {
                    var lecturasDia = lecturas.Where(l => l.FechaHora.Date == fecha).ToList();
                    var tempsDia = lecturasDia.Where(l => l.Tipo.ToLower().Contains("temperatura")).ToList();
                    var humsDia = lecturasDia.Where(l => l.Tipo.ToLower().Contains("humedad")).ToList();

                    return new TendenciaLectura
                    {
                        Fecha = fecha,
                        TemperaturaPromedio = tempsDia.Any() ? Math.Round(tempsDia.Average(t => t.Valor), 1) : 0,
                        HumedadPromedio = humsDia.Any() ? Math.Round(humsDia.Average(h => h.Valor), 1) : 0
                    };
                }).ToList();

            // Mapa de calor de zonas
            dashboard.MapaCalorZonas = zonas.Select(z =>
            {
                var alertasZona = alertas.Count(a => a.ZonaId == z.Id && a.Estado == "pendiente");
                var sensoresZona = sensores.Where(s => s.ZonaId == z.Id).ToList();
                var lecturasZona = lecturasRecientes.Where(l => sensoresZona.Any(s => s.Id == l.SensorId)).ToList();

                string estado = alertasZona == 0 ? "bien" :
                               alertasZona <= 2 ? "alerta" : "critico";

                var ultimaTemp = lecturasZona.Where(l => l.Tipo.ToLower().Contains("temperatura"))
                    .OrderByDescending(l => l.FechaHora).FirstOrDefault()?.Valor ?? 0;
                var ultimaHum = lecturasZona.Where(l => l.Tipo.ToLower().Contains("humedad"))
                    .OrderByDescending(l => l.FechaHora).FirstOrDefault()?.Valor ?? 0;

                return new ZonaEstado
                {
                    ZonaId = z.Id,
                    NombreZona = z.NombreZona,
                    NombreHuerto = huertosDict.GetValueOrDefault(z.HuertoId, "Huerto desconocido"),
                    Estado = estado,
                    AlertasActivas = alertasZona,
                    UltimaTemperatura = Math.Round(ultimaTemp, 1),
                    UltimaHumedad = Math.Round(ultimaHum, 1)
                };
            }).ToList();

            // Diccionarios adicionales
            dashboard.NombresHuertos = huertosDict;
            dashboard.NombresZonas = zonasDict;

            return dashboard;
        }

        [AllowAnonymous]
        public IActionResult Privacy()
        {
            return View();
        }

        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        // ENDPOINT TEMPORAL PARA DEBUGGING - ELIMINAR EN PRODUCCIÓN
        [AllowAnonymous]
        public IActionResult Debug()
        {
            var debugInfo = new
            {
                IsAuthenticated = User.Identity?.IsAuthenticated ?? false,
                Username = User.Identity?.Name,
                Claims = User.Claims.Select(c => new { Type = c.Type, Value = c.Value }).ToList(),
                Roles = User.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList(),
                HasAdminRole = User.IsInRole("Administrador"),
                RawRoles = User.Claims.Where(c => c.Type.Contains("role")).Select(c => new { c.Type, c.Value }).ToList()
            };

            return Json(debugInfo);
        }
    }
}