using Microsoft.EntityFrameworkCore;
using Pujalo.Api.Datos;
using Pujalo.Api.Dominio;

namespace Pujalo.Api.Servicios;

// Reglas RN-01 a RN-06 del documento de requerimientos, más la puja automática.
public class PujaService(PujaloDbContext db, TimeProvider reloj, Avisos? avisos = null)
{
    private const int MaximoDeRondas = 20;

    public static decimal MontoMinimo(Subasta subasta, decimal? pujaActual) =>
        pujaActual is null ? subasta.PrecioSalida : pujaActual.Value + subasta.IncrementoMinimo;

    public async Task<Puja> RegistrarAsync(long subastaId, long postorId, decimal monto, CancellationToken ct = default)
    {
        var (subasta, ahora) = await AbiertaParaAsync(subastaId, postorId, ct);
        if (decimal.Round(monto, 2) != monto)
            throw new ReglaException("El monto admite como máximo dos decimales.");

        var estado = await EstadoAsync(subasta, ct);
        var minimo = MontoMinimo(subasta, estado.Monto);
        if (monto < minimo)
            throw new ReglaException($"La puja mínima es RD$ {minimo:N0}.");

        var puja = Pujar(subasta, estado, postorId, monto, automatica: false, ahora);
        await ResponderAutomaticasAsync(subasta, estado, ahora, ct);
        await GuardarAsync(subasta, estado, ct);
        return puja;
    }

    // Crea o cambia la puja automática del postor y, si no va ganando, puja por él enseguida.
    public async Task ProgramarAutomaticaAsync(long subastaId, long postorId, decimal maximo, CancellationToken ct = default)
    {
        var (subasta, ahora) = await AbiertaParaAsync(subastaId, postorId, ct);
        if (decimal.Round(maximo, 2) != maximo)
            throw new ReglaException("El máximo admite como máximo dos decimales.");

        var estado = await EstadoAsync(subasta, ct);
        var minimo = estado.LiderId == postorId ? estado.Monto!.Value + subasta.IncrementoMinimo : MontoMinimo(subasta, estado.Monto);
        if (maximo < minimo)
            throw new ReglaException($"El máximo debe ser de RD$ {minimo:N0} o más.");

        var automatica = await db.PujasAutomaticas.FirstOrDefaultAsync(a => a.SubastaId == subastaId && a.PostorId == postorId, ct);
        if (automatica is null)
        {
            automatica = new PujaAutomatica { SubastaId = subastaId, PostorId = postorId };
            db.PujasAutomaticas.Add(automatica);
        }
        automatica.Maximo = maximo;
        automatica.Activa = true;
        automatica.CreadaEn = ahora;
        automatica.Postor = await db.Usuarios.SingleAsync(u => u.Id == postorId, ct);

        await ResponderAutomaticasAsync(subasta, estado, ahora, ct);
        await GuardarAsync(subasta, estado, ct);
    }

    public async Task CancelarAutomaticaAsync(long subastaId, long postorId, CancellationToken ct = default)
    {
        var automatica = await db.PujasAutomaticas.FirstOrDefaultAsync(a => a.SubastaId == subastaId && a.PostorId == postorId && a.Activa, ct)
            ?? throw ReglaException.NoEncontrado("No tienes una puja automática en esta subasta.");
        automatica.Activa = false;
        await db.SaveChangesAsync(ct);
    }

