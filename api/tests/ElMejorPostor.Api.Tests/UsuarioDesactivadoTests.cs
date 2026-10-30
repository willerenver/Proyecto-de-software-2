using Microsoft.EntityFrameworkCore;
using ElMejorPostor.Api.Dominio;
using ElMejorPostor.Api.Servicios;

namespace ElMejorPostor.Api.Tests;

// Un usuario desactivado no puede entrar a pagar, así que no puede ganar ni recibir la subasta.
public class UsuarioDesactivadoTests : IDisposable
{
    private readonly BaseDePrueba _base = new();
    private static readonly DateTime Cierre = BaseDePrueba.Hoy.AddHours(1);

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

    private async Task DesactivarAsync(long usuarioId)
    {
        await using var db = _base.Contexto();
        (await db.Usuarios.SingleAsync(u => u.Id == usuarioId)).Activo = false;
        await db.SaveChangesAsync();
    }

    private async Task<Subasta> LeerAsync(long id)
    {
        await using var db = _base.Contexto();
        return await db.Subastas.Include(s => s.PujaGanadora).SingleAsync(s => s.Id == id);
    }

    [Fact]
    public async Task Al_cerrar_no_gana_un_postor_desactivado()
    {
        var vendedor = await _base.UsuarioAsync("luis@prueba.test");
        var a = await _base.UsuarioAsync("maria@prueba.test");
        var b = await _base.UsuarioAsync("jesus@prueba.test");
        var s = await _base.SubastaAsync(vendedor, cierre: Cierre);
        await PujarAsync(s.Id, a.Id, 5000);
        await PujarAsync(s.Id, b.Id, 6000);
        await DesactivarAsync(b.Id);

        await ProcesarEnAsync(Cierre);

        var cerrada = await LeerAsync(s.Id);
        Assert.Equal(EstadoSubasta.Finalizada, cerrada.Estado);
        Assert.Equal(a.Id, cerrada.PujaGanadora!.PostorId);
        await using var db = _base.Contexto();
        Assert.All(await db.Pujas.Where(p => p.PostorId == b.Id).ToListAsync(), p => Assert.True(p.Descartada));
    }

    [Fact]
    public async Task Al_pasar_al_siguiente_se_salta_a_los_desactivados()
    {
        var vendedor = await _base.UsuarioAsync("luis@prueba.test");
        var a = await _base.UsuarioAsync("maria@prueba.test");
        var b = await _base.UsuarioAsync("jesus@prueba.test");
        var c = await _base.UsuarioAsync("ana@prueba.test");
        var s = await _base.SubastaAsync(vendedor, cierre: Cierre);
        await PujarAsync(s.Id, c.Id, 4000);
        await PujarAsync(s.Id, a.Id, 5000);
        await PujarAsync(s.Id, b.Id, 6000);
        await ProcesarEnAsync(Cierre);
        await DesactivarAsync(a.Id);

        await ProcesarEnAsync(Cierre.AddHours(48));

        var despues = await LeerAsync(s.Id);
        Assert.Equal(c.Id, despues.PujaGanadora!.PostorId);
        Assert.Equal(4000, despues.PujaGanadora.Monto);
    }

    public void Dispose() => _base.Dispose();
}
