using ConnectedRoot.Services;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// Agregar servicios al contenedor
builder.Services.AddControllersWithViews();

// Configurar MongoDB
builder.Services.Configure<MongoDbSettings>(
    builder.Configuration.GetSection("MongoDbSettings"));

builder.Services.Configure<MongoDbSettings>(options =>
{
    var mongoSection = builder.Configuration.GetSection("MongoDB");
    if (mongoSection.Exists())
    {
        options.ConnectionString = mongoSection["ConnectionString"] ?? "";
        options.DatabaseName = mongoSection["DatabaseName"] ?? "";
    }
});

builder.Services.AddScoped<MongoDbService>();

// ✅ CORREGIDO: Configurar CORS compatible con todas las versiones
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSimulator", policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            // ✅ DESARROLLO: Permitir cualquier origen
            policy.AllowAnyOrigin()
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        }
        else
        {
            // ✅ PRODUCCIÓN: Orígenes específicos
            policy.WithOrigins(
                    "http://localhost:3000",
                    "http://127.0.0.1:3000",
                    "http://localhost:5500",
                    "http://127.0.0.1:5500",
                    "http://localhost:8080",
                    "http://127.0.0.1:8080"
                )
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        }
    });
});

// Agregar soporte para controladores de API
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.WriteIndented = true;
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });

// Configurar autenticación (tu código existente)
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
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment() 
            ? CookieSecurePolicy.None 
            : CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;
        
        options.Events.OnRedirectToLogin = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogInformation("Redirigiendo a login desde: {Path}", context.Request.Path);
            
            if (context.Request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
                context.Request.Path.StartsWithSegments("/api"))
            {
                context.Response.StatusCode = 401;
                context.Response.ContentType = "application/json";
                return context.Response.WriteAsync("{\"error\":\"No autorizado\",\"requiresAuth\":true}");
            }
            context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        };
        
        options.Events.OnRedirectToAccessDenied = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogWarning("Acceso denegado para usuario: {User} en ruta: {Path}", 
                context.HttpContext.User?.Identity?.Name ?? "Anónimo", 
                context.Request.Path);
            return Task.CompletedTask;
        };
    });

// Políticas de autorización
builder.Services.AddAuthorization(options =>
{
    if (builder.Environment.IsDevelopment())
    {
        options.AddPolicy("AdminOnly", policy =>
            policy.RequireAuthenticatedUser());
        
        options.AddPolicy("SimulatorApi", policy =>
            policy.RequireAssertion(context => true)); // Permitir todo en desarrollo
    }
    else
    {
        options.AddPolicy("AdminOnly", policy =>
            policy.RequireRole("Administrador"));
            
        options.AddPolicy("SimulatorApi", policy =>
            policy.RequireAuthenticatedUser());
    }
});

builder.Services.AddHttpContextAccessor();

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
else
{
    app.UseDeveloperExceptionPage();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseStaticFiles();
app.UseRouting();

// ✅ CLAVE: CORS debe ir ANTES de Authentication
app.UseCors("AllowSimulator");

app.UseAuthentication();
app.UseAuthorization();

// Middleware para logging
app.Use(async (context, next) =>
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    
    if (context.Request.Path.StartsWithSegments("/api"))
    {
        var method = context.Request.Method;
        var path = context.Request.Path;
        var origin = context.Request.Headers["Origin"].FirstOrDefault() ?? "No origin";
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        
        logger.LogInformation("[{Timestamp}] API Request: {Method} {Path} from {Origin}", timestamp, method, path, origin);
    }
    
    await next();
});

// Mapear controladores de API
app.MapControllers();

// Rutas MVC
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "dashboard",
    pattern: "dashboard",
    defaults: new { controller = "Home", action = "Dashboard" });

app.MapControllerRoute(
    name: "auth",
    pattern: "Auth/{action=Login}",
    defaults: new { controller = "Auth" });

// Endpoints de utilidad
app.MapGet("/health", () => new { 
    status = "healthy", 
    timestamp = DateTime.Now,
    environment = app.Environment.EnvironmentName,
    database = "connected",
    cors = "enabled"
});

app.MapGet("/simulador", () => Results.Redirect("/"));

// Endpoint para verificar CORS
app.MapGet("/api/test-cors", () => new {
    message = "CORS funcionando correctamente",
    timestamp = DateTime.Now,
    server = "ConnectedRoot Backend"
});

Console.WriteLine("🚀 Connected Root Server Starting...");
Console.WriteLine($"🌍 Environment: {app.Environment.EnvironmentName}");
Console.WriteLine("📡 API Endpoints:");
Console.WriteLine("   - POST /api/simulador/iniciar");
Console.WriteLine("   - POST /api/simulador/detener");
Console.WriteLine("   - GET  /api/simulador/estado/{sensorId}");
Console.WriteLine("   - GET  /api/simulador/sensores");
Console.WriteLine("   - POST /api/lecturas");
Console.WriteLine("   - GET  /api/lecturas/ultimas/{sensorId}");
Console.WriteLine("   - GET  /health");
Console.WriteLine("   - GET  /api/test-cors");
Console.WriteLine("🔐 Authentication: Cookie-based");
Console.WriteLine("🌐 CORS: Enabled for external frontend");

app.Run();

public class MongoDbSettings
{
    public string ConnectionString { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
}