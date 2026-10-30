using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ElMejorPostor.Api.Contratos;
using ElMejorPostor.Api.Datos;
using ElMejorPostor.Api.Dominio;
using ElMejorPostor.Api.Servicios;

namespace ElMejorPostor.Api.Controllers;

[ApiController]
[Route("api/subastas")]
public class SubastasController(ElMejorPostorDbContext db, PujaService pujas, TimeProvider reloj, IWebHostEnvironment entorno, INotificadorSubastas tiempoReal) : ControllerBase
{
    private DateTime Ahora => reloj.GetUtcNow().UtcDateTime;

    // Subastas abiertas, por defecto ordenadas por hora de cierre (pantalla 01).
    [HttpGet]
    public async Task<IReadOnlyList<SubastaResumenResponse>> Listar(int? categoriaId, string? buscar, string orden = "cierre",
        int pagina = 1, int porPagina = 24, CancellationToken ct = default)
    {
        var ahora = Ahora;
        var q = db.Subastas.AsNoTracking()
            .Where(s => s.Estado != EstadoSubasta.Cancelada && s.Estado != EstadoSubasta.Finalizada && s.Estado != EstadoSubasta.Desierta)
            .Where(s => s.FechaInicio <= ahora && s.FechaCierre > ahora);
        if (categoriaId is not null)
            q = q.Where(s => s.Articulo.CategoriaId == categoriaId || s.Articulo.Categoria.CategoriaPadreId == categoriaId);
        if (!string.IsNullOrWhiteSpace(buscar))
            q = q.Where(s => EF.Functions.Like(s.Articulo.Titulo, Paginacion.PatronLike(buscar), "\\"));

        // Se ordena en la base para poder paginar; el id desempata para que las páginas no se repitan.
        q = orden switch
        {
            "precio" => q.OrderBy(s => s.Pujas.Max(p => (decimal?)p.Monto) ?? s.PrecioSalida).ThenBy(s => s.Id),
            "pujas" => q.OrderByDescending(s => s.Pujas.Count).ThenBy(s => s.Id),
            _ => q.OrderBy(s => s.FechaCierre).ThenBy(s => s.Id),
        };
        return await q.Select(Resumen()).PaginarAsync(pagina, porPagina, Response, ct);
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
        var miAutomatica = yo is null ? null : await db.PujasAutomaticas.Where(a => a.SubastaId == id && a.PostorId == yo && a.Activa)
            .Select(a => (decimal?)a.Maximo).FirstOrDefaultAsync(ct);
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
            yo == v.Id && !s.EsFinal && historial.Count == 0, miAutomatica);
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
        await tiempoReal.CambioAsync([subasta.Id], ct);
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

        if (fotos.Count == 0)
            throw new ReglaException("Elige al menos una foto.");

        // Primero se revisan todas, para no dejar archivos sueltos si una no sirve.
        // El tipo se decide por el contenido del archivo, no por lo que declara el navegador.
        var extensiones = new List<string>();
        foreach (var foto in fotos)
        {
            if (foto.Length > ImagenArticulo.PesoMaximo)
                throw new ReglaException("Cada foto puede pesar hasta 10 MB.");
            await using var contenido = foto.OpenReadStream();
            extensiones.Add(await ImagenArticulo.ExtensionPorContenidoAsync(contenido, ct)
                ?? throw new ReglaException($"«{Path.GetFileName(foto.FileName)}» no es una foto JPG o PNG."));
        }

        var carpeta = Path.Combine(entorno.WebRootPath ?? Path.Combine(entorno.ContentRootPath, "wwwroot"), "fotos");
        Directory.CreateDirectory(carpeta);
        var orden = (short)(subasta.Articulo.Imagenes.Select(i => (int)i.Orden).DefaultIfEmpty(0).Max() + 1);
        for (var i = 0; i < fotos.Count; i++)
        {
            var nombre = $"{Guid.NewGuid():N}{extensiones[i]}";
            await using (var destino = System.IO.File.Create(Path.Combine(carpeta, nombre)))
                await fotos[i].CopyToAsync(destino, ct);
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
        // Una puja automática de otro pudo responder enseguida.
        var lider = await db.Pujas.AsNoTracking().Where(p => p.SubastaId == id).OrderByDescending(p => p.Id)
            .Select(p => new { p.Monto, p.PostorId }).FirstAsync(ct);
        return StatusCode(StatusCodes.Status201Created,
            new PujaRegistradaResponse(puja.Id, puja.Monto, lider.Monto, lider.PostorId == puja.PostorId,
                PujaService.MontoMinimo(subasta, lider.Monto), subasta.FechaCierre));
    }

    [Authorize]
    [HttpPut("{id:long}/puja-automatica")]
    public async Task<IActionResult> ProgramarAutomatica(long id, PujaAutomaticaRequest r, CancellationToken ct)
    {
        await pujas.ProgramarAutomaticaAsync(id, User.UsuarioId(), r.Maximo, ct);
        return NoContent();
    }

    [Authorize]
    [HttpDelete("{id:long}/puja-automatica")]
    public async Task<IActionResult> CancelarAutomatica(long id, CancellationToken ct)
    {
        await pujas.CancelarAutomaticaAsync(id, User.UsuarioId(), ct);
        return NoContent();
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
        new(p.Id, p.PostorId == yo ? "Tú" : Ocultar(p.Postor.Correo), p.PostorId == yo, p.Monto, p.CreadaEn, p.Descartada, p.Automatica);

    // «jesus.p@correo.com» → «j***s.p», como en el historial del diseño.
    private static string Ocultar(string correo)
    {
        var alias = correo.Split('@')[0];
        return alias.Length <= 3 ? $"{alias[0]}***" : $"{alias[0]}***{alias[^3..]}";
    }
}
