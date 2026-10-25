using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Pujalo.Api.Dominio;

namespace Pujalo.Api.Datos;

// Datos de demostración con las subastas del Figma. Solo corre con «dotnet run -- --sembrar-demo»
// y solo si la base todavía no tiene usuarios; nunca se ejecuta por sí solo.
public static class DemoSeeder
{
    public static async Task SembrarAsync(IServiceProvider sp, IConfiguration config, IWebHostEnvironment entorno)
    {
        var db = sp.GetRequiredService<PujaloDbContext>();
        var hasher = sp.GetRequiredService<IPasswordHasher<Usuario>>();
        var log = sp.GetRequiredService<ILoggerFactory>().CreateLogger("DemoSeeder");
        if (await db.Usuarios.AnyAsync())
        {
            log.LogWarning("La base ya tiene usuarios: no se siembran datos de demostración.");
            return;
        }

        var contrasena = config["Demo:Contrasena"] ?? throw new InvalidOperationException("Falta Demo:Contrasena en la configuración.");
        var ahora = DateTime.UtcNow;
        var roles = await db.Roles.ToDictionaryAsync(r => r.Nombre);

        Usuario Nuevo(string nombre, string apellido, string correo, string rol = Rol.Usuario)
        {
            var u = new Usuario { Rol = roles[rol], Nombre = nombre, Apellido = apellido, Correo = correo, CreadoEn = ahora.AddYears(-2) };
            u.ContrasenaHash = hasher.HashPassword(u, contrasena);
            db.Usuarios.Add(u);
            return u;
        }

        Nuevo("Admin", "Pújalo", "admin@pujalo.test", Rol.Administrador);
        var maria = Nuevo("María", "Rodríguez", "maria.r@pujalo.test");
        var luis = Nuevo("Luis", "Rosario", "luis.rosario@pujalo.test");
        var ana = Nuevo("Ana", "Peña", "ana.pena@pujalo.test");
        var postores = new[] { Nuevo("Jesús", "Pérez", "jesus.p@pujalo.test"), Nuevo("Carlos", "Ortiz", "carlo91@pujalo.test"), Nuevo("Andrea", "Ruiz", "a.ruiz_rd@pujalo.test") };

        var fotos = Path.GetFullPath(Path.Combine(entorno.ContentRootPath, config["Demo:CarpetaFotos"] ?? ""));
        var destino = Path.Combine(entorno.ContentRootPath, "wwwroot", "fotos");
        Directory.CreateDirectory(destino);

        void Subasta(string titulo, string foto, int categoriaId, Usuario vendedor, decimal salida, decimal incremento, double minutos, Usuario?[] quienes, decimal[] montos, decimal? reserva = null)
        {
            var origen = Path.Combine(fotos, $"{foto}.jpg");
            if (File.Exists(origen)) File.Copy(origen, Path.Combine(destino, $"{foto}.jpg"), overwrite: true);
            var s = new Subasta
            {
                Articulo = new Articulo
                {
                    Vendedor = vendedor, CategoriaId = categoriaId, Titulo = titulo, Condicion = "buen_estado", CreadoEn = ahora.AddDays(-2),
                    Descripcion = $"{titulo}. Artículo de demostración.",
                    Imagenes = [new ImagenArticulo { Ruta = $"/fotos/{foto}.jpg", Orden = 1 }],
                },
                PrecioSalida = salida, IncrementoMinimo = incremento, PrecioReserva = reserva,
                FechaInicio = ahora.AddDays(-2), FechaCierre = ahora.AddMinutes(minutos), Estado = EstadoSubasta.Activa, CreadoEn = ahora.AddDays(-2),
            };
            for (var i = 0; i < montos.Length; i++)
                s.Pujas.Add(new Puja { Postor = quienes[i % quienes.Length]!, Monto = montos[i], CreadaEn = ahora.AddMinutes(-(montos.Length - i) * 9) });
            db.Subastas.Add(s);
        }

        var (j, c, a) = (postores[0], postores[1], postores[2]);
        Subasta("PlayStation 5 con control DualSense y 2 juegos", "ps5", 301, ana, 15000, 500, 38, [j, c], [15000, 18000, 21000, 24500]);
        Subasta("Reloj Casio G-Shock GW-M5610U solar", "reloj", 701, ana, 2500, 250, 52, [c, maria, j], [2500, 3000, 3250, 3500, 3750, 4000, 4250, 4500, 4750]);
        Subasta("Nintendo Switch OLED con dock y estuche", "switch", 301, ana, 9000, 300, 57, [j, c, a], [9200, 11000, 13000, 14900]);
        Subasta("Guitarra acústica Yamaha FG-331 con estuche", "guitarra", 401, luis, 4000, 500, 134, [a, j, c, j, maria], [7500, 8000, 8500, 9000, 9500], reserva: 8000);
        Subasta("iPhone 13 Pro 256 GB verde alpino, batería al 89 %", "iphone", 101, luis, 20000, 1000, 302, [maria, j], [30000, 31000]);
        Subasta("Cámara Canon AE-1 Program con lente 50 mm f/1.8", "camara", 801, luis, 6000, 250, 36 * 60, [j, c], [6000, 6250, 6500, 6750, 7000, 7200]);
        Subasta("Bicicleta Trek 4500 aro 26, talla M", "bici", 601, ana, 8000, 500, 43 * 60, [maria, j], [12300, 13300, 13800]);
        Subasta("Mecedora antigua de madera, restaurada", "mecedora", 501, luis, 4500, 500, 51 * 60, [j, maria], [4500, 5000, 5500, 6000]);

        await db.SaveChangesAsync();
        log.LogInformation("Datos de demostración sembrados. Usuarios en appsettings.Development.json (Demo).");
    }
}
