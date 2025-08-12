using ConnectedRoot.Models;

namespace ConnectedRoot.ViewModels
{
    public class HuertoPaginadoViewModel
    {
        public List<Huerto> Huertos { get; set; } = new List<Huerto>();
        public int PaginaActual { get; set; } = 1;
        public int TotalPaginas { get; set; } = 1;
        public int TotalHuertos { get; set; } = 0;
        public int HuertosPorPagina { get; set; } = 10;
        
        // Filtros
        public string? BusquedaTexto { get; set; }
        public string FiltroEstado { get; set; } = "Todos";
        public string? FiltroFechaDesde { get; set; }
        public string? FiltroFechaHasta { get; set; }
        
        // Propiedades calculadas para paginación
        public bool TienePaginaAnterior => PaginaActual > 1;
        public bool TienePaginaSiguiente => PaginaActual < TotalPaginas;
        public int HuertoDesde => TotalHuertos == 0 ? 0 : ((PaginaActual - 1) * HuertosPorPagina) + 1;
        public int HuertoHasta => Math.Min(PaginaActual * HuertosPorPagina, TotalHuertos);
        
        // Estados disponibles para el filtro
        public List<string> EstadosDisponibles { get; set; } = new List<string>
        {
            "Todos",
            "Activos",
            "Inactivos"
        };
    }
}