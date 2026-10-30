namespace ElMejorPostor.Api.Dominio;

// La foto con Orden = 1 es la portada.
public class ImagenArticulo
{
    public const int Maximo = 10;
    public const long PesoMaximo = 10 * 1024 * 1024;

    public long Id { get; set; }
    public long ArticuloId { get; set; }
    public string Ruta { get; set; } = "";
    public short Orden { get; set; } = 1;

    // Firma de los primeros bytes: JPEG empieza con FF D8 FF y PNG con 89 50 4E 47 0D 0A 1A 0A.
    public static async Task<string?> ExtensionPorContenidoAsync(Stream contenido, CancellationToken ct)
    {
        var cabecera = new byte[8];
        var leidos = await contenido.ReadAtLeastAsync(cabecera, cabecera.Length, throwOnEndOfStream: false, ct);
        if (leidos >= 3 && cabecera[0] == 0xFF && cabecera[1] == 0xD8 && cabecera[2] == 0xFF) return ".jpg";
        if (leidos == 8 && cabecera.SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A })) return ".png";
        return null;
    }
}
