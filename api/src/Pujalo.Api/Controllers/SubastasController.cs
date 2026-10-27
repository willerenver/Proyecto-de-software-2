using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pujalo.Api.Contratos;
using Pujalo.Api.Datos;
using Pujalo.Api.Dominio;
using Pujalo.Api.Servicios;

namespace Pujalo.Api.Controllers;

[ApiController]
[Route("api/subastas")]
public class SubastasController(PujaloDbContext db, PujaService pujas, TimeProvider reloj, IWebHostEnvironment entorno) : ControllerBase
{
    private DateTime Ahora => reloj.GetUtcNow().UtcDateTime;

    // Subastas abiertas, por defecto ordenadas por hora de cierre (pantalla 01).
    [HttpGet]
    public async Task<IReadOnlyList<SubastaResumenResponse>> Listar(int? categoriaId, string? buscar, string orden = "cierre", CancellationToken ct = default)
    {
        var ahora = Ahora;
        var q = db.Subastas.AsNoTracking()
            .Where(s => s.Estado != EstadoSubasta.Cancelada && s.Estado != EstadoSubasta.Finalizada && s.Estado != EstadoSubasta.Desierta)
            .Where(s => s.FechaInicio <= ahora && s.FechaCierre > ahora);
        if (categoriaId is not null)
            q = q.Where(s => s.Articulo.CategoriaId == categoriaId || s.Articulo.Categoria.CategoriaPadreId == categoriaId);
        if (!string.IsNullOrWhiteSpace(buscar))
            q = q.Where(s => EF.Functions.Like(s.Articulo.Titulo, $"%{buscar.Trim()}%"));

        var lista = await q.Select(Resumen()).ToListAsync(ct);
        return orden switch
        {
            "precio" => lista.OrderBy(s => s.PujaActual ?? s.PrecioSalida).ToList(),
            "pujas" => lista.OrderByDescending(s => s.NumeroPujas).ToList(),
            _ => lista.OrderBy(s => s.FechaCierre).ToList(),
        };
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<SubastaDetalleResponse>> Detalle(long id, CancellationToken ct)
    {
        var s = await db.Subastas.AsNoTracking()
            .Include(x => x.Articulo).ThenInclude(a => a.Vendedor)
            .Include(x => x.Articulo).ThenInclude(a => a.Categoria).ThenInclude(c => c.CategoriaPadre)
            .Include(x => x.Articulo).ThenInclude(a => a.Imagenes)
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw ReglaException.NoEncontrado("La subasta no existe.");

        var historial = await db.Pujas.AsNoTracking().Include(p => p.Postor)
            .Where(p => p.SubastaId == id).OrderByDescending(p => p.Id).ToListAsync(ct);
        var ventas = await db.Subastas.CountAsync(x => x.Articulo.VendedorId == s.Articulo.VendedorId && x.Estado == EstadoSubasta.Finalizada, ct);
        var yo = User.Identity?.IsAuthenticated == true ? User.UsuarioId() : (long?)null;
        var actual = historial.FirstOrDefault()?.Monto;
        var categoria = s.Articulo.Categoria;
        var v = s.Articulo.Vendedor;

        return new SubastaDetalleResponse(
            s.Id, s.Articulo.Titulo, s.Articulo.Descripcion, s.Articulo.Condicion,
            categoria.CategoriaPadre?.Nombre ?? categoria.Nombre, categoria.CategoriaPadre is null ? null : categoria.Nombre,
            s.Articulo.Imagenes.OrderBy(i => i.Orden).Select(i => i.Ruta).ToList(),
            new VendedorResponse(v.Id, $"{v.Nombre} {v.Apellido}", v.Iniciales, ventas),
            s.Articulo.PermiteRetiro, s.Articulo.PermiteEnvio,
            s.PrecioSalida, s.IncrementoMinimo, s.PrecioReserva is not null, s.PrecioReserva is not null && actual >= s.PrecioReserva,
            actual, PujaService.MontoMinimo(s, actual), historial.Count, s.FechaInicio, s.FechaCierre,
            s.ExtenderAlFinal, s.Estado.ToString(), s.AceptaPujas(Ahora),
            historial.Where(p => p.Id == s.PujaGanadoraId).Select(p => Puja(p, yo)).FirstOrDefault(),
            historial.Select(p => Puja(p, yo)).ToList(),
            s.PlazoPago, s.PagoConfirmadoEn is not null,
            yo == v.Id && !s.EsFinal && historial.Count == 0);
    }

    [Authorize]
    [HttpPost]
    public async Task<ActionResult<SubastaResumenResponse>> Crear(CrearSubastaRequest r, CancellationToken ct)
    {
        var ahora = Ahora;
        var inicio = r.FechaInicio.ToUniversalTime();
        await VendedorService.ValidarAsync(db, r, ct);
        if (inicio < ahora.AddMinutes(-1))
            throw new ReglaException("La fecha de inicio no puede estar en el pasado.");

        var subasta = new Subasta
        {
            Articulo = new Articulo
            {
                VendedorId = User.UsuarioId(),
                CategoriaId = r.CategoriaId,
                Titulo = r.Titulo.Trim(),
                Descripcion = r.Descripcion.Trim(),
                Condicion = r.Condicion,
                PermiteRetiro = r.PermiteRetiro,
                PermiteEnvio = r.PermiteEnvio,
                CreadoEn = ahora,
            },
            PrecioSalida = r.PrecioSalida,
            IncrementoMinimo = r.IncrementoMinimo,
            PrecioReserva = r.PrecioReserva,
            FechaInicio = inicio,
            FechaCierre = inicio.AddDays(r.DuracionDias),
            ExtenderAlFinal = r.ExtenderAlFinal,
            Estado = inicio <= ahora ? EstadoSubasta.Activa : EstadoSubasta.Programada,
            CreadoEn = ahora,
        };
        db.Subastas.Add(subasta);
        await db.SaveChangesAsync(ct);
        var creada = await db.Subastas.AsNoTracking().Where(s => s.Id == subasta.Id).Select(Resumen()).SingleAsync(ct);
        return CreatedAtAction(nameof(Detalle), new { id = subasta.Id }, creada);
    }

    // Fotos del artículo: JPG o PNG, hasta 10 MB cada una y 10 por artículo.
    [Authorize]
    [HttpPost("{id:long}/imagenes")]
    [RequestSizeLimit(ImagenArticulo.Maximo * ImagenArticulo.PesoMaximo)]
    public async Task<ActionResult<IReadOnlyList<string>>> SubirImagenes(long id, [FromForm] List<IFormFile> fotos, CancellationToken ct)
    {
        var subasta = await db.Subastas.Include(s => s.Articulo).ThenInclude(a => a.Imagenes).FirstOrDefaultAsync(s => s.Id == id, ct)
            ?? throw ReglaException.NoEncontrado("La subasta no existe.");
        if (subasta.Articulo.VendedorId != User.UsuarioId())
            throw ReglaException.Prohibido("Solo el vendedor puede subir fotos a su artículo.");
        if (subasta.Articulo.Imagenes.Count + fotos.Count > ImagenArticulo.Maximo)
            throw new ReglaException($"Puedes subir hasta {ImagenArticulo.Maximo} fotos.");

        var carpeta = Path.Combine(entorno.WebRootPath ?? Path.Combine(entorno.ContentRootPath, "wwwroot"), "fotos");
        Directory.CreateDirectory(carpeta);
        var orden = (short)(subasta.Articulo.Imagenes.Select(i => (int)i.Orden).DefaultIfEmpty(0).Max() + 1);
        foreach (var foto in fotos)
        {
            var extension = foto.ContentType switch { "image/jpeg" => ".jpg", "image/png" => ".png", _ => null }
                ?? throw new ReglaException("Solo se aceptan fotos JPG o PNG.");
            if (foto.Length > ImagenArticulo.PesoMaximo)
                throw new ReglaException("Cada foto puede pesar hasta 10 MB.");
            var nombre = $"{Guid.NewGuid():N}{extension}";
            await using (var destino = System.IO.File.Create(Path.Combine(carpeta, nombre)))
                await foto.CopyToAsync(destino, ct);
            subasta.Articulo.Imagenes.Add(new ImagenArticulo { Ruta = $"/fotos/{nombre}", Orden = orden++ });
        }
        await db.SaveChangesAsync(ct);
        return subasta.Articulo.Imagenes.OrderBy(i => i.Orden).Select(i => i.Ruta).ToList();
    }

    [Authorize]
    [HttpPost("{id:long}/pujas")]
    public async Task<ActionResult<PujaRegistradaResponse>> Pujar(long id, CrearPujaRequest r, CancellationToken ct)
    {
        var puja = await pujas.RegistrarAsync(id, User.UsuarioId(), r.Monto, ct);
        var subasta = await db.Subastas.AsNoTracking().SingleAsync(s => s.Id == id, ct);
        return StatusCode(StatusCodes.Status201Created,
            new PujaRegistradaResponse(puja.Id, puja.Monto, PujaService.MontoMinimo(subasta, puja.Monto), subasta.FechaCierre));
    }

    internal static System.Linq.Expressions.Expression<Func<Subasta, SubastaResumenResponse>> Resumen() => s =>
        new SubastaResumenResponse(
            s.Id, s.Articulo.Titulo,
            s.Articulo.Imagenes.OrderBy(i => i.Orden).Select(i => i.Ruta).FirstOrDefault(),
            s.Articulo.Categoria.CategoriaPadre != null ? s.Articulo.Categoria.CategoriaPadre.Nombre : s.Articulo.Categoria.Nombre,
            s.PrecioSalida,
            s.Pujas.OrderByDescending(p => p.Id).Select(p => (decimal?)p.Monto).FirstOrDefault(),
            s.Pujas.Count, s.FechaInicio, s.FechaCierre, s.Estado.ToString());

    private static PujaResponse Puja(Puja p, long? yo) =>
        new(p.Id, p.PostorId == yo ? "Tú" : Ocultar(p.Postor.Correo), p.PostorId == yo, p.Monto, p.CreadaEn, p.Descartada);

    // «jesus.p@correo.com» → «j***s.p», como en el historial del diseño.
    private static string Ocultar(string correo)
    {
        var alias = correo.Split('@')[0];
        return alias.Length <= 3 ? $"{alias[0]}***" : $"{alias[0]}***{alias[^3..]}";
    }
}
