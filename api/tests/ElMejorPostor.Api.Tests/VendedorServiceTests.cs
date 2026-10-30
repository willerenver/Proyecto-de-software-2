using Microsoft.EntityFrameworkCore;
using ElMejorPostor.Api.Contratos;
using ElMejorPostor.Api.Dominio;
using ElMejorPostor.Api.Servicios;

namespace ElMejorPostor.Api.Tests;

public class VendedorServiceTests : IDisposable
{
    private readonly BaseDePrueba _base = new();

    private static CrearSubastaRequest Datos(int dias = 3, decimal salida = 4500) =>
        new("Guitarra acústica Yamaha FG-331", "Con estuche", "como_nuevo", 401, salida, 250, null, BaseDePrueba.Hoy, dias);

    [Fact]
    public async Task Sin_pujas_el_vendedor_puede_corregir_la_subasta()
    {
        var vendedor = await _base.UsuarioAsync("luis@prueba.test");
        var s = await _base.SubastaAsync(vendedor, inicio: BaseDePrueba.Hoy.AddHours(-1));

        await using (var db = _base.Contexto())
            await new VendedorService(db, _base.Reloj).EditarAsync(s.Id, vendedor.Id, Datos(dias: 5, salida: 4500));

        await using var lectura = _base.Contexto();
        var editada = await lectura.Subastas.Include(x => x.Articulo).SingleAsync(x => x.Id == s.Id);
        Assert.Equal(4500, editada.PrecioSalida);
        Assert.Equal("como_nuevo", editada.Articulo.Condicion);
        Assert.Equal(BaseDePrueba.Hoy.AddHours(-1).AddDays(5), editada.FechaCierre);
    }

    [Fact]
    public async Task Con_pujas_no_se_puede_editar_ni_retirar()
    {
        var vendedor = await _base.UsuarioAsync("luis@prueba.test");
        var postor = await _base.UsuarioAsync("maria@prueba.test");
        var s = await _base.SubastaAsync(vendedor);
        await using (var db = _base.Contexto())
            await new PujaService(db, _base.Reloj).RegistrarAsync(s.Id, postor.Id, 4000);

        await using var db2 = _base.Contexto();
        var servicio = new VendedorService(db2, _base.Reloj);
        await Assert.ThrowsAsync<ReglaException>(() => servicio.EditarAsync(s.Id, vendedor.Id, Datos()));
        await Assert.ThrowsAsync<ReglaException>(() => servicio.RetirarAsync(s.Id, vendedor.Id));
    }

    [Fact]
    public async Task Solo_el_vendedor_retira_su_subasta()
    {
        var vendedor = await _base.UsuarioAsync("luis@prueba.test");
        var otro = await _base.UsuarioAsync("maria@prueba.test");
        var s = await _base.SubastaAsync(vendedor);

        await using (var db = _base.Contexto())
        {
            var servicio = new VendedorService(db, _base.Reloj);
            await Assert.ThrowsAsync<ReglaException>(() => servicio.RetirarAsync(s.Id, otro.Id));
            await servicio.RetirarAsync(s.Id, vendedor.Id);
        }

        await using var lectura = _base.Contexto();
        Assert.Equal(EstadoSubasta.Cancelada, (await lectura.Subastas.SingleAsync(x => x.Id == s.Id)).Estado);
    }

    [Fact]
    public async Task Al_quitar_una_foto_el_orden_sigue_seguido()
    {
        var vendedor = await _base.UsuarioAsync("luis@prueba.test");
        var s = await _base.SubastaAsync(vendedor);
        await using (var db = _base.Contexto())
        {
            for (short i = 1; i <= 3; i++)
                db.Imagenes.Add(new ImagenArticulo { ArticuloId = s.ArticuloId, Ruta = $"/fotos/{i}.jpg", Orden = i });
            await db.SaveChangesAsync();
        }

        await using (var db = _base.Contexto())
            await new VendedorService(db, _base.Reloj).QuitarImagenAsync(s.Id, vendedor.Id, "/fotos/1.jpg");

        await using var lectura = _base.Contexto();
        var fotos = await lectura.Imagenes.Where(i => i.ArticuloId == s.ArticuloId).OrderBy(i => i.Orden).Select(i => i.Ruta).ToListAsync();
        Assert.Equal(["/fotos/3.jpg", "/fotos/2.jpg"], fotos);
    }

    public void Dispose() => _base.Dispose();
}
