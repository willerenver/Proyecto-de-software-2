using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Pujalo.Api.Dominio;

namespace Pujalo.Api.Servicios;

public class TokenService(IConfiguration config, TimeProvider reloj)
{
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

public static class ClaimsExtensions
{
    public static long UsuarioId(this ClaimsPrincipal user) =>
        long.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("Usuario sin identificador."));
}