    private async Task<(Subasta, DateTime)> AbiertaParaAsync(long subastaId, long postorId, CancellationToken ct)
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
        return (subasta, ahora);
    }

    // Quién va ganando y con cuánto, y quién iba ganando antes de esta operación (para avisarle).
    private sealed class Estado
    {
        public decimal? Monto;
        public long? LiderId;
        public Usuario? LiderInicial;
        public readonly List<PujaAutomatica> Agotadas = [];
    }

    private async Task<Estado> EstadoAsync(Subasta subasta, CancellationToken ct)
    {
        // Las pujas solo suben, así que la más alta es la última registrada.
        var lider = await db.Pujas.Where(p => p.SubastaId == subasta.Id)
            .OrderByDescending(p => p.Id).Select(p => new { p.Monto, p.Postor }).FirstOrDefaultAsync(ct);
        return new Estado { Monto = lider?.Monto, LiderId = lider?.Postor.Id, LiderInicial = lider?.Postor };
    }

    private Puja Pujar(Subasta subasta, Estado estado, long postorId, decimal monto, bool automatica, DateTime ahora)
    {
        var puja = new Puja { SubastaId = subasta.Id, PostorId = postorId, Monto = monto, CreadaEn = ahora, Automatica = automatica };
        db.Pujas.Add(puja);
        estado.Monto = monto;
        estado.LiderId = postorId;
        if (subasta.Estado == EstadoSubasta.Programada) subasta.Estado = EstadoSubasta.Activa;
        if (subasta.ExtenderAlFinal && subasta.FechaCierre - ahora < Subasta.VentanaExtension)
            subasta.FechaCierre = ahora + Subasta.VentanaExtension;
        return puja;
    }

    // Las pujas automáticas responden hasta que nadie más puede superar al que va ganando.
    // Si el que va ganando también tiene automática, se resuelve de una vez: gana el máximo mayor
    // (en empate, la que se programó primero) por el incremento mínimo sobre el máximo del otro.
    private async Task ResponderAutomaticasAsync(Subasta subasta, Estado estado, DateTime ahora, CancellationToken ct)
    {
        var activas = await db.PujasAutomaticas.Include(a => a.Postor)
            .Where(a => a.SubastaId == subasta.Id && a.Activa).ToListAsync(ct);
        // Las que se acaban de crear o cambiar en esta operación todavía no están en la base.
        foreach (var nueva in db.ChangeTracker.Entries<PujaAutomatica>().Select(e => e.Entity))
            if (nueva.SubastaId == subasta.Id && nueva.Activa && !activas.Contains(nueva)) activas.Add(nueva);

        var inc = subasta.IncrementoMinimo;
        for (var ronda = 0; ronda < MaximoDeRondas; ronda++)
        {
            var necesario = MontoMinimo(subasta, estado.Monto);
            var retador = activas.Where(a => a.Activa && a.PostorId != estado.LiderId && a.Maximo >= necesario)
                .OrderByDescending(a => a.Maximo).ThenBy(a => a.CreadaEn).FirstOrDefault();

            // Automáticas que ya no alcanzan: se desactivan y se avisa a su dueño.
            foreach (var a in activas.Where(a => a.Activa && a.PostorId != estado.LiderId && a.Maximo < necesario))
            {
                a.Activa = false;
                estado.Agotadas.Add(a);
            }
            if (retador is null) return;

            var defensa = activas.FirstOrDefault(a => a.Activa && a.PostorId == estado.LiderId);
            if (defensa is null)
            {
                Pujar(subasta, estado, retador.PostorId, necesario, automatica: true, ahora);
                continue;
            }

            var ganaRetador = retador.Maximo > defensa.Maximo || (retador.Maximo == defensa.Maximo && retador.CreadaEn < defensa.CreadaEn);
            var (ganador, perdedor) = ganaRetador ? (retador, defensa) : (defensa, retador);
            // El que pierde llega a su máximo (si es más que lo actual); el que gana lo supera por el incremento, sin pasarse del suyo.
            if (perdedor.Maximo > estado.Monto && perdedor.Maximo < ganador.Maximo)
                Pujar(subasta, estado, perdedor.PostorId, perdedor.Maximo, automatica: true, ahora);
            perdedor.Activa = false;
            estado.Agotadas.Add(perdedor);
            var monto = Math.Min(ganador.Maximo, Math.Max(MontoMinimo(subasta, estado.Monto), perdedor.Maximo + inc));
            if (estado.Monto is null || monto > estado.Monto)
                Pujar(subasta, estado, ganador.PostorId, monto, automatica: true, ahora);
        }
    }

    private async Task GuardarAsync(Subasta subasta, Estado estado, CancellationToken ct)
    {
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

        if (avisos is null) return;
        var avisados = new HashSet<long>();
        foreach (var a in estado.Agotadas.Where(a => a.PostorId != estado.LiderId))
            if (avisados.Add(a.PostorId)) avisos.SuperaronTuMaximo(a.Postor, subasta, a.Maximo, estado.Monto!.Value);
        if (estado.LiderInicial is { } anterior && anterior.Id != estado.LiderId && avisados.Add(anterior.Id))
            avisos.TeSuperaron(anterior, subasta, estado.Monto!.Value);
        await avisos.EnviarAsync(ct);
    }
}
