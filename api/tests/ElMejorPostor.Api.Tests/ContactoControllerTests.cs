using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ElMejorPostor.Api.Controllers;
using ElMejorPostor.Api.Servicios;

namespace ElMejorPostor.Api.Tests;

public class ContactoControllerTests : IDisposable
{
    private readonly BaseDePrueba _base = new();

    private async Task<Contratos.ContactoSubastaResponse> VerComoAsync(long subastaId, long usuarioId)
    {
        await using var db = _base.Contexto();
        var usuario = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, usuarioId.ToString())], "prueba"));
        var controlador = new ContactoController(db) { ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = usuario } } };
        return (await controlador.Ver(subastaId, CancellationToken.None)).Value!;
    }

    private async Task<long> SubastaVendidaAsync(Dominio.Usuario vendedor, Dominio.Usuario ganador)
    {
        var s = await _base.SubastaAsync(vendedor, cierre: BaseDePrueba.Hoy.AddHours(1));
        await using (var db = _base.Contexto())
            await new PujaService(db, _base.Reloj).RegistrarAsync(s.Id, ganador.Id, 5000);
        _base.Reloj.Ahora = BaseDePrueba.Hoy.AddHours(1);
        await using (var db = _base.Contexto())
            await new CierreService(db, _base.Reloj).ProcesarAsync();
        return s.Id;
    }

    [Fact]
    public async Task El_ganador_ve_al_vendedor_y_el_vendedor_ve_al_ganador()
    {
        var vendedor = await _base.UsuarioAsync("luis@prueba.test");
        var ganador = await _base.UsuarioAsync("maria@prueba.test");
        var id = await SubastaVendidaAsync(vendedor, ganador);

        var comoComprador = await VerComoAsync(id, ganador.Id);
        Assert.Equal("comprador", comoComprador.Rol);
        Assert.Equal("luis@prueba.test", comoComprador.Contraparte.Correo);
        Assert.Equal(5000, comoComprador.Monto);

        var comoVendedor = await VerComoAsync(id, vendedor.Id);
        Assert.Equal("vendedor", comoVendedor.Rol);
        Assert.Equal("maria@prueba.test", comoVendedor.Contraparte.Correo);
    }

    [Fact]
    public async Task Otro_usuario_no_ve_los_datos_de_contacto()
    {
        var vendedor = await _base.UsuarioAsync("luis@prueba.test");
        var ganador = await _base.UsuarioAsync("maria@prueba.test");
        var curioso = await _base.UsuarioAsync("jesus@prueba.test");
        var id = await SubastaVendidaAsync(vendedor, ganador);

        var error = await Assert.ThrowsAsync<ReglaException>(() => VerComoAsync(id, curioso.Id));
        Assert.Equal(StatusCodes.Status403Forbidden, error.EstadoHttp);
    }

    [Fact]
    public async Task Sin_ganador_no_hay_datos_de_contacto()
    {
        var vendedor = await _base.UsuarioAsync("luis@prueba.test");
        var s = await _base.SubastaAsync(vendedor);

        var error = await Assert.ThrowsAsync<ReglaException>(() => VerComoAsync(s.Id, vendedor.Id));
        Assert.Equal(StatusCodes.Status409Conflict, error.EstadoHttp);
    }

    public void Dispose() => _base.Dispose();
}
