using Microsoft.EntityFrameworkCore;
using Pujalo.Api.Datos;
using Pujalo.Api.Dominio;

namespace Pujalo.Api.Servicios;

// Activa las subastas que llegan a su inicio y cierra las que llegan a su fin (RF-11 y RF-12).
public class CierreService(PujaloDbContext db, TimeProvider reloj)
{
    public async Task<int> ProcesarAsync(CancellationToken ct = default)
    {
        var ahora = reloj.GetUtcNow().UtcDateTime;
        var cambios = 0;

        var porIniciar = await db.Subastas
            .Where(s => s.Estado == EstadoSubasta.Programada && s.FechaInicio <= ahora && s.FechaCierre > ahora)
            .ToListAsync(ct);
        foreach (var s in porIniciar) s.Estado = EstadoSubasta.Activa;
        cambios += porIniciar.Count;

        var porCerrar = await db.Subastas
            .Where(s => (s.Estado == EstadoSubasta.Programada || s.Estado == EstadoSubasta.Activa) && s.FechaCierre <= ahora)
            .ToListAsync(ct);
        foreach (var s in porCerrar)
        {
            var ganadora = await db.Pujas.Where(p => p.SubastaId == s.Id).OrderByDescending(p => p.Id).FirstOrDefaultAsync(ct);
            var alcanzaReserva = ganadora is not null && (s.PrecioReserva is null || ganadora.Monto >= s.PrecioReserva);
            s.Estado = alcanzaReserva ? EstadoSubasta.Finalizada : EstadoSubasta.Desierta;
            s.PujaGanadoraId = alcanzaReserva ? ganadora!.Id : null;
            s.CerradaEn = ahora;
            s.Version++;
        }
        cambios += porCerrar.Count;

        if (cambios > 0)
        {
            try
            {
                await db.SaveChangesAsync(ct);
            }
            catch (DbUpdateConcurrencyException)
            {
                // Entró una puja de último momento (y quizá extendió el cierre): se reintenta en la próxima vuelta.
                db.ChangeTracker.Clear();
                return 0;
            }
        }
        return cambios;
    }
}

// Ejecuta CierreService cada 15 segundos mientras la API está en marcha.
public class CierreWorker(IServiceScopeFactory scopes, ILogger<CierreWorker> log) : BackgroundService
{
    private static readonly TimeSpan Intervalo = TimeSpan.FromSeconds(15);

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        using var temporizador = new PeriodicTimer(Intervalo);
        do
        {
            try
            {
                using var scope = scopes.CreateScope();
                await scope.ServiceProvider.GetRequiredService<CierreService>().ProcesarAsync(ct);
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                log.LogError(e, "Falló el cierre automático de subastas");
            }
        } while (await temporizador.WaitForNextTickAsync(ct));
    }
}
