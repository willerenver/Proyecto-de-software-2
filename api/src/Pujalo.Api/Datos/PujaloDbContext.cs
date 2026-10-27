using Microsoft.EntityFrameworkCore;
using Pujalo.Api.Dominio;

namespace Pujalo.Api.Datos;

// Esquema según el diccionario de datos del documento de avance.
public class PujaloDbContext(DbContextOptions<PujaloDbContext> options) : DbContext(options)
{
    public DbSet<Rol> Roles => Set<Rol>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<Articulo> Articulos => Set<Articulo>();
    public DbSet<ImagenArticulo> Imagenes => Set<ImagenArticulo>();
    public DbSet<Subasta> Subastas => Set<Subasta>();
    public DbSet<Puja> Pujas => Set<Puja>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Rol>(e =>
        {
            e.ToTable("rol");
            e.Property(x => x.Nombre).HasMaxLength(30).IsRequired();
            e.Property(x => x.Descripcion).HasMaxLength(150);
            e.HasIndex(x => x.Nombre).IsUnique();
            e.HasData(
                new Rol { Id = 1, Nombre = Rol.Usuario, Descripcion = "Puede publicar artículos y pujar en subastas de otros." },
                new Rol { Id = 2, Nombre = Rol.Administrador, Descripcion = "Gestiona usuarios, artículos, subastas y categorías." });
        });

        b.Entity<Usuario>(e =>
        {
            e.ToTable("usuario");
            e.Property(x => x.Nombre).HasMaxLength(60).IsRequired();
            e.Property(x => x.Apellido).HasMaxLength(60).IsRequired();
            e.Property(x => x.Correo).HasMaxLength(120).IsRequired();
            e.Property(x => x.ContrasenaHash).HasMaxLength(255).IsRequired();
            e.Property(x => x.Telefono).HasMaxLength(20);
            e.Property(x => x.Cedula).HasMaxLength(11).IsFixedLength();
            e.Property(x => x.Activo).HasDefaultValue(true);
            e.Property(x => x.RecuperacionHash).HasMaxLength(64);
            e.HasIndex(x => x.Correo).IsUnique();
            e.HasIndex(x => x.Cedula).IsUnique();
            e.HasOne(x => x.Rol).WithMany().HasForeignKey(x => x.RolId).OnDelete(DeleteBehavior.Restrict);
            e.Ignore(x => x.Iniciales);
        });

        b.Entity<Categoria>(e =>
        {
            e.ToTable("categoria");
            e.Property(x => x.Nombre).HasMaxLength(60).IsRequired();
            e.Property(x => x.Activa).HasDefaultValue(true);
            e.HasIndex(x => new { x.Nombre, x.CategoriaPadreId }).IsUnique();
            e.HasOne(x => x.CategoriaPadre).WithMany(x => x.Subcategorias).HasForeignKey(x => x.CategoriaPadreId).OnDelete(DeleteBehavior.Restrict);
            e.ToTable(t => t.HasCheckConstraint("ck_categoria_padre", "categoria_padre_id IS NULL OR categoria_padre_id <> id"));
            e.HasData(CatalogoCategorias.Todas());
        });

        b.Entity<Articulo>(e =>
        {
            e.ToTable("articulo");
            e.Property(x => x.Titulo).HasMaxLength(80).IsRequired();
            e.Property(x => x.Descripcion).HasMaxLength(2000).IsRequired();
            e.Property(x => x.Condicion).HasMaxLength(20).IsRequired();
            e.HasOne(x => x.Vendedor).WithMany().HasForeignKey(x => x.VendedorId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Categoria).WithMany().HasForeignKey(x => x.CategoriaId).OnDelete(DeleteBehavior.Restrict);
            e.HasMany(x => x.Imagenes).WithOne().HasForeignKey(x => x.ArticuloId).OnDelete(DeleteBehavior.Cascade);
            e.ToTable(t => t.HasCheckConstraint("ck_articulo_condicion",
                $"condicion IN ({string.Join(", ", Condiciones.Validas.Select(c => $"'{c}'"))})"));
        });

