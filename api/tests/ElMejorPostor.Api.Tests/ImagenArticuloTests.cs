using ElMejorPostor.Api.Dominio;

namespace ElMejorPostor.Api.Tests;

public class ImagenArticuloTests
{
    private static Task<string?> TipoAsync(params byte[] bytes) => ImagenArticulo.ExtensionPorContenidoAsync(new MemoryStream(bytes), default);

    [Fact]
    public async Task Reconoce_jpg_y_png_por_su_contenido()
    {
        Assert.Equal(".jpg", await TipoAsync(0xFF, 0xD8, 0xFF, 0xE0, 0, 0x10));
        Assert.Equal(".png", await TipoAsync(0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0));
    }

    [Fact]
    public async Task Rechaza_otro_archivo_aunque_diga_ser_imagen()
    {
        Assert.Null(await TipoAsync("<script>alert(1)</script>"u8.ToArray()));
        Assert.Null(await TipoAsync(0x25, 0x50, 0x44, 0x46));
        Assert.Null(await TipoAsync());
    }
}
