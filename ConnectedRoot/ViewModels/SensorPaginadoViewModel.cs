using ConnectedRoot.Models;

namespace ConnectedRoot.ViewModels
{
    public class SensorPaginadoViewModel
    {
        public List<Sensor> Sensores { get; set; } = new List<Sensor>();
        public int PaginaActual { get; set; } = 1;
        public int TotalPaginas { get; set; } = 1;
        public int TotalSensores { get; set; } = 0;
        public int SensoresPorPagina { get; set; } = 12;
        
        // Filtros
        public string? BusquedaTexto { get; set; }
        public string FiltroTipoSensor { get; set; } = "Todos";
        public string FiltroZona { get; set; } = "Todos";
        public string FiltroEstado { get; set; } = "Todos";
        
        // Opciones para dropdowns
        public Dictionary<string, string> ZonasDisponibles { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, string> NombresZonas { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, string> NombresHuertos { get; set; } = new Dictionary<string, string>();
        
        // AGREGAR ESTAS PROPIEDADES QUE FALTAN:
        public List<string> TiposSensorDisponibles { get; set; } = new List<string>
        {
            "Todos",
            "Temperatura",
            "Humedad del Suelo", 
            "Humedad Ambiental",
            "Luminosidad",
            "pH del Suelo",
            "Temperatura y Humedad"
        };
        
        public List<string> EstadosDisponibles { get; set; } = new List<string>
        {
            "Todos",
            "Activos",
            "Inactivos", 
            "Mantenimiento"
        };
        
        // Propiedades calculadas para paginación
        public bool TienePaginaAnterior => PaginaActual > 1;
        public bool TienePaginaSiguiente => PaginaActual < TotalPaginas;
        
        public int SensorDesde => TotalSensores == 0 ? 0 : (PaginaActual - 1) * SensoresPorPagina + 1;
        public int SensorHasta => Math.Min(PaginaActual * SensoresPorPagina, TotalSensores);
    }
}