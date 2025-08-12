using ConnectedRoot.Models;

namespace ConnectedRoot.ViewModels
{
    public class DashboardViewModel
    {
        // Resumen General
        public int TotalHuertosActivos { get; set; }
        public int TotalZonas { get; set; }
        public int SensoresFuncionando { get; set; }
        public int TotalSensores { get; set; }
        public int AlertasPendientes { get; set; }

        // Estado de Sensores
        public double TemperaturaPromedio { get; set; }
        public double HumedadPromedio { get; set; }
        public int SensoresOffline { get; set; }
        public DateTime UltimaActualizacion { get; set; }

        // Alertas Críticas
        public int AlertasTemperaturaAlta { get; set; }
        public int AlertasHumedadBaja { get; set; }
        public int AlertasSensoresSinDatos { get; set; }
        public List<Alerta> AlertasCriticasHoy { get; set; } = new();

        // Producción
        public int PlantasEnCrecimiento { get; set; }
        public int ProximasCosechas { get; set; }
        public Dictionary<string, int> PlantasPorEstado { get; set; } = new();

        // Tendencias
        public int LecturasUltimos7Dias { get; set; }
        public double PromedioLecturasPorDia { get; set; }
        public string ZonaMasActiva { get; set; } = string.Empty;
        public int LecturasZonaMasActiva { get; set; }

        // Rendimiento por Huerto
        public List<RendimientoHuerto> RendimientoPorHuerto { get; set; } = new();

        // Tareas Pendientes
        public int SensoresMantenimiento { get; set; }
        public int ZonasRiegoUrgente { get; set; }
        public int CosechasProgramadas { get; set; }

        // Datos para gráficas
        public List<AlertasPorHuerto> AlertasPorHuertoData { get; set; } = new();
        public List<TendenciaLectura> TendenciasTemperaturaHumedad { get; set; } = new();
        public List<ZonaEstado> MapaCalorZonas { get; set; } = new();

        // Información adicional
        public Dictionary<string, string> NombresHuertos { get; set; } = new();
        public Dictionary<string, string> NombresZonas { get; set; } = new();
    }

    public class RendimientoHuerto
    {
        public string HuertoId { get; set; } = string.Empty;
        public string NombreHuerto { get; set; } = string.Empty;
        public double PorcentajeSensoresActivos { get; set; }
        public int TotalAlertas { get; set; }
        public string Estado { get; set; } = string.Empty; // "excelente", "bueno", "regular", "critico"
    }

    public class AlertasPorHuerto
    {
        public string NombreHuerto { get; set; } = string.Empty;
        public int CantidadAlertas { get; set; }
    }

    public class TendenciaLectura
    {
        public DateTime Fecha { get; set; }
        public double TemperaturaPromedio { get; set; }
        public double HumedadPromedio { get; set; }
    }

    public class ZonaEstado
    {
        public string ZonaId { get; set; } = string.Empty;
        public string NombreZona { get; set; } = string.Empty;
        public string NombreHuerto { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty; // "bien", "alerta", "critico"
        public int AlertasActivas { get; set; }
        public double UltimaTemperatura { get; set; }
        public double UltimaHumedad { get; set; }
    }
}