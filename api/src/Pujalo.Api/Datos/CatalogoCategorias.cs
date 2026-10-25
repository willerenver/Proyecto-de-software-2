using Pujalo.Api.Dominio;

namespace Pujalo.Api.Datos;

// Catálogo inicial de categorías y subcategorías, el mismo que muestra el diseño de Figma.
public static class CatalogoCategorias
{
    private static readonly (int Id, string Nombre, string[] Subcategorias)[] Lista =
    [
        (1, "Celulares", ["iPhone", "Android", "Accesorios"]),
        (2, "Electrónica", ["Audio", "Monitores", "Computadoras"]),
        (3, "Videojuegos", ["Consolas", "Juegos", "Controles"]),
        (4, "Instrumentos", ["Guitarras acústicas", "Guitarras eléctricas", "Teclados", "Percusión"]),
        (5, "Hogar y muebles", ["Muebles", "Decoración", "Cocina"]),
        (6, "Deportes", ["Bicicletas", "Gimnasio", "Pesca"]),
        (7, "Relojes", ["Digitales", "Análogos", "Inteligentes"]),
        (8, "Fotografía", ["Cámaras de rollo", "Cámaras digitales", "Lentes", "Accesorios"]),
        (9, "Coleccionables", ["Monedas", "Sellos", "Figuras"]),
    ];

    public static IEnumerable<Categoria> Todas()
    {
        foreach (var (id, nombre, subcategorias) in Lista)
        {
            yield return new Categoria { Id = id, Nombre = nombre };
            for (var i = 0; i < subcategorias.Length; i++)
                yield return new Categoria { Id = id * 100 + i + 1, Nombre = subcategorias[i], CategoriaPadreId = id };
        }
    }
}
