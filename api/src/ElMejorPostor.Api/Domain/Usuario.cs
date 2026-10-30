namespace ElMejorPostor.Api.Dominio;

public class Usuario
{
    public long Id { get; set; }
    public short RolId { get; set; }
    public Rol Rol { get; set; } = null!;
    public string Nombre { get; set; } = "";
    public string Apellido { get; set; } = "";
    public string Correo { get; set; } = "";
    public string ContrasenaHash { get; set; } = "";
    public string? Telefono { get; set; }
    public string? Cedula { get; set; }
    public bool Activo { get; set; } = true;
    public DateTime CreadoEn { get; set; }

    // Cambia al restablecer la contraseña o al desactivar la cuenta: los tokens emitidos antes dejan de valer.
    public int VersionSesion { get; set; }

    // Recuperación de contraseña: se guarda el hash del código, nunca el código.
    public string? RecuperacionHash { get; set; }
    public DateTime? RecuperacionExpira { get; set; }

    public string Iniciales => $"{Nombre.FirstOrDefault()}{Apellido.FirstOrDefault()}".ToUpperInvariant();
}
