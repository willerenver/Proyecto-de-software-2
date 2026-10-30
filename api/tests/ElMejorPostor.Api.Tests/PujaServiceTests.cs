using Microsoft.EntityFrameworkCore;
using ElMejorPostor.Api.Servicios;

namespace ElMejorPostor.Api.Tests;

public class PujaServiceTests : IDisposable
{
    private readonly BaseDePrueba _base = new();

    private async Task<decimal> PujarAsync(long subastaId, long postorId, decimal monto)
    {
        await using var db = _base.Contexto();
        return (await new PujaService(db, _base.Reloj).RegistrarAsync(subastaId, postorId, monto)).Monto;
    }

    [Fact]
    public async Task La_primera_puja_debe_ser_al_menos_el_precio_de_salida()
    {
        var vendedor = await _base.UsuarioAsync("luis@prueba.test");
        var postor = await _base.UsuarioAsync("maria@prueba.test");
        var s = await _base.SubastaAsync(vendedor, salida: 4000);

        var error = await Assert.ThrowsAsync<ReglaException>(() => PujarAsync(s.Id, postor.Id, 3999));
        Assert.Contains("4,000", error.Message);
        Assert.Equal(4000, await PujarAsync(s.Id, postor.Id, 4000));
    }

    [Fact]
    public async Task Cada_puja_debe_superar_la_actual_por_el_incremento_minimo()
    {
        var vendedor = await _base.UsuarioAsync("luis@prueba.test");
        var a = await _base.UsuarioAsync("maria@prueba.test");
        var b = await _base.UsuarioAsync("jesus@prueba.test");
        var s = await _base.SubastaAsync(vendedor, salida: 4000, incremento: 500);
        await PujarAsync(s.Id, a.Id, 9000);

        await Assert.ThrowsAsync<ReglaException>(() => PujarAsync(s.Id, b.Id, 9000));
        await Assert.ThrowsAsync<ReglaException>(() => PujarAsync(s.Id, b.Id, 9499.99m));
        Assert.Equal(9500, await PujarAsync(s.Id, b.Id, 9500));
    }

    [Fact]
    public async Task El_vendedor_no_puede_pujar_en_su_propia_subasta()
    {
        var vendedor = await _base.UsuarioAsync("luis@prueba.test");
        var s = await _base.SubastaAsync(vendedor);

        var error = await Assert.ThrowsAsync<ReglaException>(() => PujarAsync(s.Id, vendedor.Id, 5000));
        Assert.Equal(403, error.EstadoHttp);
    }

    [Fact]
    public async Task No_se_aceptan_pujas_antes_del_inicio_ni_despues_del_cierre()
    {
        var vendedor = await _base.UsuarioAsync("luis@prueba.test");
        var postor = await _base.UsuarioAsync("maria@prueba.test");
        var futura = await _base.SubastaAsync(vendedor, inicio: BaseDePrueba.Hoy.AddHours(1), cierre: BaseDePrueba.Hoy.AddDays(3));
        var cerrada = await _base.SubastaAsync(vendedor, inicio: BaseDePrueba.Hoy.AddDays(-3), cierre: BaseDePrueba.Hoy.AddSeconds(-1));

        await Assert.ThrowsAsync<ReglaException>(() => PujarAsync(futura.Id, postor.Id, 5000));
        var error = await Assert.ThrowsAsync<ReglaException>(() => PujarAsync(cerrada.Id, postor.Id, 5000));
        Assert.Equal(409, error.EstadoHttp);
    }

    [Fact]
    public async Task Una_puja_en_los_ultimos_dos_minutos_extiende_el_cierre()
    {
        var vendedor = await _base.UsuarioAsync("luis@prueba.test");
        var postor = await _base.UsuarioAsync("maria@prueba.test");
        var s = await _base.SubastaAsync(vendedor, cierre: BaseDePrueba.Hoy.AddSeconds(30));

        await PujarAsync(s.Id, postor.Id, 5000);

        await using var db = _base.Contexto();
        var cierre = await db.Subastas.Where(x => x.Id == s.Id).Select(x => x.FechaCierre).SingleAsync();
        Assert.Equal(BaseDePrueba.Hoy.AddMinutes(2), cierre);
    }

    [Fact]
    public async Task Dos_pujas_simultaneas_del_mismo_monto_dejan_una_sola_registrada()
    {
        var vendedor = await _base.UsuarioAsync("luis@prueba.test");
        var a = await _base.UsuarioAsync("maria@prueba.test");
        var b = await _base.UsuarioAsync("jesus@prueba.test");
        var s = await _base.SubastaAsync(vendedor, salida: 4000);

        var resultados = await Task.WhenAll(
            Task.Run(() => Capturar(() => PujarAsync(s.Id, a.Id, 5000))),
            Task.Run(() => Capturar(() => PujarAsync(s.Id, b.Id, 5000))));

        Assert.Single(resultados, r => r is null);
        await using var db = _base.Contexto();
        Assert.Equal(1, await db.Pujas.CountAsync(p => p.SubastaId == s.Id));
    }

    private static async Task<Exception?> Capturar(Func<Task> accion)
    {
        try { await accion(); return null; }
        catch (Exception e) { return e; }
    }

    public void Dispose() => _base.Dispose();
}
