using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using ElMejorPostor.Api.Datos;
using ElMejorPostor.Api.Dominio;
using ElMejorPostor.Api.Controllers;
using ElMejorPostor.Api.Servicios;

var builder = WebApplication.CreateBuilder(args);

// SQL Server. La cadena va en user-secrets o en la variable ConnectionStrings__ElMejorPostor (ver README).
var conexion = builder.Configuration.GetConnectionString("ElMejorPostor");
if (string.IsNullOrWhiteSpace(conexion))
    throw new InvalidOperationException("Falta la cadena de conexión ConnectionStrings:ElMejorPostor. Configúrala con dotnet user-secrets (ver README).");
// Reintentos: en Docker la API puede arrancar antes de que SQL Server acepte conexiones.
builder.Services.AddDbContext<ElMejorPostorDbContext>(o => o.UseSqlServer(conexion, sql => sql.EnableRetryOnFailure(10)));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<PujaService>();
builder.Services.AddScoped<CierreService>();
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<VendedorService>();
builder.Services.AddScoped<Avisos>();
builder.Services.AddSignalR();
builder.Services.AddSingleton<INotificadorSubastas, NotificadorSubastas>();
builder.Services.AddScoped<RecuperacionService>();
builder.Services.AddSingleton<ColaDeCorreo>();
builder.Services.AddSingleton<IEnviadorCorreo>(sp => sp.GetRequiredService<ColaDeCorreo>());
builder.Services.AddScoped<BuzonDeCorreo>();
builder.Services.AddHostedService<EnvioDeCorreoWorker>();
if (!string.IsNullOrWhiteSpace(builder.Configuration["Smtp:Host"]))
    builder.Services.AddSingleton<IServidorCorreo, SmtpServidorCorreo>();
else if (builder.Environment.IsDevelopment())
    builder.Services.AddSingleton<IServidorCorreo, RegistroServidorCorreo>();
else
    throw new InvalidOperationException("Falta configurar Smtp:Host. Fuera de Development los correos se envían por SMTP.");
builder.Services.AddScoped<IPasswordHasher<Usuario>, PasswordHasher<Usuario>>();
builder.Services.AddHostedService<CierreWorker>();
// /api/salud responde 200 si la API está arriba y llega a SQL Server (lo usan Docker y el monitoreo).
builder.Services.AddHealthChecks().AddDbContextCheck<ElMejorPostorDbContext>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(o =>
{
    o.MapInboundClaims = false;
    o.TokenValidationParameters = new TokenValidationParameters
    {
        ValidIssuer = builder.Configuration["Jwt:Emisor"],
        ValidAudience = builder.Configuration["Jwt:Audiencia"],
        IssuerSigningKey = TokenService.Clave(builder.Configuration),
        NameClaimType = System.Security.Claims.ClaimTypes.Name,
        RoleClaimType = System.Security.Claims.ClaimTypes.Role,
    };
    o.Events = new JwtBearerEvents { OnTokenValidated = ValidacionDeSesion.ValidarAsync };
});

// Límite de intentos por IP en entrar, registrarse y recuperar la contraseña.
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    o.OnRejected = (context, ct) => new ValueTask(context.HttpContext.Response.WriteAsJsonAsync(
        new { title = "Hiciste demasiados intentos. Espera un minuto y vuelve a intentarlo.", status = 429 }, ct));
    o.AddPolicy(Limites.Cuenta, context => System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "sin-ip",
        _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions { PermitLimit = builder.Configuration.GetValue("Limites:CuentaPorMinuto", 10), Window = TimeSpan.FromMinutes(1) }));
});

// Detrás de nginx (docker-compose) la IP real del cliente llega en X-Forwarded-For. Solo se acepta si
// la configuración lo pide (Proxy:Confiable) y viene de una red privada, que es donde está el proxy.
var detrasDeProxy = builder.Configuration.GetValue("Proxy:Confiable", false);
if (detrasDeProxy)
    builder.Services.Configure<Microsoft.AspNetCore.Builder.ForwardedHeadersOptions>(o =>
    {
        o.ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor;
        o.ForwardLimit = 1;
        foreach (var red in new[] { "10.0.0.0/8", "172.16.0.0/12", "192.168.0.0/16" })
            o.KnownIPNetworks.Add(System.Net.IPNetwork.Parse(red));
    });
