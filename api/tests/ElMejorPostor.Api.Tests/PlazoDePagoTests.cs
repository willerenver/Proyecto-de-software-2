using Microsoft.EntityFrameworkCore;
using ElMejorPostor.Api.Dominio;
using ElMejorPostor.Api.Servicios;

namespace ElMejorPostor.Api.Tests;

public class PlazoDePagoTests : IDisposable
{
    private readonly BaseDePrueba _base = new();

    private async Task PujarAsync(long subastaId, long postorId, decimal monto)
    {
        await using var db = _base.Contexto();
        await new PujaService(db, _base.Reloj).RegistrarAsync(subastaId, postorId, monto);
    }

    private async Task ProcesarEnAsync(DateTime momento)
    {
        _base.Reloj.Ahora = momento;
        await using var db = _base.Contexto();
        await new CierreService(db, _base.Reloj).ProcesarAsync();
    }

    private async Task<Subasta> LeerAsync(long id)
    {
        await using var db = _base.Contexto();
        return await db.Subastas.Include(s => s.PujaGanadora).SingleAsync(s => s.Id == id);
    }

    private static readonly DateTime Cierre = BaseDePrueba.Hoy.AddHours(1);

    [Fact]
    public async Task Al_cerrar_el_ganador_tiene_48_horas_para_pagar()
    {
        var vendedor = await _base.UsuarioAsync("luis@prueba.test");
        var a = await _base.UsuarioAsync("maria@prueba.test");
        var s = await _base.SubastaAsync(vendedor, cierre: Cierre);
        await PujarAsync(s.Id, a.Id, 5000);

        await ProcesarEnAsync(Cierre);

        Assert.Equal(Cierre.AddHours(48), (await LeerAsync(s.Id)).PlazoPago);
    }

    [Fact]
    public async Task Si_no_paga_a_tiempo_gana_el_siguiente_postor()
    {
        var vendedor = await _base.UsuarioAsync("luis@prueba.test");
        var a = await _base.UsuarioAsync("maria@prueba.test");
        var b = await _base.UsuarioAsync("jesus@prueba.test");
        var s = await _base.SubastaAsync(vendedor, cierre: Cierre);
        await PujarAsync(s.Id, b.Id, 4000);
        await PujarAsync(s.Id, a.Id, 5000);
        await PujarAsync(s.Id, b.Id, 6000);
        await PujarAsync(s.Id, a.Id, 7000);
        await ProcesarEnAsync(Cierre);

        await ProcesarEnAsync(Cierre.AddHours(47));
        Assert.Equal(a.Id, (await LeerAsync(s.Id)).PujaGanadora!.PostorId);

        await ProcesarEnAsync(Cierre.AddHours(48));
        var despues = await LeerAsync(s.Id);
        Assert.Equal(EstadoSubasta.Finalizada, despues.Estado);
        Assert.Equal(b.Id, despues.PujaGanadora!.PostorId);
        Assert.Equal(6000, despues.PujaGanadora.Monto);
        Assert.Equal(Cierre.AddHours(96), despues.PlazoPago);

        await using var db = _base.Contexto();
        Assert.All(await db.Pujas.Where(p => p.PostorId == a.Id).ToListAsync(), p => Assert.True(p.Descartada));
    }

    [Fact]
    public async Task Si_nadie_mas_pujo_o_no_alcanza_la_reserva_queda_desierta()
    {
        var vendedor = await _base.UsuarioAsync("luis@prueba.test");
        var a = await _base.UsuarioAsync("maria@prueba.test");
        var b = await _base.UsuarioAsync("jesus@prueba.test");
        var s = await _base.SubastaAsync(vendedor, reserva: 6000, cierre: Cierre);
        await PujarAsync(s.Id, b.Id, 5000);
        await PujarAsync(s.Id, a.Id, 6000);
        await ProcesarEnAsync(Cierre);

        await ProcesarEnAsync(Cierre.AddHours(48));

        var despues = await LeerAsync(s.Id);
        Assert.Equal(EstadoSubasta.Desierta, despues.Estado);
        Assert.Null(despues.PujaGanadoraId);
        Assert.Null(despues.PlazoPago);
    }

    [Fact]
    public async Task Con_el_pago_confirmado_no_pasa_al_siguiente()
    {
        var vendedor = await _base.UsuarioAsync("luis@prueba.test");
        var a = await _base.UsuarioAsync("maria@prueba.test");
        var b = await _base.UsuarioAsync("jesus@prueba.test");
        var s = await _base.SubastaAsync(vendedor, cierre: Cierre);
        await PujarAsync(s.Id, b.Id, 4000);
        await PujarAsync(s.Id, a.Id, 5000);
        await ProcesarEnAsync(Cierre);

        _base.Reloj.Ahora = Cierre.AddHours(10);
        await using (var db = _base.Contexto())
            await new VendedorService(db, _base.Reloj).ConfirmarPagoAsync(s.Id, vendedor.Id);
        await ProcesarEnAsync(Cierre.AddHours(60));

        Assert.Equal(a.Id, (await LeerAsync(s.Id)).PujaGanadora!.PostorId);
    }

    [Fact]
    public async Task Solo_el_vendedor_confirma_y_no_despues_del_plazo()
    {
        var vendedor = await _base.UsuarioAsync("luis@prueba.test");
        var a = await _base.UsuarioAsync("maria@prueba.test");
        var s = await _base.SubastaAsync(vendedor, cierre: Cierre);
        await PujarAsync(s.Id, a.Id, 5000);
        await ProcesarEnAsync(Cierre);

        await using var db = _base.Contexto();
        var servicio = new VendedorService(db, _base.Reloj);
        await Assert.ThrowsAsync<ReglaException>(() => servicio.ConfirmarPagoAsync(s.Id, a.Id));
        _base.Reloj.Ahora = Cierre.AddHours(49);
        await Assert.ThrowsAsync<ReglaException>(() => servicio.ConfirmarPagoAsync(s.Id, vendedor.Id));
    }

    public void Dispose() => _base.Dispose();
}
