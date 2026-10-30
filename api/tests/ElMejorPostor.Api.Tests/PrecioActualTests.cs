using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ElMejorPostor.Api.Controllers;
using ElMejorPostor.Api.Servicios;

namespace ElMejorPostor.Api.Tests;

// Cuando el ganador no paga, sus pujas quedan descartadas: el precio que se muestra es el del nuevo ganador.
public class PrecioActualTests : IDisposable
{
    private readonly BaseDePrueba _base = new();
    private static readonly DateTime Cierre = BaseDePrueba.Hoy.AddHours(1);

    private static ControllerContext Como(long usuarioId) => new()
    {
        HttpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, usuarioId.ToString())], "prueba")),
        },
    };

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

    [Fact]
    public async Task Despues_de_pasar_al_siguiente_se_muestra_el_monto_del_nuevo_ganador()
    {
        var vendedor = await _base.UsuarioAsync("luis@prueba.test");
        var incumplido = await _base.UsuarioAsync("maria@prueba.test");
        var siguiente = await _base.UsuarioAsync("jesus@prueba.test");
        var s = await _base.SubastaAsync(vendedor, cierre: Cierre);
        await PujarAsync(s.Id, siguiente.Id, 6000);
        await PujarAsync(s.Id, incumplido.Id, 7000);
        await ProcesarEnAsync(Cierre);
        await ProcesarEnAsync(Cierre.AddHours(48));

        await using var db = _base.Contexto();
        var detalle = (await new SubastasController(db, null!, _base.Reloj, null!, null!) { ControllerContext = Como(siguiente.Id) }
            .Detalle(s.Id, CancellationToken.None)).Value!;
        Assert.Equal(6000, detalle.PujaActual);
        Assert.Equal(6000, detalle.PujaGanadora!.Monto);

        var venta = Assert.Single(await new MisVentasController(db) { ControllerContext = Como(vendedor.Id) }.Listar(CancellationToken.None));
        Assert.Equal(6000, venta.PujaActual);

        var mia = Assert.Single(await new MisPujasController(db, _base.Reloj) { ControllerContext = Como(siguiente.Id) }.Listar(CancellationToken.None));
        Assert.Equal(6000, mia.PujaActual);
        Assert.Equal("ganada", mia.Situacion);
    }

    [Fact]
    public async Task Mis_pujas_no_falla_si_todas_mis_pujas_quedaron_descartadas()
    {
        var vendedor = await _base.UsuarioAsync("luis@prueba.test");
        var incumplido = await _base.UsuarioAsync("maria@prueba.test");
        var s = await _base.SubastaAsync(vendedor, cierre: Cierre);
        await PujarAsync(s.Id, incumplido.Id, 5000);
        await ProcesarEnAsync(Cierre);
        await ProcesarEnAsync(Cierre.AddHours(48));

        await using var db = _base.Contexto();
        var mia = Assert.Single(await new MisPujasController(db, _base.Reloj) { ControllerContext = Como(incumplido.Id) }.Listar(CancellationToken.None));
        Assert.Equal("perdida", mia.Situacion);
    }

    public void Dispose() => _base.Dispose();
}