builder.Services.AddAuthorization();

builder.Services.AddCors(o => o.AddDefaultPolicy(p => p
    .WithOrigins(builder.Configuration.GetSection("Cors:Origenes").Get<string[]>() ?? [])
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials()
    .WithExposedHeaders(Paginacion.CabeceraTotal)));

builder.Services.AddControllers().ConfigureApiBehaviorOptions(o => o.InvalidModelStateResponseFactory = context =>
    new Microsoft.AspNetCore.Mvc.BadRequestObjectResult(new
    {
        title = "Revisa los datos del formulario.",
        status = StatusCodes.Status400BadRequest,
        errors = context.ModelState.Where(e => e.Value?.Errors.Count > 0)
            .ToDictionary(e => e.Key, e => e.Value!.Errors.Select(x => x.ErrorMessage).ToArray()),
    }));
// Documento OpenAPI con el esquema del token, para probar en /api/docs los endpoints que piden sesión.
builder.Services.AddOpenApi(o => o.AddDocumentTransformer((documento, _, _) =>
{
    documento.Info.Title = "API de El Mejor Postor";
    documento.Info.Description = "Subastas en línea. Para los endpoints con sesión, entra con POST /api/auth/login y usa el token.";
    documento.Components ??= new Microsoft.OpenApi.OpenApiComponents();
    documento.Components.SecuritySchemes ??= new Dictionary<string, Microsoft.OpenApi.IOpenApiSecurityScheme>();
    documento.Components.SecuritySchemes["Bearer"] = new Microsoft.OpenApi.OpenApiSecurityScheme
    {
        Type = Microsoft.OpenApi.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "El token que devuelve /api/auth/login.",
    };
    documento.Security = [new Microsoft.OpenApi.OpenApiSecurityRequirement { [new Microsoft.OpenApi.OpenApiSecuritySchemeReference("Bearer", documento)] = [] }];
    return Task.CompletedTask;
}));

var app = builder.Build();

// Las reglas de negocio se devuelven con su mensaje; cualquier otro error, con un texto genérico.
app.UseExceptionHandler(e => e.Run(async context =>
{
    var error = context.Features.Get<IExceptionHandlerFeature>()?.Error;
    var (estado, titulo) = error is ReglaException regla
        ? (regla.EstadoHttp, regla.Message)
        : (StatusCodes.Status500InternalServerError, "Ocurrió un error inesperado. Intenta de nuevo en unos minutos.");
    if (estado == StatusCodes.Status500InternalServerError)
        app.Logger.LogError(error, "Error no controlado en {Ruta}", context.Request.Path);
    context.Response.StatusCode = estado;
    await context.Response.WriteAsJsonAsync(new { title = titulo, status = estado });
}));

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ElMejorPostorDbContext>();
    await db.Database.MigrateAsync();
    if (args.Contains("--sembrar-demo") || args.Contains("--reiniciar-demo"))
        await DemoSeeder.SembrarAsync(scope.ServiceProvider, app.Configuration, app.Environment, reiniciar: args.Contains("--reiniciar-demo"));
}

// Documentación de la API, solo en desarrollo: /api/docs (Scalar) y /api/openapi/v1.json.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi("/api/openapi/{documentName}.json");
    app.MapScalarApiReference("/api/docs", o => o
        .WithTitle("API de El Mejor Postor")
        .WithOpenApiRoutePattern("/api/openapi/{documentName}.json")
        .AddPreferredSecuritySchemes("Bearer"));
}

if (detrasDeProxy)
    app.UseForwardedHeaders();
app.UseStaticFiles();
app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<SubastasHub>(SubastasHub.Ruta);
app.MapHealthChecks("/api/salud");

app.Run();

public partial class Program;
