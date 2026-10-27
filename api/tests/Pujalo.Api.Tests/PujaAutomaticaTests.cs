using Microsoft.EntityFrameworkCore;
using Pujalo.Api.Dominio;
using Pujalo.Api.Servicios;

namespace Pujalo.Api.Tests;

public class PujaAutomaticaTests : IDisposable
{
    private readonly BaseDePrueba _base = new();

    private async Task PujarAsync(long subastaId, long postorId, decimal monto)
    {
        await using var db = _base.Contexto();
        await new PujaService(db, _base.Reloj).RegistrarAsync(subastaId, postorId, monto);
    }

    private async Task AutomaticaAsync(long subastaId, long postorId, decimal maximo)
    {
        await using var db = _base.Contexto();
        await new PujaService(db, _base.Reloj).ProgramarAutomaticaAsync(subastaId, postorId, maximo);
    }

    private async Task<List<(long Postor, decimal Monto, bool Automatica)>> PujasAsync(long subastaId)
    {
        await using var db = _base.Contexto();
        return (await db.Pujas.Where(p => p.SubastaId == subastaId).OrderBy(p => p.Id).ToListAsync())
            .Select(p => (p.PostorId, p.Monto, p.Automatica)).ToList();
    }

    [Fact]
    public async Task Sin_pujas_la_automatica_entra_con_el_precio_de_salida()
    {
        var vendedor = await _base.UsuarioAsync("luis@prueba.test");
        var a = await _base.UsuarioAsync("maria@prueba.test");
        var s = await _base.SubastaAsync(vendedor, salida: 4000, incremento: 500);

        await AutomaticaAsync(s.Id, a.Id, 9000);

        Assert.Equal([(a.Id, 4000m, true)], await PujasAsync(s.Id));
    }

    [Fact]
    public async Task Responde_a_una_puja_manual_solo_con_el_incremento_minimo()
    {
        var vendedor = await _base.UsuarioAsync("luis@prueba.test");
        var a = await _base.UsuarioAsync("maria@prueba.test");
        var b = await _base.UsuarioAsync("jesus@prueba.test");
        var s = await _base.SubastaAsync(vendedor, salida: 4000, incremento: 500);
        await AutomaticaAsync(s.Id, a.Id, 9000);

        await PujarAsync(s.Id, b.Id, 6000);

        var pujas = await PujasAsync(s.Id);
        Assert.Equal((a.Id, 6500m, true), pujas[^1]);
    }

    [Fact]
    public async Task Si_la_manual_supera_el_maximo_la_automatica_se_desactiva()
    {
        var vendedor = await _base.UsuarioAsync("luis@prueba.test");
        var a = await _base.UsuarioAsync("maria@prueba.test");
        var b = await _base.UsuarioAsync("jesus@prueba.test");
        var s = await _base.SubastaAsync(vendedor, salida: 4000, incremento: 500);
        await AutomaticaAsync(s.Id, a.Id, 9000);

        await PujarAsync(s.Id, b.Id, 8800);

        Assert.Equal((b.Id, 8800m, false), (await PujasAsync(s.Id))[^1]);
        await using var db = _base.Contexto();
        Assert.False((await db.PujasAutomaticas.SingleAsync()).Activa);
    }

    [Fact]
    public async Task Entre_dos_automaticas_gana_el_maximo_mayor_por_el_incremento()
    {
        var vendedor = await _base.UsuarioAsync("luis@prueba.test");
        var a = await _base.UsuarioAsync("maria@prueba.test");
        var b = await _base.UsuarioAsync("jesus@prueba.test");
        var s = await _base.SubastaAsync(vendedor, salida: 4000, incremento: 500);
        await AutomaticaAsync(s.Id, a.Id, 7000);

        await AutomaticaAsync(s.Id, b.Id, 12000);

        var pujas = await PujasAsync(s.Id);
        Assert.Equal((a.Id, 7000m, true), pujas[^2]);
        Assert.Equal((b.Id, 7500m, true), pujas[^1]);
    }

    [Fact]
    public async Task En_empate_gana_la_que_se_programo_primero()
    {
        var vendedor = await _base.UsuarioAsync("luis@prueba.test");
        var a = await _base.UsuarioAsync("maria@prueba.test");
        var b = await _base.UsuarioAsync("jesus@prueba.test");
        var s = await _base.SubastaAsync(vendedor, salida: 4000, incremento: 500);
        await AutomaticaAsync(s.Id, a.Id, 8000);
        _base.Reloj.Ahora = BaseDePrueba.Hoy.AddMinutes(5);

        await AutomaticaAsync(s.Id, b.Id, 8000);

        Assert.Equal((a.Id, 8000m, true), (await PujasAsync(s.Id))[^1]);
    }

    [Fact]
    public async Task El_vendedor_no_puede_programar_una_automatica()
    {
        var vendedor = await _base.UsuarioAsync("luis@prueba.test");
        var s = await _base.SubastaAsync(vendedor);

        await Assert.ThrowsAsync<ReglaException>(() => AutomaticaAsync(s.Id, vendedor.Id, 9000));
    }

    [Fact]
    public async Task La_automatica_de_una_cuenta_desactivada_no_puja()
    {
        var vendedor = await _base.UsuarioAsync("luis@prueba.test");
        var a = await _base.UsuarioAsync("maria@prueba.test");
        var b = await _base.UsuarioAsync("jesus@prueba.test");
        var s = await _base.SubastaAsync(vendedor, salida: 4000, incremento: 500);
        await AutomaticaAsync(s.Id, a.Id, 9000);
        await using (var db = _base.Contexto())
            await db.Usuarios.Where(u => u.Id == a.Id).ExecuteUpdateAsync(u => u.SetProperty(x => x.Activo, false));

        await PujarAsync(s.Id, b.Id, 5000);

        Assert.Equal((b.Id, 5000m, false), (await PujasAsync(s.Id))[^1]);
    }

    [Fact]
    public async Task Una_automatica_desactivada_ya_no_responde()
    {
        var vendedor = await _base.UsuarioAsync("luis@prueba.test");
        var a = await _base.UsuarioAsync("maria@prueba.test");
        var b = await _base.UsuarioAsync("jesus@prueba.test");
        var s = await _base.SubastaAsync(vendedor, salida: 4000, incremento: 500);
        await AutomaticaAsync(s.Id, a.Id, 9000);
        await using (var db = _base.Contexto())
            await new PujaService(db, _base.Reloj).CancelarAutomaticaAsync(s.Id, a.Id);

        await PujarAsync(s.Id, b.Id, 5000);

        Assert.Equal((b.Id, 5000m, false), (await PujasAsync(s.Id))[^1]);
    }

    public void Dispose() => _base.Dispose();
}