        b.Entity<ImagenArticulo>(e =>
        {
            e.ToTable("imagen_articulo");
            e.Property(x => x.Ruta).HasMaxLength(255).IsRequired();
            e.Property(x => x.Orden).HasDefaultValue((short)1);
            e.HasIndex(x => new { x.ArticuloId, x.Orden }).IsUnique();
            e.ToTable(t => t.HasCheckConstraint("ck_imagen_orden", $"orden BETWEEN 1 AND {ImagenArticulo.Maximo}"));
        });

        b.Entity<Subasta>(e =>
        {
            e.ToTable("subasta");
            e.Property(x => x.PrecioSalida).HasColumnType("decimal(12,2)");
            e.Property(x => x.IncrementoMinimo).HasColumnType("decimal(12,2)");
            e.Property(x => x.PrecioReserva).HasColumnType("decimal(12,2)");
            e.Property(x => x.Estado).HasConversion<string>().HasMaxLength(12)
                .HasDefaultValue(EstadoSubasta.Programada).HasSentinel(EstadoSubasta.Programada);
            e.Property(x => x.Version).IsConcurrencyToken();
            e.HasIndex(x => x.ArticuloId).IsUnique();
            e.HasIndex(x => x.PujaGanadoraId).IsUnique();
            e.HasIndex(x => new { x.Estado, x.FechaCierre });
            e.HasOne(x => x.Articulo).WithOne(x => x.Subasta).HasForeignKey<Subasta>(x => x.ArticuloId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.PujaGanadora).WithOne().HasForeignKey<Subasta>(x => x.PujaGanadoraId).OnDelete(DeleteBehavior.Restrict);
            e.HasMany(x => x.Pujas).WithOne(x => x.Subasta).HasForeignKey(x => x.SubastaId).OnDelete(DeleteBehavior.Restrict);
            e.Ignore(x => x.EsFinal);
            e.ToTable(t =>
            {
                t.HasCheckConstraint("ck_subasta_estado", "estado IN ('Programada', 'Activa', 'Finalizada', 'Desierta', 'Cancelada')");
                t.HasCheckConstraint("ck_subasta_fechas", "fecha_cierre > fecha_inicio");
            });
        });

        b.Entity<Puja>(e =>
        {
            e.ToTable("puja");
            e.Property(x => x.Monto).HasColumnType("decimal(12,2)");
            // Dos pujas iguales en la misma subasta son imposibles aunque falle la validación.
            e.HasIndex(x => new { x.SubastaId, x.Monto }).IsUnique();
            e.HasIndex(x => new { x.PostorId, x.SubastaId });
            e.HasOne(x => x.Postor).WithMany().HasForeignKey(x => x.PostorId).OnDelete(DeleteBehavior.Restrict);
        });

        // Columnas en snake_case, como en el diccionario de datos (categoria_padre_id, fecha_cierre…).
        // Todas las fechas se guardan en UTC; al leerlas se marcan como UTC para que la API las devuelva con «Z».
        var utc = new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<DateTime, DateTime>(
            v => v.ToUniversalTime(), v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
        foreach (var entidad in b.Model.GetEntityTypes())
            foreach (var propiedad in entidad.GetProperties())
            {
                propiedad.SetColumnName(SnakeCase(propiedad.Name));
                if (propiedad.ClrType == typeof(DateTime) || propiedad.ClrType == typeof(DateTime?))
                    propiedad.SetValueConverter(utc);
            }
    }

    private static string SnakeCase(string nombre) =>
        string.Concat(nombre.Select((c, i) => i > 0 && char.IsUpper(c) ? $"_{char.ToLowerInvariant(c)}" : char.ToLowerInvariant(c).ToString()));
}
