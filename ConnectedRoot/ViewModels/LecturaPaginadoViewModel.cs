using ConnectedRoot.Models;

namespace ConnectedRoot.ViewModels
{
    public class LecturaPaginadoViewModel
    {
        public List<Lectura> Lecturas { get; set; } = new List<Lectura>();
        public int PaginaActual { get; set; } = 1;
        public int TotalPaginas { get; set; } = 1;
        public int TotalLecturas { get; set; } = 0;
        public int LecturasPorPagina { get; set; } = 15;
        
        // Filtros
        public string? BusquedaTexto { get; set; }
        public string FiltroTipo { get; set; } = "Todos";
        public string FiltroSensor { get; set; } = "Todos";
        public string? FiltroFechaDesde { get; set; }
        public string? FiltroFechaHasta { get; set; }
        
        // Opciones para dropdowns
        public Dictionary<string, string> SensoresDisponibles { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, string> NombresSensores { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, string> NombresZonas { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, string> NombresHuertos { get; set; } = new Dictionary<string, string>();
        
        public List<string> TiposDisponibles { get; set; } = new List<string>
        {
            "Todos",
            "Temperatura",
            "Humedad del Suelo", 
            "Humedad Ambiental",
            "Luminosidad",
            "pH del Suelo"
        };
        
        // Propiedades calculadas para paginación
        public bool TienePaginaAnterior => PaginaActual > 1;
        public bool TienePaginaSiguiente => PaginaActual < TotalPaginas;
        
        public int LecturaDesde => TotalLecturas == 0 ? 0 : (PaginaActual - 1) * LecturasPorPagina + 1;
        public int LecturaHasta => Math.Min(PaginaActual * LecturasPorPagina, TotalLecturas);
    }
}

namespace ConnectedRoot.ViewModels
{
    public class AlertaPaginadoViewModel
    {
        public List<Alerta> Alertas { get; set; } = new List<Alerta>();
        public int PaginaActual { get; set; } = 1;
        public int TotalPaginas { get; set; } = 1;
        public int TotalAlertas { get; set; } = 0;
        public int AlertasPorPagina { get; set; } = 12;
        
        // Filtros
        public string? BusquedaTexto { get; set; }
        public string FiltroTipo { get; set; } = "Todos";
        public string FiltroEstado { get; set; } = "Todos";
        public string FiltroZona { get; set; } = "Todos";
        public string? FiltroFechaDesde { get; set; }
        public string? FiltroFechaHasta { get; set; }
        
        // Opciones para dropdowns
        public Dictionary<string, string> ZonasDisponibles { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, string> NombresZonas { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, string> NombresHuertos { get; set; } = new Dictionary<string, string>();
        
        public List<string> TiposAlertaDisponibles { get; set; } = new List<string>
        {
            "Todos",
            "Temperatura Alta",
            "Temperatura Baja", 
            "Humedad Baja",
            "Humedad Alta",
            "Sensor Desconectado",
            "Plagas Detectadas",
            "Mantenimiento Requerido"
        };
        
        public List<string> EstadosDisponibles { get; set; } = new List<string>
        {
            "Todos",
            "Pendientes",
            "Resueltas"
        };
        
        // Propiedades calculadas para paginación
        public bool TienePaginaAnterior => PaginaActual > 1;
        public bool TienePaginaSiguiente => PaginaActual < TotalPaginas;
        
        public int AlertaDesde => TotalAlertas == 0 ? 0 : (PaginaActual - 1) * AlertasPorPagina + 1;
        public int AlertaHasta => Math.Min(PaginaActual * AlertasPorPagina, TotalAlertas);
        
        // Estadísticas
        public int AlertasPendientes => Alertas.Count(a => a.Estado == "pendiente");
        public int AlertasHoy => Alertas.Count(a => a.FechaHora.Date == DateTime.Today);
    }
}