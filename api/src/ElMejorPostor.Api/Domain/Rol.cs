namespace ElMejorPostor.Api.Dominio;

// Catálogo de permisos. Comprador y vendedor no son roles: cualquier usuario puede hacer ambas cosas.
public class Rol
{
    public const string Usuario = "usuario";
    public const string Administrador = "administrador";

    public short Id { get; set; }
    public string Nombre { get; set; } = "";
    public string? Descripcion { get; set; }
}
