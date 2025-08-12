using ConnectedRoot.Models;

namespace ConnectedRoot.ViewModels
{
    public class ZonaPaginadoViewModel
    {
        public List<Zona> Zonas { get; set; } = new List<Zona>();
        public int PaginaActual { get; set; } = 1;
        public int TotalPaginas { get; set; } = 1;
        public int TotalZonas { get; set; } = 0;
        public int ZonasPorPagina { get; set; } = 12;
        
        // Filtros
        public string? BusquedaTexto { get; set; }
        public string FiltroTipoZona { get; set; } = "Todos";
        public string? FiltroHuerto { get; set; }
        
        // Propiedades calculadas para paginación
        public bool TienePaginaAnterior => PaginaActual > 1;
        public bool TienePaginaSiguiente => PaginaActual < TotalPaginas;
        public int ZonaDesde => TotalZonas == 0 ? 0 : ((PaginaActual - 1) * ZonasPorPagina) + 1;
        public int ZonaHasta => Math.Min(PaginaActual * ZonasPorPagina, TotalZonas);
        
        // Tipos de zona disponibles para el filtro
        public List<string> TiposZonaDisponibles { get; set; } = new List<string>
        {
            "Todos",
            "Hortalizas",
            "Frutales",
            "Aromáticas",
            "Semillero", 
            "Compostaje",
            "Invernadero",
            "Otro"
        };

        // Huertos disponibles para el filtro
        public Dictionary<string, string> HuertosDisponibles { get; set; } = new Dictionary<string, string>();
        
        // Para mostrar nombres de huertos en lugar de IDs
        public Dictionary<string, string> NombresHuertos { get; set; } = new Dictionary<string, string>();
    }
}