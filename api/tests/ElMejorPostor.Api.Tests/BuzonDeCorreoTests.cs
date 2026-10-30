using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using ElMejorPostor.Api.Dominio;
using ElMejorPostor.Api.Servicios;

namespace ElMejorPostor.Api.Tests;

public class BuzonDeCorreoTests : IDisposable
{
    private readonly BaseDePrueba _base = new();

    private sealed class ServidorDePrueba : IServidorCorreo
    {
        public bool Falla { get; set; }
        public List<string> Entregados { get; } = [];

        public Task EntregarAsync(string para, string asunto, string texto, CancellationToken ct)
        {
            if (Falla) throw new InvalidOperationException("SMTP caído");
            Entregados.Add($"{para}: {asunto}");
            return Task.CompletedTask;
        }
    }

    // Cada llamada usa un contexto nuevo, como si la API se hubiera reiniciado entre una y otra.
    private async Task<int> EntregarAsync(ServidorDePrueba servidor)
    {
        await using var db = _base.Contexto();
        return await new BuzonDeCorreo(db, servidor, _base.Reloj, NullLogger<BuzonDeCorreo>.Instance).EntregarAsync(CancellationToken.None);
    }

    private async Task GuardarAsync(params (string, string, string)[] correos)
    {
        await using var db = _base.Contexto();
        await new BuzonDeCorreo(db, new ServidorDePrueba(), _base.Reloj, NullLogger<BuzonDeCorreo>.Instance).GuardarAsync(correos, CancellationToken.None);
    }

    private async Task<List<CorreoPendiente>> PendientesAsync()
    {
        await using var db = _base.Contexto();
        return await db.CorreosPendientes.ToListAsync();
    }

    [Fact]
    public async Task Entrega_los_correos_guardados_y_los_borra()
    {
        await GuardarAsync(("maria@prueba.test", "Te superaron", "…"), ("luis@prueba.test", "Vendiste", "…"));
        var servidor = new ServidorDePrueba();

        await EntregarAsync(servidor);

        Assert.Equal(["maria@prueba.test: Te superaron", "luis@prueba.test: Vendiste"], servidor.Entregados);
        Assert.Empty(await PendientesAsync());
    }

    [Fact]
    public async Task Si_el_servidor_falla_se_reintenta_mas_tarde_aunque_la_API_se_reinicie()
    {
        await GuardarAsync(("maria@prueba.test", "Ganaste", "…"));
        await EntregarAsync(new ServidorDePrueba { Falla = true });

        var pendiente = Assert.Single(await PendientesAsync());
        Assert.Equal(1, pendiente.Intentos);
        Assert.Equal(BaseDePrueba.Hoy.AddMinutes(1), pendiente.ProximoIntento);

        var servidor = new ServidorDePrueba();
        _base.Reloj.Ahora = BaseDePrueba.Hoy.AddSeconds(30);
        await EntregarAsync(servidor);
        Assert.Empty(servidor.Entregados);

        _base.Reloj.Ahora = BaseDePrueba.Hoy.AddMinutes(1);
        await EntregarAsync(servidor);
        Assert.Equal(["maria@prueba.test: Ganaste"], servidor.Entregados);
        Assert.Empty(await PendientesAsync());
    }

    [Fact]
    public async Task Despues_del_maximo_de_intentos_se_descarta()
    {
        await GuardarAsync(("maria@prueba.test", "Ganaste", "…"));
        var servidor = new ServidorDePrueba { Falla = true };

        for (var i = 0; i < CorreoPendiente.MaximoDeIntentos; i++)
        {
            _base.Reloj.Ahora = BaseDePrueba.Hoy.AddDays(i);
            await EntregarAsync(servidor);
        }

        Assert.Empty(await PendientesAsync());
    }

    public void Dispose() => _base.Dispose();
}
