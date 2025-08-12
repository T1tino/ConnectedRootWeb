using ConnectedRoot.Models;

namespace ConnectedRoot.ViewModels
{
    public class UsuarioPaginadoViewModel
    {
        // Lista de usuarios de la página actual
        public List<Usuario> Usuarios { get; set; } = new List<Usuario>();
        
        // Información de paginación
        public int PaginaActual { get; set; } = 1;
        public int TotalPaginas { get; set; }
        public int TotalUsuarios { get; set; }
        public int UsuariosPorPagina { get; set; } = 10;
        
        // Filtros
        public string? BusquedaTexto { get; set; }
        public string? FiltroRol { get; set; }
        public string? FiltroEstado { get; set; }
        
        // Propiedades calculadas para la vista
        public bool TienePaginaAnterior => PaginaActual > 1;
        public bool TienePaginaSiguiente => PaginaActual < TotalPaginas;
        public int UsuarioDesde => TotalUsuarios == 0 ? 0 : ((PaginaActual - 1) * UsuariosPorPagina) + 1;
        public int UsuarioHasta => Math.Min(PaginaActual * UsuariosPorPagina, TotalUsuarios);
        
        // Lista de roles para el dropdown
        public List<string> RolesDisponibles { get; set; } = new List<string> 
        { 
            "Todos", "Administrador", "Agricultor" 
        };
        
        // Lista de estados para el dropdown
        public List<string> EstadosDisponibles { get; set; } = new List<string> 
        { 
            "Todos", "Activos", "Inactivos" 
        };
    }
}