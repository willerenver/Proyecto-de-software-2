using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ElMejorPostor.Api.Contratos;
using ElMejorPostor.Api.Datos;
using ElMejorPostor.Api.Servicios;

namespace ElMejorPostor.Api.Controllers;

// Acciones del vendedor sobre su propia subasta. Las reglas están en VendedorService.
[Authorize]
[ApiController]
[Route("api/subastas/{id:long}")]
public class MisSubastasController(ElMejorPostorDbContext db, VendedorService vendedor) : ControllerBase
{
    [HttpGet("edicion")]
    public async Task<ActionResult<SubastaEdicionResponse>> Edicion(long id, CancellationToken ct)
    {
        var s = await db.Subastas.AsNoTracking().Include(x => x.Articulo).ThenInclude(a => a.Imagenes)
            .FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw ReglaException.NoEncontrado("La subasta no existe.");
        if (s.Articulo.VendedorId != User.UsuarioId())
            throw ReglaException.Prohibido("Solo el vendedor puede modificar la subasta.");
        var a = s.Articulo;
        return new SubastaEdicionResponse(
            s.Id, a.Titulo, a.Descripcion, a.Condicion, a.CategoriaId, s.PrecioSalida, s.IncrementoMinimo, s.PrecioReserva,
            s.FechaInicio, (int)Math.Round((s.FechaCierre - s.FechaInicio).TotalDays), s.ExtenderAlFinal, a.PermiteRetiro, a.PermiteEnvio,
            s.Estado.ToString(), a.Imagenes.OrderBy(i => i.Orden).Select(i => i.Ruta).ToList());
    }

    [HttpPut]
    public async Task<IActionResult> Editar(long id, CrearSubastaRequest r, CancellationToken ct)
    {
        await vendedor.EditarAsync(id, User.UsuarioId(), r, ct);
        return NoContent();
    }

    [HttpPost("retirar")]
    public async Task<IActionResult> Retirar(long id, CancellationToken ct)
    {
        await vendedor.RetirarAsync(id, User.UsuarioId(), ct);
        return NoContent();
    }

    [HttpDelete("imagenes")]
    public async Task<IActionResult> QuitarImagen(long id, [FromQuery] string ruta, CancellationToken ct)
    {
        await vendedor.QuitarImagenAsync(id, User.UsuarioId(), ruta, ct);
        return NoContent();
    }

    [HttpPost("confirmar-pago")]
    public async Task<IActionResult> ConfirmarPago(long id, CancellationToken ct)
    {
        await vendedor.ConfirmarPagoAsync(id, User.UsuarioId(), ct);
        return NoContent();
    }
}
