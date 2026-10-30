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

    private async Task RecibirAsync(ServidorDePrueba servidor, params CorreoEnCola[] correos)
    {
        await using var db = _base.Contexto();
        await new BuzonDeCorreo(db, servidor, _base.Reloj, NullLogger<BuzonDeCorreo>.Instance).RecibirAsync(correos, CancellationToken.None);
    }

    private Task GuardarAsync(params (string Para, string Asunto, string Texto)[] correos) =>
        RecibirAsync(new ServidorDePrueba(), correos.Select(c => new CorreoEnCola(c.Para, c.Asunto, c.Texto)).ToArray());

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

    [Fact]
    public async Task Un_correo_con_un_codigo_secreto_se_entrega_sin_guardarlo_en_la_base()
    {
        var servidor = new ServidorDePrueba();
        await RecibirAsync(servidor, new CorreoEnCola("maria@prueba.test", "Restablece tu contraseña", "…codigo=abc", ContieneSecreto: true));

        Assert.Equal(["maria@prueba.test: Restablece tu contraseña"], servidor.Entregados);
        Assert.Empty(await PendientesAsync());
    }

    [Fact]
    public async Task Si_falla_el_envio_de_un_codigo_secreto_tampoco_se_guarda()
    {
        await RecibirAsync(new ServidorDePrueba { Falla = true }, new CorreoEnCola("maria@prueba.test", "Restablece tu contraseña", "…codigo=abc", ContieneSecreto: true));

        Assert.Empty(await PendientesAsync());
    }

    public void Dispose() => _base.Dispose();
}
