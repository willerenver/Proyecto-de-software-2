using ElMejorPostor.Api.Contratos;
using ElMejorPostor.Api.Dominio;
using ElMejorPostor.Api.Servicios;

namespace ElMejorPostor.Api.Tests;

// Los montos tienen un tope para que ninguna suma (puja + incremento) desborde la columna decimal(12,2).
public class MontoMaximoTests : IDisposable
{
    private readonly BaseDePrueba _base = new();
    private const decimal Excesivo = Subasta.MontoMaximo + 1;

    [Fact]
    public async Task Una_puja_por_encima_del_tope_se_rechaza_con_un_mensaje_claro()
    {
        var vendedor = await _base.UsuarioAsync("luis@prueba.test");
        var postor = await _base.UsuarioAsync("maria@prueba.test");
        var s = await _base.SubastaAsync(vendedor);

        await using var db = _base.Contexto();
        var servicio = new PujaService(db, _base.Reloj);
        var error = await Assert.ThrowsAsync<ReglaException>(() => servicio.RegistrarAsync(s.Id, postor.Id, Excesivo));
        Assert.Contains("no puede pasar de", error.Message);
        error = await Assert.ThrowsAsync<ReglaException>(() => servicio.ProgramarAutomaticaAsync(s.Id, postor.Id, Excesivo));
        Assert.Contains("no puede pasar de", error.Message);
    }

    [Fact]
    public async Task Al_publicar_los_precios_tienen_el_mismo_tope()
    {
        await using var db = _base.Contexto();
        foreach (var r in new[]
        {
            Datos(salida: Excesivo),
            Datos(incremento: Excesivo),
            Datos(reserva: Excesivo),
        })
        {
            var error = await Assert.ThrowsAsync<ReglaException>(() => VendedorService.ValidarAsync(db, r, CancellationToken.None));
            Assert.Contains("no puede pasar de", error.Message);
        }
    }

    private static CrearSubastaRequest Datos(decimal salida = 4000, decimal incremento = 250, decimal? reserva = null) =>
        new("Guitarra acústica Yamaha FG-331", "Con estuche", "como_nuevo", 401, salida, incremento, reserva, BaseDePrueba.Hoy, 3);

    public void Dispose() => _base.Dispose();
}
