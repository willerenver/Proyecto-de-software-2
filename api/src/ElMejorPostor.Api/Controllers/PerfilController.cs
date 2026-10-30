using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using ElMejorPostor.Api.Contratos;
using ElMejorPostor.Api.Datos;
using ElMejorPostor.Api.Dominio;
using ElMejorPostor.Api.Servicios;

namespace ElMejorPostor.Api.Controllers;

// «Mi perfil»: datos de contacto y cambio de contraseña. El correo y la cédula identifican la cuenta y no se cambian aquí.
[Authorize]
[ApiController]
[Route("api/perfil")]
public class PerfilController(ElMejorPostorDbContext db, IPasswordHasher<Usuario> hasher, TokenService tokens) : ControllerBase
{
    [HttpGet]
    public async Task<PerfilResponse> Ver(CancellationToken ct) => Respuesta(await YoAsync(ct));

    [HttpPut]
    public async Task<SesionResponse> Editar(EditarPerfilRequest r, CancellationToken ct)
    {
        var u = await YoAsync(ct);
        u.Nombre = r.Nombre.Trim();
        u.Apellido = r.Apellido.Trim();
        u.Telefono = r.Telefono.Trim();
        await db.SaveChangesAsync(ct);
        // El nombre va en el token y en la sesión del navegador: se emite uno nuevo.
        return Sesion(u);
    }

    // Pide la contraseña actual. Cierra las demás sesiones y devuelve un token nuevo para seguir en esta.
    [EnableRateLimiting(Limites.Cuenta)]
    [HttpPost("contrasena")]
    public async Task<SesionResponse> CambiarContrasena(CambiarContrasenaRequest r, CancellationToken ct)
    {
        var u = await YoAsync(ct);
        if (hasher.VerifyHashedPassword(u, u.ContrasenaHash, r.Actual) == PasswordVerificationResult.Failed)
            throw new ReglaException("La contraseña actual no es correcta.");
        if (r.Actual == r.Nueva)
            throw new ReglaException("La contraseña nueva debe ser distinta de la actual.");
        u.ContrasenaHash = hasher.HashPassword(u, r.Nueva);
        u.VersionSesion++;
        await db.SaveChangesAsync(ct);
        return Sesion(u);
    }

    private async Task<Usuario> YoAsync(CancellationToken ct)
    {
        var id = User.UsuarioId();
        return await db.Usuarios.Include(u => u.Rol).SingleAsync(u => u.Id == id, ct);
    }

    private SesionResponse Sesion(Usuario u)
    {
        var (token, expira) = tokens.Emitir(u);
        return new SesionResponse(token, expira, new UsuarioResponse(u.Id, u.Nombre, u.Apellido, u.Iniciales, u.Correo, u.Rol.Nombre, u.CreadoEn.Year));
    }

    // La cédula se muestra enmascarada: «001-****567-8».
    private static PerfilResponse Respuesta(Usuario u) =>
        new(u.Id, u.Nombre, u.Apellido, u.Correo, u.Telefono,
            u.Cedula is { Length: 11 } c ? $"{c[..3]}-****{c[7..10]}-{c[10]}" : null, u.CreadoEn.Year);
}
