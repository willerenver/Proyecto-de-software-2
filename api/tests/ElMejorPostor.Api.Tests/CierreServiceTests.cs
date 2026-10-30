using Microsoft.EntityFrameworkCore;
using ElMejorPostor.Api.Dominio;
using ElMejorPostor.Api.Servicios;

namespace ElMejorPostor.Api.Tests;

public class CierreServiceTests : IDisposable
{
    private readonly BaseDePrueba _base = new();

    private async Task CerrarCuandoVenceAsync(DateTime momento)
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

    private async Task PujarAsync(long subastaId, long postorId, decimal monto)
    {
        await using var db = _base.Contexto();
        await new PujaService(db, _base.Reloj).RegistrarAsync(subastaId, postorId, monto);
    }

    [Fact]
    public async Task Al_cierre_gana_la_puja_mas_alta()
    {
        var vendedor = await _base.UsuarioAsync("luis@prueba.test");
        var a = await _base.UsuarioAsync("maria@prueba.test");
        var b = await _base.UsuarioAsync("jesus@prueba.test");
        var s = await _base.SubastaAsync(vendedor, cierre: BaseDePrueba.Hoy.AddHours(1));
        await PujarAsync(s.Id, a.Id, 5000);
        await PujarAsync(s.Id, b.Id, 6000);

        await CerrarCuandoVenceAsync(BaseDePrueba.Hoy.AddHours(1));

        var cerrada = await LeerAsync(s.Id);
        Assert.Equal(EstadoSubasta.Finalizada, cerrada.Estado);
        Assert.Equal(b.Id, cerrada.PujaGanadora!.PostorId);
        Assert.Equal(6000, cerrada.PujaGanadora.Monto);
        Assert.NotNull(cerrada.CerradaEn);
    }

    [Fact]
    public async Task Sin_pujas_la_subasta_queda_desierta()
    {
        var vendedor = await _base.UsuarioAsync("luis@prueba.test");
        var s = await _base.SubastaAsync(vendedor, cierre: BaseDePrueba.Hoy.AddHours(1));

        await CerrarCuandoVenceAsync(BaseDePrueba.Hoy.AddHours(1));

        var cerrada = await LeerAsync(s.Id);
        Assert.Equal(EstadoSubasta.Desierta, cerrada.Estado);
        Assert.Null(cerrada.PujaGanadoraId);
    }

    [Fact]
    public async Task Si_no_se_alcanza_la_reserva_queda_desierta()
    {
        var vendedor = await _base.UsuarioAsync("luis@prueba.test");
        var postor = await _base.UsuarioAsync("maria@prueba.test");
        var s = await _base.SubastaAsync(vendedor, salida: 4000, reserva: 8000, cierre: BaseDePrueba.Hoy.AddHours(1));
        await PujarAsync(s.Id, postor.Id, 7500);

        await CerrarCuandoVenceAsync(BaseDePrueba.Hoy.AddHours(1));

        Assert.Equal(EstadoSubasta.Desierta, (await LeerAsync(s.Id)).Estado);
    }

    [Fact]
    public async Task Una_subasta_programada_se_activa_al_llegar_su_inicio()
    {
        var vendedor = await _base.UsuarioAsync("luis@prueba.test");
        var s = await _base.SubastaAsync(vendedor, inicio: BaseDePrueba.Hoy.AddHours(1), cierre: BaseDePrueba.Hoy.AddDays(3));
        await using (var db = _base.Contexto())
        {
            var guardada = await db.Subastas.SingleAsync(x => x.Id == s.Id);
            guardada.Estado = EstadoSubasta.Programada;
            await db.SaveChangesAsync();
        }

        await CerrarCuandoVenceAsync(BaseDePrueba.Hoy.AddMinutes(59));
        Assert.Equal(EstadoSubasta.Programada, (await LeerAsync(s.Id)).Estado);

        await CerrarCuandoVenceAsync(BaseDePrueba.Hoy.AddHours(1));
        Assert.Equal(EstadoSubasta.Activa, (await LeerAsync(s.Id)).Estado);
    }

    public void Dispose() => _base.Dispose();
}
