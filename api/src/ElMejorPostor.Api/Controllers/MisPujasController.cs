using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ElMejorPostor.Api.Contratos;
using ElMejorPostor.Api.Datos;
using ElMejorPostor.Api.Dominio;
using ElMejorPostor.Api.Servicios;

namespace ElMejorPostor.Api.Controllers;

// Pantalla 05: una fila por subasta en la que el usuario pujó.
[Authorize]
[ApiController]
[Route("api/mis-pujas")]
public class MisPujasController(ElMejorPostorDbContext db, TimeProvider reloj) : ControllerBase
{
    [HttpGet]
    public async Task<IReadOnlyList<MiPujaResponse>> Listar(CancellationToken ct)
    {
        var yo = User.UsuarioId();
        var ahora = reloj.GetUtcNow().UtcDateTime;
        var filas = await db.Subastas.AsNoTracking()
            .Where(s => s.Pujas.Any(p => p.PostorId == yo))
            .Select(s => new
            {
                s.Id,
                s.Articulo.Titulo,
                Foto = s.Articulo.Imagenes.OrderBy(i => i.Orden).Select(i => i.Ruta).FirstOrDefault(),
                MiPuja = s.Pujas.Where(p => p.PostorId == yo).OrderByDescending(p => p.Id).Select(p => p.Monto).First(),
                // Puede no haber ninguna vigente si se descartaron todas (el ganador no pagó y nadie más alcanzó la reserva).
                Ultima = s.Pujas.Where(p => !p.Descartada).OrderByDescending(p => p.Monto).Select(p => new { p.Monto, p.PostorId }).FirstOrDefault(),
                s.IncrementoMinimo,
                s.FechaCierre,
                s.Estado,
                GanadorId = s.PujaGanadora != null ? s.PujaGanadora.PostorId : (long?)null,
            })
            .ToListAsync(ct);

        return filas
            .Select(f => new { f, Actual = f.Ultima?.Monto ?? f.MiPuja })
            .Select(x => new MiPujaResponse(x.f.Id, x.f.Titulo, x.f.Foto, x.f.MiPuja, x.Actual, x.Actual + x.f.IncrementoMinimo, x.f.FechaCierre,
                Situacion(x.f.Estado, x.f.FechaCierre, x.f.Ultima?.PostorId, x.f.GanadorId)))
            .OrderBy(f => f.FechaCierre)
            .ToList();

        string Situacion(EstadoSubasta estado, DateTime cierre, long? ultimoPostor, long? ganador) => estado switch
        {
            EstadoSubasta.Finalizada => ganador == yo ? "ganada" : "perdida",
            EstadoSubasta.Desierta or EstadoSubasta.Cancelada => "perdida",
            _ when cierre <= ahora => "cerrando",
            _ => ultimoPostor == yo ? "vas_ganando" : "superado",
        };
    }
}
