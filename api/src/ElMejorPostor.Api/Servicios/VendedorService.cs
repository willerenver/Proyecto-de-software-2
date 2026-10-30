using Microsoft.EntityFrameworkCore;
using ElMejorPostor.Api.Contratos;
using ElMejorPostor.Api.Datos;
using ElMejorPostor.Api.Dominio;

namespace ElMejorPostor.Api.Servicios;

// Lo que el vendedor puede hacer con su subasta: corregirla o retirarla mientras nadie ha pujado,
// quitarle fotos y confirmar que recibió el pago del ganador.
public class VendedorService(ElMejorPostorDbContext db, TimeProvider reloj, Avisos? avisos = null, INotificadorSubastas? tiempoReal = null)
{
    private DateTime Ahora => reloj.GetUtcNow().UtcDateTime;

    // Reglas de los datos de una publicación; las usa también la creación (SubastasController).
    public static async Task ValidarAsync(ElMejorPostorDbContext db, CrearSubastaRequest r, CancellationToken ct)
    {
        if (!Condiciones.Validas.Contains(r.Condicion))
            throw new ReglaException("Elige un estado válido para el artículo.");
        if (!Subasta.DuracionesDias.Contains(r.DuracionDias))
            throw new ReglaException("La duración debe ser de 1, 3, 5 o 7 días.");
        if (r.PrecioSalida > Subasta.MontoMaximo || r.IncrementoMinimo > Subasta.MontoMaximo || r.PrecioReserva > Subasta.MontoMaximo)
            throw new ReglaException(Subasta.MensajeMontoMaximo);
        if (r.PrecioReserva is not null && r.PrecioReserva < r.PrecioSalida)
            throw new ReglaException("La reserva no puede ser menor que el precio de salida.");
        if (!r.PermiteRetiro && !r.PermiteEnvio)
            throw new ReglaException("Elige al menos una forma de entrega.");
        if (!await db.Categorias.AnyAsync(c => c.Id == r.CategoriaId && c.Activa, ct))
            throw new ReglaException("Elige una categoría válida.");
    }

    public async Task<Subasta> EditarAsync(long id, long vendedorId, CrearSubastaRequest r, CancellationToken ct = default)
    {
        var s = await SinPujasAsync(id, vendedorId, "modificar", ct);
        await ValidarAsync(db, r, ct);

        // Una subasta que ya empezó conserva su inicio; una programada puede moverlo.
        var ahora = Ahora;
        var inicio = s.Estado == EstadoSubasta.Programada ? r.FechaInicio.ToUniversalTime() : s.FechaInicio;
        if (s.Estado == EstadoSubasta.Programada && inicio < ahora.AddMinutes(-1))
            throw new ReglaException("La fecha de inicio no puede estar en el pasado.");
        var cierre = inicio.AddDays(r.DuracionDias);
        if (cierre <= ahora)
            throw new ReglaException("Con esa duración la subasta ya habría terminado. Elige más días.");

        s.Articulo.Titulo = r.Titulo.Trim();
        s.Articulo.Descripcion = r.Descripcion.Trim();
        s.Articulo.Condicion = r.Condicion;
        s.Articulo.CategoriaId = r.CategoriaId;
        s.Articulo.PermiteRetiro = r.PermiteRetiro;
        s.Articulo.PermiteEnvio = r.PermiteEnvio;
        s.PrecioSalida = r.PrecioSalida;
        s.IncrementoMinimo = r.IncrementoMinimo;
        s.PrecioReserva = r.PrecioReserva;
        s.FechaInicio = inicio;
        s.FechaCierre = cierre;
        s.ExtenderAlFinal = r.ExtenderAlFinal;
        s.Estado = inicio <= ahora ? EstadoSubasta.Activa : EstadoSubasta.Programada;
        await GuardarAsync(s, ct);
        return s;
    }

