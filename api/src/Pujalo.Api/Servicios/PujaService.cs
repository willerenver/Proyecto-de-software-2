using Microsoft.EntityFrameworkCore;
using Pujalo.Api.Datos;
using Pujalo.Api.Dominio;

namespace Pujalo.Api.Servicios;

// Reglas RN-01 a RN-06 del documento de requerimientos.
public class PujaService(PujaloDbContext db, TimeProvider reloj)
{
    public static decimal MontoMinimo(Subasta subasta, decimal? pujaActual) =>
        pujaActual is null ? subasta.PrecioSalida : pujaActual.Value + subasta.IncrementoMinimo;

    public async Task<Puja> RegistrarAsync(long subastaId, long postorId, decimal monto, CancellationToken ct = default)
    {
        var ahora = reloj.GetUtcNow().UtcDateTime;
        var subasta = await db.Subastas.Include(s => s.Articulo)
            .FirstOrDefaultAsync(s => s.Id == subastaId, ct)
            ?? throw ReglaException.NoEncontrado("La subasta no existe.");

        if (subasta.Articulo.VendedorId == postorId)
            throw ReglaException.Prohibido("No puedes pujar en tu propia subasta.");
        if (subasta.FechaInicio > ahora)
            throw new ReglaException("La subasta todavía no ha empezado.");
        if (!subasta.AceptaPujas(ahora))
            throw ReglaException.Conflicto("La subasta ya cerró y no acepta más pujas.");
        if (decimal.Round(monto, 2) != monto)
            throw new ReglaException("El monto admite como máximo dos decimales.");

        // Las pujas solo suben, así que la más alta es la última registrada.
        var actual = await db.Pujas.Where(p => p.SubastaId == subastaId)
            .OrderByDescending(p => p.Id).Select(p => (decimal?)p.Monto).FirstOrDefaultAsync(ct);
        var minimo = MontoMinimo(subasta, actual);
        if (monto < minimo)
            throw new ReglaException($"La puja mínima es RD$ {minimo:N0}.");

        var puja = new Puja { SubastaId = subastaId, PostorId = postorId, Monto = monto, CreadaEn = ahora };
        db.Pujas.Add(puja);

        if (subasta.Estado == EstadoSubasta.Programada) subasta.Estado = EstadoSubasta.Activa;
        if (subasta.ExtenderAlFinal && subasta.FechaCierre - ahora < Subasta.VentanaExtension)
            subasta.FechaCierre = ahora + Subasta.VentanaExtension;

        // Si otra puja entró mientras tanto, Version ya cambió y esta falla: nunca quedan dos «pujas actuales».
        subasta.Version++;
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (Exception e) when (e is DbUpdateConcurrencyException or DbUpdateException)
        {
            db.ChangeTracker.Clear();
            throw ReglaException.Conflicto("Alguien pujó al mismo tiempo. Revisa la puja actual y vuelve a intentarlo.");
        }
        return puja;
    }
}
