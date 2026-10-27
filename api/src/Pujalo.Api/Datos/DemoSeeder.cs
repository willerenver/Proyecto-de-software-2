using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Pujalo.Api.Dominio;

namespace Pujalo.Api.Datos;

// Datos de demostración con las subastas del Figma, en todos los estados. Nunca corre por sí solo:
//   --sembrar-demo     siembra solo si la base todavía no tiene usuarios.
//   --reiniciar-demo   borra la base, la vuelve a crear y siembra con fechas a partir de ahora (solo en Development).
public static class DemoSeeder
{
    public static async Task SembrarAsync(IServiceProvider sp, IConfiguration config, IWebHostEnvironment entorno, bool reiniciar = false)
    {
        var db = sp.GetRequiredService<PujaloDbContext>();
        var hasher = sp.GetRequiredService<IPasswordHasher<Usuario>>();
        var log = sp.GetRequiredService<ILoggerFactory>().CreateLogger("DemoSeeder");

        if (reiniciar)
        {
            if (!entorno.IsDevelopment())
                throw new InvalidOperationException("--reiniciar-demo borra la base: solo se permite en Development.");
            await db.Database.EnsureDeletedAsync();
            await db.Database.MigrateAsync();
            log.LogWarning("Base de datos borrada y creada de nuevo para la demostración.");
        }
        else if (await db.Usuarios.AnyAsync())
        {
            log.LogWarning("La base ya tiene usuarios: no se siembran datos de demostración. Usa --reiniciar-demo para empezar de cero.");
            return;
        }

        var contrasena = config["Demo:Contrasena"] ?? throw new InvalidOperationException("Falta Demo:Contrasena en la configuración.");
        var ahora = DateTime.UtcNow;
        var roles = await db.Roles.ToDictionaryAsync(r => r.Nombre);
        var n = 0;

        Usuario Nuevo(string nombre, string apellido, string correo, string rol = Rol.Usuario)
        {
            n++;
            var u = new Usuario
            {
                Rol = roles[rol], Nombre = nombre, Apellido = apellido, Correo = correo, CreadoEn = ahora.AddYears(-2),
                Telefono = $"809-555-01{n:00}", Cedula = $"0010000{n:000}{n % 10}",
            };
            u.ContrasenaHash = hasher.HashPassword(u, contrasena);
            db.Usuarios.Add(u);
            return u;
        }

        Nuevo("Admin", "Pújalo", "admin@pujalo.test", Rol.Administrador);
        var maria = Nuevo("María", "Rodríguez", "maria.r@pujalo.test");
        var luis = Nuevo("Luis", "Rosario", "luis.rosario@pujalo.test");
        var ana = Nuevo("Ana", "Peña", "ana.pena@pujalo.test");
        var (j, c, a) = (Nuevo("Jesús", "Pérez", "jesus.p@pujalo.test"), Nuevo("Carlos", "Ortiz", "carlo91@pujalo.test"), Nuevo("Andrea", "Ruiz", "a.ruiz_rd@pujalo.test"));

        var fotos = Path.GetFullPath(Path.Combine(entorno.ContentRootPath, config["Demo:CarpetaFotos"] ?? ""));
        var destino = Path.Combine(entorno.WebRootPath ?? Path.Combine(entorno.ContentRootPath, "wwwroot"), "fotos");
        Directory.CreateDirectory(destino);

        // «inicio» y «cierre» en minutos desde ahora (negativo = en el pasado).
        Subasta Subasta(string titulo, string? foto, int categoriaId, Usuario vendedor, decimal salida, decimal incremento,
            double inicio, double cierre, Usuario[] quienes, decimal[] montos, decimal? reserva = null, string descripcion = "")
        {
            var imagenes = new List<ImagenArticulo>();
            if (foto is not null)
            {
                var origen = Path.Combine(fotos, $"{foto}.jpg");
                if (File.Exists(origen)) File.Copy(origen, Path.Combine(destino, $"{foto}.jpg"), overwrite: true);
                imagenes.Add(new ImagenArticulo { Ruta = $"/fotos/{foto}.jpg", Orden = 1 });
            }
            var s = new Subasta
            {
                Articulo = new Articulo
                {
                    Vendedor = vendedor, CategoriaId = categoriaId, Titulo = titulo, Condicion = "buen_estado", CreadoEn = ahora.AddMinutes(inicio),
                    Descripcion = descripcion.Length > 0 ? descripcion : $"{titulo}. Funciona perfectamente; se entrega tal como se ve en las fotos.",
                    PermiteRetiro = true, PermiteEnvio = categoriaId != 501, Imagenes = imagenes,
                },
                PrecioSalida = salida, IncrementoMinimo = incremento, PrecioReserva = reserva,
                FechaInicio = ahora.AddMinutes(inicio), FechaCierre = ahora.AddMinutes(cierre), CreadoEn = ahora.AddMinutes(inicio),
                Estado = inicio > 0 ? EstadoSubasta.Programada : EstadoSubasta.Activa,
            };
            // Pujas repartidas entre el inicio y ahora (o el cierre, si ya cerró).
            var fin = Math.Min(0, cierre) - 1;
            for (var i = 0; i < montos.Length; i++)
                s.Pujas.Add(new Puja { Postor = quienes[i % quienes.Length], Monto = montos[i], CreadaEn = ahora.AddMinutes(inicio + (fin - inicio) * (i + 1) / (montos.Length + 1)) });
            db.Subastas.Add(s);
            return s;
        }

        const double H = 60, D = 1440;

        // Activas: una cierra en menos de 1 hora para mostrar «Cierra pronto» y la extensión al final.
        Subasta("Reloj Casio G-Shock GW-M5610U solar", "reloj", 701, ana, 2500, 250, -2 * D, 50, [c, maria, j], [2500, 3000, 3250, 3500, 3750, 4000, 4250, 4500, 4750]);
        Subasta("PlayStation 5 con control DualSense y 2 juegos", "ps5", 301, ana, 15000, 500, -2 * D, 3 * H, [j, c], [15000, 18000, 21000, 24500]);
        Subasta("Nintendo Switch OLED con dock y estuche", "switch", 301, ana, 9000, 300, -D, 8 * H, [j, c, a], [9200, 11000, 13000, 14900]);
        Subasta("Guitarra acústica Yamaha FG-331 con estuche", "guitarra", 401, luis, 4000, 500, -2 * D, 26 * H, [a, j, c, j, maria], [7500, 8000, 8500, 9000, 9500], reserva: 8000,
            descripcion: "Guitarra acústica Yamaha FG-331 de los años 90. Suena muy bien, el mástil está derecho y los trastes están parejos.\nLe cambié las cuerdas hace dos semanas. Se entrega con estuche acolchado y correa.");
        Subasta("iPhone 13 Pro 256 GB verde alpino, batería al 89 %", "iphone", 101, luis, 20000, 1000, -D, 2 * D, [maria, j], [30000, 31000]);
        Subasta("Cámara Canon AE-1 Program con lente 50 mm f/1.8", "camara", 801, luis, 6000, 250, -D, 3 * D, [j, c], [6000, 6250, 6500, 6750, 7000, 7200]);
        Subasta("Bicicleta Trek 4500 aro 26, talla M", "bici", 601, ana, 8000, 500, -3 * D, 4 * D, [maria, j], [12300, 13300, 13800]);
        Subasta("Mecedora antigua de madera, restaurada", "mecedora", 501, luis, 4500, 500, -D, 6 * D, [j, maria], [4500, 5000, 5500, 6000]);

        // Programada: empieza mañana.
        Subasta("Teclado Yamaha PSR-E373 con base y pedal", null, 403, ana, 9000, 250, D, 4 * D, [], []);

        // Vendida hace 2 horas: María ganó y tiene 46 horas para pagarle a Ana.
        var vendida = Subasta("Fender Stratocaster Highway One", "strat", 402, ana, 30000, 1000, -3 * D, -2 * H, [j, maria, c, maria], [30000, 32000, 35000, 38000]);
        vendida.Estado = EstadoSubasta.Finalizada;
        vendida.CerradaEn = ahora.AddHours(-2);
        vendida.PlazoPago = ahora.AddHours(46);

        // Vendida y pagada: Luis ya confirmó el pago de Carlos.
        var pagada = Subasta("Monitor LG 27 pulgadas 4K", null, 202, luis, 9000, 500, -6 * D, -3 * D, [c, a], [9000, 10500, 12800]);
        pagada.Estado = EstadoSubasta.Finalizada;
        pagada.CerradaEn = ahora.AddDays(-3);
        pagada.PlazoPago = ahora.AddDays(-1);
        pagada.PagoConfirmadoEn = ahora.AddDays(-2);

        // Sin ganador: no alcanzó la reserva.
        var desierta = Subasta("Samsung Galaxy S21 128 GB", null, 102, ana, 12000, 500, -5 * D, -D, [j], [12000, 12500], reserva: 17000);
        desierta.Estado = EstadoSubasta.Desierta;
        desierta.CerradaEn = ahora.AddDays(-1);

        await db.SaveChangesAsync();

        // La puja ganadora se asigna después de guardar, cuando las pujas ya tienen id.
        foreach (var s in new[] { vendida, pagada })
            s.PujaGanadoraId = s.Pujas.OrderByDescending(p => p.Monto).First().Id;
        await db.SaveChangesAsync();
        log.LogInformation("Datos de demostración sembrados. Usuarios en appsettings.Development.json (Demo).");
    }
}
