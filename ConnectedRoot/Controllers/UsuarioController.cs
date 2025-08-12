using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ConnectedRoot.Services;
using ConnectedRoot.Models;
using ConnectedRoot.ViewModels;
using MongoDB.Driver;
using MongoDB.Bson;

namespace ConnectedRoot.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class UsuarioController : Controller
    {
        private readonly MongoDbService _mongoService;

        public UsuarioController(MongoDbService mongoService)
        {
            _mongoService = mongoService;
        }

        // GET: Usuario - MÉTODO ACTUALIZADO CON FILTROS Y PAGINACIÓN
        public async Task<IActionResult> Index(string? busquedaTexto, string? filtroRol, string? filtroEstado, int pagina = 1)
        {
            try
            {
                const int usuariosPorPagina = 10;

                // Crear el filtro base
                var filterBuilder = Builders<Usuario>.Filter;
                var filtros = new List<FilterDefinition<Usuario>>();

                // Filtro por texto (nombre, apellidos o correo)
                if (!string.IsNullOrWhiteSpace(busquedaTexto))
                {
                    var textoFiltro = filterBuilder.Or(
                        filterBuilder.Regex(u => u.Nombre, new BsonRegularExpression(busquedaTexto, "i")),
                        filterBuilder.Regex(u => u.PrimerApellido, new BsonRegularExpression(busquedaTexto, "i")),
                        filterBuilder.Regex(u => u.SegundoApellido, new BsonRegularExpression(busquedaTexto, "i")),
                        filterBuilder.Regex(u => u.Correo, new BsonRegularExpression(busquedaTexto, "i"))
                    );
                    filtros.Add(textoFiltro);
                }

                // Filtro por rol
                if (!string.IsNullOrWhiteSpace(filtroRol) && filtroRol != "Todos")
                {
                    // Usar expresión regular con opción "i" (insensitive)
                    filtros.Add(filterBuilder.Regex(u => u.Rol, new BsonRegularExpression($"^{filtroRol}$", "i")));
                }

                // Filtro por estado
                if (!string.IsNullOrWhiteSpace(filtroEstado) && filtroEstado != "Todos")
                {
                    bool esActivo = filtroEstado == "Activos";
                    filtros.Add(filterBuilder.Eq(u => u.Activo, esActivo));
                }

                // Combinar todos los filtros
                var filtroFinal = filtros.Count > 0
                    ? filterBuilder.And(filtros)
                    : filterBuilder.Empty;

                // Contar total de usuarios que cumplen el filtro
                var totalUsuarios = await _mongoService.Usuarios.CountDocumentsAsync(filtroFinal);

                // Calcular paginación
                var totalPaginas = (int)Math.Ceiling((double)totalUsuarios / usuariosPorPagina);
                var usuariosAOmitir = (pagina - 1) * usuariosPorPagina;

                // Obtener usuarios de la página actual
                var usuarios = await _mongoService.Usuarios
                    .Find(filtroFinal)
                    .Skip(usuariosAOmitir)
                    .Limit(usuariosPorPagina)
                    .ToListAsync();

                // Crear el ViewModel
                var viewModel = new UsuarioPaginadoViewModel
                {
                    Usuarios = usuarios,
                    PaginaActual = pagina,
                    TotalPaginas = totalPaginas,
                    TotalUsuarios = (int)totalUsuarios,
                    UsuariosPorPagina = usuariosPorPagina,
                    BusquedaTexto = busquedaTexto,
                    FiltroRol = filtroRol ?? "Todos",
                    FiltroEstado = filtroEstado ?? "Todos"
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
                return View(new UsuarioPaginadoViewModel());
            }
        }

        // GET: Usuario/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            try
            {
                var usuario = await _mongoService.Usuarios.Find(u => u.Id == id).FirstOrDefaultAsync();
                if (usuario == null)
                {
                    TempData["Error"] = "Usuario no encontrado.";
                    return RedirectToAction("Index");
                }
                return View(usuario);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al obtener usuario: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        // GET: Usuario/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Usuario/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Usuario usuario)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return View(usuario);
                }

                // Validar que el correo no exista
                var existeCorreo = await _mongoService.Usuarios
                    .Find(u => u.Correo.ToLower() == usuario.Correo.ToLower())
                    .FirstOrDefaultAsync();

                if (existeCorreo != null)
                {
                    ModelState.AddModelError("Correo", "Ya existe un usuario con este correo electrónico.");
                    return View(usuario);
                }

                // Validar y limpiar los datos antes de guardar
                ValidarUsuario(usuario);

                // TODO: Implementar hash de contraseña en producción
                // usuario.Contraseña = HashPassword(usuario.Contraseña);

                await _mongoService.Usuarios.InsertOneAsync(usuario);
                TempData["Success"] = "Usuario creado exitosamente.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Error al crear usuario: {ex.Message}";
                return View(usuario);
            }
        }

        // GET: Usuario/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            try
            {
                var usuario = await _mongoService.Usuarios.Find(u => u.Id == id).FirstOrDefaultAsync();
                if (usuario == null)
                {
                    TempData["Error"] = "Usuario no encontrado.";
                    return RedirectToAction("Index");
                }

                // Limpiar contraseña para no mostrarla en el formulario
                usuario.Contraseña = string.Empty;
                return View(usuario);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al obtener usuario: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        // POST: Usuario/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, Usuario usuario)
        {
            if (id != usuario.Id)
            {
                return NotFound();
            }

            try
            {
                if (!ModelState.IsValid)
                {
                    return View(usuario);
                }

                // Obtener el usuario actual ANTES de las validaciones
                var usuarioActual = await _mongoService.Usuarios.Find(u => u.Id == id).FirstOrDefaultAsync();
                if (usuarioActual == null)
                {
                    TempData["Error"] = "Usuario no encontrado.";
                    return RedirectToAction("Index");
                }

                // Validar que el correo no exista en otro usuario (solo si el correo cambió)
                if (usuario.Correo.ToLower() != usuarioActual.Correo.ToLower())
                {
                    var existeCorreo = await _mongoService.Usuarios
                        .Find(u => u.Correo.ToLower() == usuario.Correo.ToLower() && u.Id != id)
                        .FirstOrDefaultAsync();

                    if (existeCorreo != null)
                    {
                        ModelState.AddModelError("Correo", "Ya existe otro usuario con este correo electrónico.");
                        return View(usuario);
                    }
                }

                ValidarUsuario(usuario);

                // Si no se proporcionó nueva contraseña, mantener la actual
                if (string.IsNullOrWhiteSpace(usuario.Contraseña))
                {
                    usuario.Contraseña = usuarioActual.Contraseña;
                }
                else
                {
                    // TODO: Implementar hash de contraseña en producción
                    // usuario.Contraseña = HashPassword(usuario.Contraseña);
                }

                // Preservar campos que no deben perderse
                usuario.HuertosAsignadosIds = usuarioActual.HuertosAsignadosIds ?? new List<string>();

                var filter = Builders<Usuario>.Filter.Eq(u => u.Id, id);
                await _mongoService.Usuarios.ReplaceOneAsync(filter, usuario);

                TempData["Success"] = "Usuario actualizado exitosamente.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Error al actualizar usuario: {ex.Message}";
                return View(usuario);
            }
        }

        // POST: Usuario/ToggleStatus/5
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
                var usuario = await _mongoService.Usuarios.Find(u => u.Id == id).FirstOrDefaultAsync();
                if (usuario == null)
                {
                    TempData["Error"] = "Usuario no encontrado.";
                    return RedirectToAction("Index");
                }

                // Cambiar el estado
                usuario.Activo = !usuario.Activo;

                var filter = Builders<Usuario>.Filter.Eq(u => u.Id, id);
                var update = Builders<Usuario>.Update.Set(u => u.Activo, usuario.Activo);

                await _mongoService.Usuarios.UpdateOneAsync(filter, update);

                string mensaje = usuario.Activo ? "Usuario activado exitosamente." : "Usuario desactivado exitosamente.";
                TempData["Success"] = mensaje;

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al cambiar estado del usuario: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        // Método privado para validar y limpiar datos del usuario
        private void ValidarUsuario(Usuario usuario)
        {
            usuario.Nombre = string.IsNullOrWhiteSpace(usuario.Nombre) ? string.Empty : usuario.Nombre.Trim();
            usuario.PrimerApellido = string.IsNullOrWhiteSpace(usuario.PrimerApellido) ? string.Empty : usuario.PrimerApellido.Trim();
            usuario.SegundoApellido = string.IsNullOrWhiteSpace(usuario.SegundoApellido) ? string.Empty : usuario.SegundoApellido.Trim();
            usuario.Correo = string.IsNullOrWhiteSpace(usuario.Correo) ? string.Empty : usuario.Correo.Trim().ToLower();
            usuario.Rol = string.IsNullOrWhiteSpace(usuario.Rol) ? string.Empty : usuario.Rol.Trim();

            // No sobreescribir HuertosAsignadosIds si ya tiene valor
            if (usuario.HuertosAsignadosIds == null)
            {
                usuario.HuertosAsignadosIds = new List<string>();
            }
        }

        // TODO: Implementar en producción
        // private string HashPassword(string password)
        // {
        //     // Usar BCrypt, Argon2 o similar
        //     return password; // Temporal
        // }
    }
}