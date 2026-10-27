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
public class AdminController(PujaloDbContext db, TimeProvider reloj, INotificadorSubastas tiempoReal) : ControllerBase
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
        if (usuario.Activo && !r.Activo)
        {
            usuario.VersionSesion++;
            // Sus pujas automáticas dejan de pujar por él.
            await db.PujasAutomaticas.Where(a => a.PostorId == id && a.Activa).ExecuteUpdateAsync(a => a.SetProperty(x => x.Activa, false), ct);
        }
        usuario.Activo = r.Activo;
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    // Categorías activas e inactivas, con cuántos artículos tiene cada una.
    [HttpGet("categorias")]
    public async Task<IReadOnlyList<CategoriaAdminResponse>> Categorias(CancellationToken ct) =>
        await db.Categorias.AsNoTracking().OrderBy(c => c.CategoriaPadreId ?? c.Id).ThenBy(c => c.CategoriaPadreId != null).ThenBy(c => c.Nombre)
            .Select(c => new CategoriaAdminResponse(c.Id, c.Nombre, c.CategoriaPadreId, c.Activa, db.Articulos.Count(a => a.CategoriaId == c.Id)))
            .ToListAsync(ct);

    [HttpPost("categorias")]
    public async Task<ActionResult<CategoriaAdminResponse>> CrearCategoria(GuardarCategoriaRequest r, CancellationToken ct)
    {
        var categoria = new Categoria();
        await AplicarAsync(categoria, r, ct);
        db.Categorias.Add(categoria);
        await db.SaveChangesAsync(ct);
        return new CategoriaAdminResponse(categoria.Id, categoria.Nombre, categoria.CategoriaPadreId, categoria.Activa, 0);
    }

    // Renombrar, mover o desactivar. Una categoría desactivada no se ofrece al publicar,
    // pero sus subastas siguen visibles.
    [HttpPut("categorias/{id:int}")]
    public async Task<IActionResult> EditarCategoria(int id, GuardarCategoriaRequest r, CancellationToken ct)
    {
        var categoria = await db.Categorias.FindAsync([id], ct) ?? throw ReglaException.NoEncontrado("La categoría no existe.");
        await AplicarAsync(categoria, r, ct);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    private async Task AplicarAsync(Categoria categoria, GuardarCategoriaRequest r, CancellationToken ct)
    {
        var nombre = r.Nombre.Trim();
        if (r.CategoriaPadreId is not null)
        {
            if (r.CategoriaPadreId == categoria.Id)
                throw new ReglaException("Una categoría no puede ser su propia subcategoría.");
            var padre = await db.Categorias.AsNoTracking().FirstOrDefaultAsync(c => c.Id == r.CategoriaPadreId, ct)
                ?? throw new ReglaException("La categoría principal no existe.");
            if (padre.CategoriaPadreId is not null)
                throw new ReglaException("Solo se admite un nivel de subcategoría.");
            if (categoria.Id != 0 && await db.Categorias.AnyAsync(c => c.CategoriaPadreId == categoria.Id, ct))
                throw new ReglaException("Esta categoría tiene subcategorías; no puede pasar a ser subcategoría.");
        }
        if (await db.Categorias.AnyAsync(c => c.Id != categoria.Id && c.Nombre == nombre && c.CategoriaPadreId == r.CategoriaPadreId, ct))
            throw ReglaException.Conflicto("Ya existe una categoría con ese nombre en el mismo lugar.");
        categoria.Nombre = nombre;
        categoria.CategoriaPadreId = r.CategoriaPadreId;
        categoria.Activa = r.Activa;
    }

    // Todas las subastas, primero las que siguen abiertas.
    [HttpGet("subastas")]
    public async Task<IReadOnlyList<SubastaResumenResponse>> Subastas(CancellationToken ct) =>
        (await db.Subastas.AsNoTracking().Select(SubastasController.Resumen()).ToListAsync(ct))
            .OrderBy(s => s.Estado is "Programada" or "Activa" ? 0 : 1).ThenBy(s => s.FechaCierre)
            .ToList();

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
        await tiempoReal.CambioAsync([subasta.Id], ct);
        return NoContent();
    }
}
