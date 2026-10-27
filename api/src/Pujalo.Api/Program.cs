using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Pujalo.Api.Datos;
using Pujalo.Api.Dominio;
using Pujalo.Api.Controllers;
using Pujalo.Api.Servicios;

var builder = WebApplication.CreateBuilder(args);

// SQL Server. La cadena va en user-secrets o en la variable ConnectionStrings__Pujalo (ver README).
var conexion = builder.Configuration.GetConnectionString("Pujalo");
if (string.IsNullOrWhiteSpace(conexion))
    throw new InvalidOperationException("Falta la cadena de conexión ConnectionStrings:Pujalo. Configúrala con dotnet user-secrets (ver README).");
// Reintentos: en Docker la API puede arrancar antes de que SQL Server acepte conexiones.
builder.Services.AddDbContext<PujaloDbContext>(o => o.UseSqlServer(conexion, sql => sql.EnableRetryOnFailure(10)));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<PujaService>();
builder.Services.AddScoped<CierreService>();
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<VendedorService>();
builder.Services.AddScoped<RecuperacionService>();
builder.Services.AddSingleton<ColaDeCorreo>();
builder.Services.AddSingleton<IEnviadorCorreo>(sp => sp.GetRequiredService<ColaDeCorreo>());
builder.Services.AddHostedService<EnvioDeCorreoWorker>();
if (!string.IsNullOrWhiteSpace(builder.Configuration["Smtp:Host"]))
    builder.Services.AddSingleton<IServidorCorreo, SmtpServidorCorreo>();
else if (builder.Environment.IsDevelopment())
    builder.Services.AddSingleton<IServidorCorreo, RegistroServidorCorreo>();
else
    throw new InvalidOperationException("Falta configurar Smtp:Host. Fuera de Development los correos se envían por SMTP.");
builder.Services.AddScoped<IPasswordHasher<Usuario>, PasswordHasher<Usuario>>();
builder.Services.AddHostedService<CierreWorker>();

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

// Detrás de nginx (docker-compose) la IP real del cliente llega en X-Forwarded-For.
builder.Services.Configure<Microsoft.AspNetCore.Builder.ForwardedHeadersOptions>(o =>
{
    o.ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor;
    o.KnownIPNetworks.Clear();
    o.KnownProxies.Clear();
});
builder.Services.AddAuthorization();

builder.Services.AddCors(o => o.AddDefaultPolicy(p => p
    .WithOrigins(builder.Configuration.GetSection("Cors:Origenes").Get<string[]>() ?? [])
    .AllowAnyHeader()
    .AllowAnyMethod()));

builder.Services.AddControllers().ConfigureApiBehaviorOptions(o => o.InvalidModelStateResponseFactory = context =>
    new Microsoft.AspNetCore.Mvc.BadRequestObjectResult(new
    {
        title = "Revisa los datos del formulario.",
        status = StatusCodes.Status400BadRequest,
        errors = context.ModelState.Where(e => e.Value?.Errors.Count > 0)
            .ToDictionary(e => e.Key, e => e.Value!.Errors.Select(x => x.ErrorMessage).ToArray()),
    }));
builder.Services.AddOpenApi();

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
    var db = scope.ServiceProvider.GetRequiredService<PujaloDbContext>();
    await db.Database.MigrateAsync();
    if (args.Contains("--sembrar-demo") || args.Contains("--reiniciar-demo"))
        await DemoSeeder.SembrarAsync(scope.ServiceProvider, app.Configuration, app.Environment, reiniciar: args.Contains("--reiniciar-demo"));
}

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.UseForwardedHeaders();
app.UseStaticFiles();
app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program;
