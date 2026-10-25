namespace Pujalo.Api.Dominio;

public static class Condiciones
{
    public static readonly string[] Validas = ["nuevo", "como_nuevo", "buen_estado", "con_detalles", "restaurado"];
}

// Lo que se vende. Aquí se guarda quién es el vendedor; la subasta lo obtiene a través del artículo.
public class Articulo
{
    public long Id { get; set; }
    public long VendedorId { get; set; }
    public Usuario Vendedor { get; set; } = null!;
    public int CategoriaId { get; set; }
    public Categoria Categoria { get; set; } = null!;
    public string Titulo { get; set; } = "";
    public string Descripcion { get; set; } = "";
    public string Condicion { get; set; } = "buen_estado";
    public bool PermiteRetiro { get; set; } = true;
    public bool PermiteEnvio { get; set; }
    public DateTime CreadoEn { get; set; }
    public List<ImagenArticulo> Imagenes { get; set; } = [];
    public Subasta? Subasta { get; set; }
}
