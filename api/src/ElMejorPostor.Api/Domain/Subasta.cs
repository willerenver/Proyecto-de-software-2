namespace ElMejorPostor.Api.Dominio;

public enum EstadoSubasta
{
    Programada,
    Activa,
    Finalizada,
    Desierta,
    Cancelada,
}

public class Subasta
{
    public static readonly int[] DuracionesDias = [1, 3, 5, 7];
    public static readonly TimeSpan VentanaExtension = TimeSpan.FromMinutes(2);
    public static readonly TimeSpan PlazoParaPagar = TimeSpan.FromHours(48);
    // Tope de cualquier monto: así una puja más el incremento nunca desborda decimal(12,2).
    public const decimal MontoMaximo = 999_999_999.99m;
    public static string MensajeMontoMaximo => $"El monto no puede pasar de RD$ {MontoMaximo:N2}.";

    public long Id { get; set; }
    public long ArticuloId { get; set; }
    public Articulo Articulo { get; set; } = null!;
    public decimal PrecioSalida { get; set; }
    public decimal IncrementoMinimo { get; set; }
    public decimal? PrecioReserva { get; set; }
    public DateTime FechaInicio { get; set; }
    public DateTime FechaCierre { get; set; }
    public bool ExtenderAlFinal { get; set; } = true;
    public EstadoSubasta Estado { get; set; } = EstadoSubasta.Programada;
    public long? PujaGanadoraId { get; set; }
    public Puja? PujaGanadora { get; set; }
    public DateTime? CerradaEn { get; set; }

    // El ganador tiene hasta esta fecha para pagar. Si el vendedor no confirma el pago a tiempo,
    // la subasta pasa al siguiente postor (CierreService).
    public DateTime? PlazoPago { get; set; }
    public DateTime? PagoConfirmadoEn { get; set; }
    public DateTime CreadoEn { get; set; }

    // Token de concurrencia: cada puja lo incrementa, así dos pujas simultáneas no pueden quedar ambas como la más alta.
    public int Version { get; set; }

    public List<Puja> Pujas { get; set; } = [];

    public bool EsFinal => Estado is EstadoSubasta.Finalizada or EstadoSubasta.Desierta or EstadoSubasta.Cancelada;

    // Se calcula con la hora y no solo con Estado, para no depender de que el proceso de cierre ya haya pasado.
    public bool AceptaPujas(DateTime ahora) => !EsFinal && FechaInicio <= ahora && ahora < FechaCierre;
}
