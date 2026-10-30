using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ElMejorPostor.Api.Contratos;
using ElMejorPostor.Api.Datos;
using ElMejorPostor.Api.Servicios;

namespace ElMejorPostor.Api.Controllers;

// Las subastas que publicó el usuario. Si ya se vendió, incluye los datos del ganador para coordinar la entrega.
[Authorize]
[ApiController]
[Route("api/mis-ventas")]
public class MisVentasController(ElMejorPostorDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IReadOnlyList<MiVentaResponse>> Listar(CancellationToken ct)
    {
        var yo = User.UsuarioId();
        var filas = await db.Subastas.AsNoTracking()
            .Where(s => s.Articulo.VendedorId == yo)
            .Select(s => new MiVentaResponse(
                s.Id, s.Articulo.Titulo,
                s.Articulo.Imagenes.OrderBy(i => i.Orden).Select(i => i.Ruta).FirstOrDefault(),
                s.PrecioSalida,
                s.Pujas.OrderByDescending(p => p.Id).Select(p => (decimal?)p.Monto).FirstOrDefault(),
                s.Pujas.Count, s.FechaInicio, s.FechaCierre, s.Estado.ToString(),
                s.PujaGanadora == null ? null : new ContactoResponse(
                    s.PujaGanadora.Postor.Nombre + " " + s.PujaGanadora.Postor.Apellido,
                    s.PujaGanadora.Postor.Correo, s.PujaGanadora.Postor.Telefono),
                s.PlazoPago, s.PagoConfirmadoEn != null))
            .ToListAsync(ct);
        return filas.OrderByDescending(f => f.FechaCierre).ToList();
    }
}
