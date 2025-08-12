using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using ConnectedRoot.Services;
using ConnectedRoot.ViewModels;
using MongoDB.Driver;
using System.Security.Claims;

namespace ConnectedRoot.Controllers
{
    public class AuthController : Controller
    {
        private readonly MongoDbService _mongoService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(MongoDbService mongoService, ILogger<AuthController> logger)
        {
            _mongoService = mongoService;
            _logger = logger;
        }

        // GET: /Auth/Login
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            // Si ya está autenticado, redirigir al dashboard
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        // POST: /Auth/Login
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                // Buscar usuario por correo
                var usuario = await _mongoService.Usuarios
                    .Find(u => u.Correo.ToLower() == model.Correo.ToLower())
                    .FirstOrDefaultAsync();

                // Validar usuario existe
                if (usuario == null)
                {
                    ModelState.AddModelError(string.Empty, "Correo o contraseña incorrectos.");
                    return View(model);
                }

                // Validar usuario activo
                if (!usuario.Activo)
                {
                    ModelState.AddModelError(string.Empty, "Su cuenta está desactivada. Contacte al administrador.");
                    return View(model);
                }

                // Validar contraseña (en producción usar hash)
                // TODO: Implementar verificación de hash en producción
                if (usuario.Contraseña != model.Contraseña)
                {
                    ModelState.AddModelError(string.Empty, "Correo o contraseña incorrectos.");
                    return View(model);
                }

                // Validar que solo administradores puedan acceder
                if (usuario.Rol.ToLower() != "administrador")
                {
                    ModelState.AddModelError(string.Empty, "Acceso denegado. Solo los administradores pueden ingresar al sistema.");
                    _logger.LogWarning("Intento de acceso denegado para usuario {Correo} con rol {Rol}", 
                        usuario.Correo, usuario.Rol);
                    return View(model);
                }

                // Crear claims del usuario
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, usuario.Id!),
                    new Claim(ClaimTypes.Name, $"{usuario.Nombre} {usuario.PrimerApellido}"),
                    new Claim(ClaimTypes.Email, usuario.Correo),
                    new Claim(ClaimTypes.Role, usuario.Rol.First().ToString().ToUpper() + usuario.Rol.Substring(1).ToLower()),
                    new Claim("UsuarioId", usuario.Id!),
                    new Claim("NombreCompleto", $"{usuario.Nombre} {usuario.PrimerApellido} {usuario.SegundoApellido}".Trim())
                };

                // Crear identidad
                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                // Configurar propiedades de autenticación
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = model.RecordarUsuario,
                    ExpiresUtc = model.RecordarUsuario 
                        ? DateTimeOffset.UtcNow.AddDays(30) 
                        : DateTimeOffset.UtcNow.AddHours(8)
                };

                // Iniciar sesión
                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                _logger.LogInformation("Usuario {Correo} inició sesión exitosamente", usuario.Correo);

                // Redirigir
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }

                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error durante el proceso de login para {Correo}", model.Correo);
                ModelState.AddModelError(string.Empty, "Error interno del servidor. Intente nuevamente.");
                return View(model);
            }
        }

        // POST: /Auth/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            try
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                _logger.LogInformation("Usuario {UserId} cerró sesión", User.FindFirst("UsuarioId")?.Value);
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error durante el logout");
                return RedirectToAction("Login");
            }
        }

        // GET: /Auth/AccessDenied
        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}