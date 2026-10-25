namespace Pujalo.Api.Dominio;

// La foto con Orden = 1 es la portada.
public class ImagenArticulo
{
    public const int Maximo = 10;
    public const long PesoMaximo = 10 * 1024 * 1024;

    public long Id { get; set; }
    public long ArticuloId { get; set; }
    public string Ruta { get; set; } = "";
    public short Orden { get; set; } = 1;
}
