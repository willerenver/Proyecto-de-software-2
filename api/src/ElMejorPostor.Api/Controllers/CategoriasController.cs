using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ElMejorPostor.Api.Contratos;
using ElMejorPostor.Api.Datos;

namespace ElMejorPostor.Api.Controllers;

[ApiController]
[Route("api/categorias")]
public class CategoriasController(ElMejorPostorDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IReadOnlyList<CategoriaResponse>> Listar(CancellationToken ct)
    {
        var todas = await db.Categorias.AsNoTracking().Where(c => c.Activa).OrderBy(c => c.Id).ToListAsync(ct);
        return todas.Where(c => c.CategoriaPadreId is null)
            .Select(p => new CategoriaResponse(p.Id, p.Nombre,
                todas.Where(c => c.CategoriaPadreId == p.Id).Select(c => new CategoriaResponse(c.Id, c.Nombre, [])).ToList()))
            .ToList();
    }
}
