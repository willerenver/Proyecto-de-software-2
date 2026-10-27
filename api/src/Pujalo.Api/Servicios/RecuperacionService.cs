using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Pujalo.Api.Datos;
using Pujalo.Api.Dominio;

namespace Pujalo.Api.Servicios;

// «Olvidé mi contraseña»: se envía un enlace con un código de un solo uso que vence en 1 hora.
// En la base solo queda el hash del código. Al cambiarla se cierran las sesiones abiertas.
public class RecuperacionService(PujaloDbContext db, IPasswordHasher<Usuario> hasher, IEnviadorCorreo correo, IConfiguration config, TimeProvider reloj)
{
    public static readonly TimeSpan Vigencia = TimeSpan.FromHours(1);

    // Responde igual exista o no la cuenta, para no revelar qué correos están registrados.
    public async Task SolicitarAsync(string correoUsuario, CancellationToken ct = default)
    {
        var correoNormal = correoUsuario.Trim().ToLowerInvariant();
        var usuario = await db.Usuarios.FirstOrDefaultAsync(u => u.Correo == correoNormal, ct);
        if (usuario is null || !usuario.Activo) return;

        var codigo = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        usuario.RecuperacionHash = Hash(codigo);
        usuario.RecuperacionExpira = reloj.GetUtcNow().UtcDateTime + Vigencia;
        await db.SaveChangesAsync(ct);

        var enlace = $"{(config["Web:Url"] ?? "http://localhost:5173").TrimEnd('/')}/restablecer-contrasena?codigo={codigo}";
        await correo.EnviarAsync(usuario.Correo, "Restablece tu contraseña de Pújalo",
            $"Hola, {usuario.Nombre}:\n\nPara elegir una contraseña nueva abre este enlace (vence en 1 hora):\n{enlace}\n\n" +
            "Si no lo pediste, ignora este correo: tu contraseña no cambia.", ct);
    }

    public async Task RestablecerAsync(string codigo, string contrasena, CancellationToken ct = default)
    {
        var hash = Hash(codigo.Trim());
        var ahora = reloj.GetUtcNow().UtcDateTime;
        var usuario = await db.Usuarios.FirstOrDefaultAsync(u => u.RecuperacionHash == hash, ct);
        if (usuario is null || usuario.RecuperacionExpira is null || usuario.RecuperacionExpira <= ahora)
            throw new ReglaException("El enlace no es válido o ya venció. Pide uno nuevo.");

        usuario.ContrasenaHash = hasher.HashPassword(usuario, contrasena);
        usuario.RecuperacionHash = null;
        usuario.RecuperacionExpira = null;
        usuario.VersionSesion++;
        await db.SaveChangesAsync(ct);
    }

    private static string Hash(string codigo) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(codigo))).ToLowerInvariant();
}
