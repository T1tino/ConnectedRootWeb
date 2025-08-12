using ConnectedRoot.Services;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// Agregar servicios al contenedor
builder.Services.AddControllersWithViews();

// Configurar MongoDB
builder.Services.AddScoped<MongoDbService>();

// Configurar autenticación con cookies
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.LogoutPath = "/Auth/Logout";
        options.AccessDeniedPath = "/Auth/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Cookie.Name = "ConnectedRootAuth";
        options.Cookie.HttpOnly = true;
        // CAMBIO IMPORTANTE: Cambiar a None en desarrollo para HTTP
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment() 
            ? CookieSecurePolicy.None 
            : CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;
        
        // Mejorar el manejo de redirección
        options.Events.OnRedirectToLogin = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogInformation("Redirigiendo a login desde: {Path}", context.Request.Path);
            
            if (context.Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                context.Response.StatusCode = 401;
                return Task.CompletedTask;
            }
            context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        };
        
        // Agregar evento para depurar acceso denegado
        options.Events.OnRedirectToAccessDenied = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogWarning("Acceso denegado para usuario: {User} en ruta: {Path}", 
                context.HttpContext.User?.Identity?.Name ?? "Anónimo", 
                context.Request.Path);
            return Task.CompletedTask;
        };
    });

// CAMBIO: Hacer la política de autorización más flexible durante desarrollo
builder.Services.AddAuthorization(options =>
{
    if (builder.Environment.IsDevelopment())
    {
        // En desarrollo, solo requiere autenticación, no rol específico
        options.AddPolicy("AdminOnly", policy =>
            policy.RequireAuthenticatedUser());
    }
    else
    {
        // En producción, requiere rol específico
        options.AddPolicy("AdminOnly", policy =>
            policy.RequireRole("Administrador"));
    }
});

// Servicios adicionales
builder.Services.AddHttpContextAccessor();

// Configurar logging más detallado
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Logging.SetMinimumLevel(LogLevel.Information);

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// CAMBIO: Comentar HTTPS redirect en desarrollo si causa problemas
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseStaticFiles();
app.UseRouting();

// IMPORTANTE: El orden es crucial
app.UseAuthentication(); // Debe ir antes de Authorization
app.UseAuthorization();

// Middleware de logging para depuración
app.Use(async (context, next) =>
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("Request: {Method} {Path} - Usuario autenticado: {IsAuthenticated}", 
        context.Request.Method, 
        context.Request.Path, 
        context.User?.Identity?.IsAuthenticated ?? false);
    
    await next();
});

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Ruta adicional para el dashboard
app.MapControllerRoute(
    name: "dashboard",
    pattern: "dashboard",
    defaults: new { controller = "Home", action = "Dashboard" });

app.MapControllerRoute(
    name: "auth",
    pattern: "Auth/{action=Login}",
    defaults: new { controller = "Auth" });

app.Run();