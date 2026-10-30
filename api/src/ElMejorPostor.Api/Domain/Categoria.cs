namespace ElMejorPostor.Api.Dominio;

// Admite un nivel de subcategoría («Fotografía › Cámaras de rollo»).
public class Categoria
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public int? CategoriaPadreId { get; set; }
    public Categoria? CategoriaPadre { get; set; }
    public List<Categoria> Subcategorias { get; set; } = [];
    public bool Activa { get; set; } = true;
}
