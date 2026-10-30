using ElMejorPostor.Api.Dominio;

namespace ElMejorPostor.Api.Servicios;

// Correos que avisan a cada parte de lo que pasa en sus subastas. Los servicios los preparan mientras
// trabajan y los envían solo si los cambios se guardaron; si algo falla, se descartan.
public class Avisos(IEnviadorCorreo correo, IConfiguration config)
{
    private readonly List<(string Para, string Asunto, string Texto)> _pendientes = [];

    private string Enlace(Subasta s) => $"{(config["Web:Url"] ?? "http://localhost:5173").TrimEnd('/')}/subastas/{s.Id}";
    private static string Rd(decimal monto) => $"RD$ {monto:N0}";
    private static string Hora(DateTime? utc) => utc is null ? "" : $"{utc:dd/MM/yyyy HH:mm} (hora UTC)";

    private void Agregar(Usuario para, string asunto, string cuerpo, Subasta s) =>
        _pendientes.Add((para.Correo, asunto, $"Hola, {para.Nombre}:\n\n{cuerpo}\n\nVer la subasta: {Enlace(s)}\n\nEl Mejor Postor"));

    public void TeSuperaron(Usuario anterior, Subasta s, decimal nuevoMonto) =>
        Agregar(anterior, $"Te superaron en «{s.Articulo.Titulo}»",
            $"Alguien pujó {Rd(nuevoMonto)} y ya no vas ganando. Para volver a ganar puja {Rd(nuevoMonto + s.IncrementoMinimo)} o más antes del cierre.", s);

    public void SuperaronTuMaximo(Usuario postor, Subasta s, decimal maximo, decimal actual) =>
        Agregar(postor, $"Superaron tu puja automática en «{s.Articulo.Titulo}»",
            $"Tu puja automática llegó a su máximo de {Rd(maximo)} y alguien va ganando con {Rd(actual)}. Si quieres seguir, puja {Rd(actual + s.IncrementoMinimo)} o más.", s);

    public void Ganaste(Usuario ganador, Subasta s, decimal monto) =>
        Agregar(ganador, $"¡Ganaste «{s.Articulo.Titulo}»!",
            $"Ganaste la subasta con {Rd(monto)}. Tienes hasta el {Hora(s.PlazoPago)} para ponerte de acuerdo con el vendedor y pagar; si no, la subasta pasa al siguiente postor.", s);

    public void Vendiste(Usuario vendedor, Subasta s, Usuario ganador, decimal monto) =>
        Agregar(vendedor, $"Vendiste «{s.Articulo.Titulo}»",
            $"{ganador.Nombre} {ganador.Apellido} ganó con {Rd(monto)}. Escríbele para coordinar el pago y la entrega, y confirma el pago en El Mejor Postor cuando lo recibas.", s);

    public void SinGanador(Usuario vendedor, Subasta s) =>
        Agregar(vendedor, $"«{s.Articulo.Titulo}» terminó sin ganador",
            "La subasta cerró sin pujas o sin alcanzar el precio de reserva. Puedes volver a publicarla cuando quieras.", s);

    public void PerdisteFaltaDePago(Usuario incumplido, Subasta s) =>
        Agregar(incumplido, $"Venció el plazo para pagar «{s.Articulo.Titulo}»",
            "El vendedor no confirmó tu pago en 48 horas, así que la subasta pasó al siguiente postor.", s);

    public void PasoATi(Usuario nuevo, Subasta s, decimal monto) =>
        Agregar(nuevo, $"¡Ahora ganaste «{s.Articulo.Titulo}»!",
            $"El ganador anterior no pagó a tiempo y tu puja de {Rd(monto)} es la ganadora. Tienes hasta el {Hora(s.PlazoPago)} para pagar.", s);

    public void PagoConfirmado(Usuario comprador, Subasta s) =>
        Agregar(comprador, $"El vendedor confirmó tu pago de «{s.Articulo.Titulo}»",
            "El vendedor recibió tu pago. Solo falta coordinar la entrega.", s);

    public async Task EnviarAsync(CancellationToken ct = default)
    {
        foreach (var (para, asunto, texto) in _pendientes)
            await correo.EnviarAsync(para, asunto, texto, ct: ct);
        _pendientes.Clear();
    }

    public void Descartar() => _pendientes.Clear();
}
