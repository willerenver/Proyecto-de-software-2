using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ElMejorPostor.Api.Datos;
using ElMejorPostor.Api.Dominio;

namespace ElMejorPostor.Api.Tests;

public class RelojFijo(DateTime ahora) : TimeProvider
{
    public DateTime Ahora { get; set; } = ahora;
    public override DateTimeOffset GetUtcNow() => new(Ahora, TimeSpan.Zero);
}

// Base SQLite en un archivo temporal con el esquema del modelo (índices, restricciones y catálogos).
// Las migraciones son de SQL Server; las pruebas no necesitan un servidor.
public sealed class BaseDePrueba : IDisposable
{
    private readonly string _archivo = Path.Combine(Path.GetTempPath(), $"elmejorpostor-prueba-{Guid.NewGuid():N}.db");

    public static readonly DateTime Hoy = new(2026, 10, 23, 18, 0, 0, DateTimeKind.Utc);
    public RelojFijo Reloj { get; } = new(Hoy);

    public BaseDePrueba()
    {
        using var db = Contexto();
        db.Database.EnsureCreated();
    }

    public ElMejorPostorDbContext Contexto() =>
        new(new DbContextOptionsBuilder<ElMejorPostorDbContext>().UseSqlite($"Data Source={_archivo}").Options);

    public async Task<Usuario> UsuarioAsync(string correo)
    {
        await using var db = Contexto();
        var u = new Usuario { RolId = 1, Nombre = correo, Apellido = "Prueba", Correo = correo, ContrasenaHash = "x", CreadoEn = Hoy };
        db.Usuarios.Add(u);
        await db.SaveChangesAsync();
        return u;
    }

    public async Task<Subasta> SubastaAsync(Usuario vendedor, decimal salida = 4000, decimal incremento = 500, decimal? reserva = null,
        DateTime? inicio = null, DateTime? cierre = null, bool extender = true)
    {
        await using var db = Contexto();
        var s = new Subasta
        {
            Articulo = new Articulo { VendedorId = vendedor.Id, CategoriaId = 401, Titulo = "Guitarra acústica Yamaha", Descripcion = "Prueba", CreadoEn = Hoy },
            PrecioSalida = salida,
            IncrementoMinimo = incremento,
            PrecioReserva = reserva,
            FechaInicio = inicio ?? Hoy.AddDays(-1),
            FechaCierre = cierre ?? Hoy.AddHours(2),
            ExtenderAlFinal = extender,
            Estado = EstadoSubasta.Activa,
            CreadoEn = Hoy,
        };
        db.Subastas.Add(s);
        await db.SaveChangesAsync();
        return s;
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        foreach (var f in new[] { _archivo, $"{_archivo}-wal", $"{_archivo}-shm" })
            if (File.Exists(f)) File.Delete(f);
    }
}
