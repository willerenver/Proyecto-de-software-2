using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pujalo.Api.Contratos;
using Pujalo.Api.Datos;
using Pujalo.Api.Dominio;
using Pujalo.Api.Servicios;

namespace Pujalo.Api.Controllers;

// RF-15 y RF-16: administración básica. Solo responde a usuarios con rol «administrador».
[Authorize(Roles = Rol.Administrador)]
[ApiController]
[Route("api/admin")]
public class AdminController(PujaloDbContext db, TimeProvider reloj) : ControllerBase
{
    [HttpGet("usuarios")]
    public async Task<IReadOnlyList<UsuarioAdminResponse>> Usuarios(CancellationToken ct) =>
        await db.Usuarios.AsNoTracking().OrderBy(u => u.Id)
            .Select(u => new UsuarioAdminResponse(u.Id, u.Nombre + " " + u.Apellido, u.Correo, u.Rol.Nombre, u.Activo, u.CreadoEn))
            .ToListAsync(ct);

    [HttpPatch("usuarios/{id:long}")]
    public async Task<IActionResult> CambiarActivo(long id, CambiarActivoRequest r, CancellationToken ct)
    {
        var usuario = await db.Usuarios.FindAsync([id], ct) ?? throw ReglaException.NoEncontrado("El usuario no existe.");
        if (usuario.Id == User.UsuarioId() && !r.Activo)
            throw new ReglaException("No puedes desactivar tu propia cuenta.");
        usuario.Activo = r.Activo;
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    // Cancela una subasta que aún no ha cerrado. Las pujas se conservan en el historial.
    [HttpPost("subastas/{id:long}/cancelar")]
    public async Task<IActionResult> Cancelar(long id, CancellationToken ct)
    {
        var subasta = await db.Subastas.FindAsync([id], ct) ?? throw ReglaException.NoEncontrado("La subasta no existe.");
        if (subasta.EsFinal)
            throw ReglaException.Conflicto("La subasta ya terminó; no se puede cancelar.");
        subasta.Estado = EstadoSubasta.Cancelada;
        subasta.CerradaEn = reloj.GetUtcNow().UtcDateTime;
        subasta.Version++;
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}
