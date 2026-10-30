using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ElMejorPostor.Api.Contratos;
using ElMejorPostor.Api.Datos;
using ElMejorPostor.Api.Dominio;
using ElMejorPostor.Api.Servicios;

namespace ElMejorPostor.Api.Controllers;

// Al finalizar una subasta, el ganador ve los datos del vendedor y el vendedor los del ganador.
// Nadie más puede verlos.
[Authorize]
[ApiController]
[Route("api/subastas/{id:long}/contacto")]
public class ContactoController(ElMejorPostorDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ContactoSubastaResponse>> Ver(long id, CancellationToken ct)
    {
        var s = await db.Subastas.AsNoTracking()
            .Include(x => x.Articulo).ThenInclude(a => a.Vendedor)
            .Include(x => x.PujaGanadora).ThenInclude(p => p!.Postor)
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw ReglaException.NoEncontrado("La subasta no existe.");
        if (s.Estado != EstadoSubasta.Finalizada || s.PujaGanadora is null)
            throw ReglaException.Conflicto("La subasta todavía no tiene un ganador.");

        var yo = User.UsuarioId();
        var ganador = s.PujaGanadora.Postor;
        var vendedor = s.Articulo.Vendedor;
        if (yo == ganador.Id)
            return new ContactoSubastaResponse("comprador", s.PujaGanadora.Monto, Contacto(vendedor), s.PlazoPago, s.PagoConfirmadoEn is not null);
        if (yo == vendedor.Id)
            return new ContactoSubastaResponse("vendedor", s.PujaGanadora.Monto, Contacto(ganador), s.PlazoPago, s.PagoConfirmadoEn is not null);
        throw ReglaException.Prohibido("Solo el ganador y el vendedor pueden ver estos datos.");
    }

    private static ContactoResponse Contacto(Usuario u) => new($"{u.Nombre} {u.Apellido}", u.Correo, u.Telefono);
}
