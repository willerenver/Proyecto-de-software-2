using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Pujalo.Api.Dominio;

namespace Pujalo.Api.Servicios;

public class TokenService(IConfiguration config, TimeProvider reloj)
{
    public const string ClaimVersion = "ver";

    public static SymmetricSecurityKey Clave(IConfiguration config) =>
        new(Encoding.UTF8.GetBytes(config["Jwt:Clave"] ?? throw new InvalidOperationException("Falta Jwt:Clave en la configuración.")));

    public (string Token, DateTime Expira) Emitir(Usuario usuario)
    {
        var expira = reloj.GetUtcNow().UtcDateTime.AddHours(config.GetValue("Jwt:HorasDeValidez", 8));
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
            new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new Claim(ClaimTypes.Name, $"{usuario.Nombre} {usuario.Apellido}"),
            new Claim(ClaimTypes.Email, usuario.Correo),
            new Claim(ClaimTypes.Role, usuario.Rol.Nombre),
            new Claim(ClaimVersion, usuario.VersionSesion.ToString()),
        };
        var token = new JwtSecurityToken(
            issuer: config["Jwt:Emisor"],
            audience: config["Jwt:Audiencia"],
            claims: claims,
            expires: expira,
            signingCredentials: new SigningCredentials(Clave(config), SecurityAlgorithms.HmacSha256));
        return (new JwtSecurityTokenHandler().WriteToken(token), expira);
    }
}

public static class ValidacionDeSesion
{
    // Cada petición con token comprueba que la cuenta siga activa y que la sesión no se haya invalidado.
    public static async Task ValidarAsync(TokenValidatedContext context)
    {
        var principal = context.Principal!;
        if (!long.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
            || !int.TryParse(principal.FindFirstValue(TokenService.ClaimVersion), out var version))
        {
            context.Fail("Token sin datos de sesión.");
            return;
        }
        var db = context.HttpContext.RequestServices.GetRequiredService<Datos.PujaloDbContext>();
        var vigente = await db.Usuarios.AsNoTracking().AnyAsync(u => u.Id == id && u.Activo && u.VersionSesion == version, context.HttpContext.RequestAborted);
        if (!vigente) context.Fail("La sesión ya no es válida.");
    }
}

public static class ClaimsExtensions
{
    public static long UsuarioId(this ClaimsPrincipal user) =>
        long.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("Usuario sin identificador."));
}
