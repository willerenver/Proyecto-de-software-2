using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Pujalo.Api.Datos;
using Pujalo.Api.Dominio;
using Pujalo.Api.Servicios;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<PujaloDbContext>(o => o.UseSqlite(builder.Configuration.GetConnectionString("Pujalo")));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<PujaService>();
builder.Services.AddScoped<CierreService>();
builder.Services.AddScoped<TokenService>();
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
    if (args.Contains("--sembrar-demo"))
        await DemoSeeder.SembrarAsync(scope.ServiceProvider, app.Configuration, app.Environment);
}

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.UseStaticFiles();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program;