    public async Task RetirarAsync(long id, long vendedorId, CancellationToken ct = default)
    {
        var s = await SinPujasAsync(id, vendedorId, "retirar", ct);
        s.Estado = EstadoSubasta.Cancelada;
        s.CerradaEn = Ahora;
        await GuardarAsync(s, ct);
    }

    public async Task QuitarImagenAsync(long id, long vendedorId, string ruta, CancellationToken ct = default)
    {
        var s = await SinPujasAsync(id, vendedorId, "modificar", ct);
        var imagenes = await db.Imagenes.Where(i => i.ArticuloId == s.ArticuloId).OrderBy(i => i.Orden).ToListAsync(ct);
        var imagen = imagenes.FirstOrDefault(i => i.Ruta == ruta) ?? throw ReglaException.NoEncontrado("La foto no existe.");
        if (imagenes.Count == 1)
            throw new ReglaException("El artículo debe tener al menos una foto.");

        // La última ocupa el lugar de la borrada, para que el orden siga siendo 1..n (máximo 10).
        // Son dos guardados por el índice único (articulo_id, orden); la transacción evita quedar a medias.
        await using var transaccion = await db.Database.BeginTransactionAsync(ct);
        db.Imagenes.Remove(imagen);
        await db.SaveChangesAsync(ct);
        var ultima = imagenes[^1];
        if (ultima != imagen)
        {
            ultima.Orden = imagen.Orden;
            await db.SaveChangesAsync(ct);
        }
        await transaccion.CommitAsync(ct);
    }

    // El vendedor confirma que recibió el pago; con eso la subasta ya no pasa al siguiente postor.
    public async Task ConfirmarPagoAsync(long id, long vendedorId, CancellationToken ct = default)
    {
        var s = await DelVendedorAsync(id, vendedorId, ct);
        if (s.Estado != EstadoSubasta.Finalizada || s.PujaGanadoraId is null)
            throw ReglaException.Conflicto("La subasta todavía no tiene un ganador.");
        if (s.PagoConfirmadoEn is not null)
            throw ReglaException.Conflicto("Ya confirmaste el pago de esta subasta.");
        if (s.PlazoPago is not null && s.PlazoPago <= Ahora)
            throw ReglaException.Conflicto("Venció el plazo de pago; la subasta pasa al siguiente postor.");
        s.PagoConfirmadoEn = Ahora;
        await GuardarAsync(s, ct);
        if (avisos is not null)
        {
            var comprador = await db.Pujas.Where(p => p.Id == s.PujaGanadoraId).Select(p => p.Postor).SingleAsync(ct);
            avisos.PagoConfirmado(comprador, s);
            await avisos.EnviarAsync(ct);
        }
    }

    private async Task<Subasta> DelVendedorAsync(long id, long vendedorId, CancellationToken ct)
    {
        var s = await db.Subastas.Include(x => x.Articulo).FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw ReglaException.NoEncontrado("La subasta no existe.");
        if (s.Articulo.VendedorId != vendedorId)
            throw ReglaException.Prohibido("Solo el vendedor puede hacer esto con la subasta.");
        return s;
    }

    private async Task<Subasta> SinPujasAsync(long id, long vendedorId, string accion, CancellationToken ct)
    {
        var s = await DelVendedorAsync(id, vendedorId, ct);
        if (s.EsFinal)
            throw ReglaException.Conflicto($"La subasta ya terminó; no se puede {accion}.");
        if (await db.Pujas.AnyAsync(p => p.SubastaId == id, ct))
            throw ReglaException.Conflicto($"La subasta ya tiene pujas; no se puede {accion}.");
        return s;
    }

    // Si entró una puja mientras tanto, Version cambió y no se guarda nada.
    private async Task GuardarAsync(Subasta s, CancellationToken ct)
    {
        s.Version++;
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            db.ChangeTracker.Clear();
            throw ReglaException.Conflicto("Alguien pujó mientras hacías el cambio. Revisa la subasta e intenta de nuevo.");
        }
        if (tiempoReal is not null) await tiempoReal.CambioAsync([s.Id], ct);
    }
}
