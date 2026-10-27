using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Pujalo.Api.Contratos;
using Pujalo.Api.Datos;
using Pujalo.Api.Dominio;
using Pujalo.Api.Servicios;

namespace Pujalo.Api.Controllers;

public static class Limites
{
    public const string Cuenta = "cuenta";
}

[ApiController]
[Route("api/auth")]
public class AuthController(PujaloDbContext db, TokenService tokens, IPasswordHasher<Usuario> hasher, RecuperacionService recuperacion, TimeProvider reloj) : ControllerBase
{
    [EnableRateLimiting(Limites.Cuenta)]
    [HttpPost("registro")]
    public async Task<ActionResult<SesionResponse>> Registrar(RegistroRequest r, CancellationToken ct)
    {
        var correo = r.Correo.Trim().ToLowerInvariant();
        var cedula = new string(r.Cedula.Where(char.IsDigit).ToArray());
        if (await db.Usuarios.AnyAsync(u => u.Correo == correo, ct))
            throw ReglaException.Conflicto("Ya existe una cuenta con ese correo.");
        if (await db.Usuarios.AnyAsync(u => u.Cedula == cedula, ct))
            throw ReglaException.Conflicto("Ya existe una cuenta con esa cédula.");

        var rol = await db.Roles.SingleAsync(x => x.Nombre == Rol.Usuario, ct);
        var usuario = new Usuario
        {
            Rol = rol,
            Nombre = r.Nombre.Trim(),
            Apellido = r.Apellido.Trim(),
            Correo = correo,
            Telefono = r.Telefono.Trim(),
            Cedula = cedula,
            CreadoEn = reloj.GetUtcNow().UtcDateTime,
        };
        usuario.ContrasenaHash = hasher.HashPassword(usuario, r.Contrasena);
        db.Usuarios.Add(usuario);
        await db.SaveChangesAsync(ct);
        return Sesion(usuario);
    }

    [EnableRateLimiting(Limites.Cuenta)]
    [HttpPost("login")]
    public async Task<ActionResult<SesionResponse>> Login(LoginRequest r, CancellationToken ct)
    {
        var correo = r.Correo.Trim().ToLowerInvariant();
        var usuario = await db.Usuarios.Include(u => u.Rol).FirstOrDefaultAsync(u => u.Correo == correo, ct);
        if (usuario is null || hasher.VerifyHashedPassword(usuario, usuario.ContrasenaHash, r.Contrasena) == PasswordVerificationResult.Failed)
            throw new ReglaException("El correo o la contraseña no son correctos.", StatusCodes.Status401Unauthorized);
        if (!usuario.Activo)
            throw ReglaException.Prohibido("Tu cuenta está desactivada. Escríbenos para revisarla.");
        return Sesion(usuario);
    }

    [EnableRateLimiting(Limites.Cuenta)]
    [HttpPost("recuperar")]
    public async Task<IActionResult> Recuperar(RecuperarRequest r, CancellationToken ct)
    {
        await recuperacion.SolicitarAsync(r.Correo, ct);
        return NoContent();
    }

    [EnableRateLimiting(Limites.Cuenta)]
    [HttpPost("restablecer")]
    public async Task<IActionResult> Restablecer(RestablecerRequest r, CancellationToken ct)
    {
        await recuperacion.RestablecerAsync(r.Codigo, r.Contrasena, ct);
        return NoContent();
    }

    [Authorize]
    [HttpGet("yo")]
    public async Task<ActionResult<UsuarioResponse>> Yo(CancellationToken ct)
    {
        var id = User.UsuarioId();
        var usuario = await db.Usuarios.Include(u => u.Rol).SingleAsync(u => u.Id == id, ct);
        return Respuesta(usuario);
    }

    private SesionResponse Sesion(Usuario usuario)
    {
        var (token, expira) = tokens.Emitir(usuario);
        return new SesionResponse(token, expira, Respuesta(usuario));
    }

    private static UsuarioResponse Respuesta(Usuario u) =>
        new(u.Id, u.Nombre, u.Apellido, u.Iniciales, u.Correo, u.Rol.Nombre, u.CreadoEn.Year);
}
