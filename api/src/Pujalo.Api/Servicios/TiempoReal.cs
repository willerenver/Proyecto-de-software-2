using Microsoft.AspNetCore.SignalR;

namespace Pujalo.Api.Servicios;

// Avisa al instante a quien está mirando una subasta (o el listado) que algo cambió: una puja, el cierre,
// el pase al siguiente postor, una edición o una cancelación. El navegador vuelve a pedir los datos a la API,
// así el hub no repite reglas ni expone nada que la API no muestre ya.
public class SubastasHub : Hub
{
    public const string Ruta = "/api/tiempo-real";
    public const string Todas = "todas";

    public static string Grupo(long subastaId) => $"subasta-{subastaId}";

    public Task Seguir(long subastaId) => Groups.AddToGroupAsync(Context.ConnectionId, Grupo(subastaId));

    public Task Dejar(long subastaId) => Groups.RemoveFromGroupAsync(Context.ConnectionId, Grupo(subastaId));

    public Task SeguirListado() => Groups.AddToGroupAsync(Context.ConnectionId, Todas);
}

public interface INotificadorSubastas
{
    Task CambioAsync(IEnumerable<long> subastas, CancellationToken ct = default);
}

public class NotificadorSubastas(IHubContext<SubastasHub> hub, ILogger<NotificadorSubastas> log) : INotificadorSubastas
{
    public async Task CambioAsync(IEnumerable<long> subastas, CancellationToken ct = default)
    {
        try
        {
            foreach (var id in subastas.Distinct())
                await hub.Clients.Group(SubastasHub.Grupo(id)).SendAsync("cambio", id, ct);
            await hub.Clients.Group(SubastasHub.Todas).SendAsync("listado", ct);
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            // El aviso es un extra: si falla, las pantallas se actualizan con la consulta periódica.
            log.LogWarning(e, "No se pudo avisar en tiempo real");
        }
    }
}
