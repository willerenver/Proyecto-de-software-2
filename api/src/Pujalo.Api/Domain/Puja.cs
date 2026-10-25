namespace Pujalo.Api.Dominio;

// Una oferta. Una vez registrada no se modifica ni se borra.
public class Puja
{
    public long Id { get; set; }
    public long SubastaId { get; set; }
    public Subasta Subasta { get; set; } = null!;
    public long PostorId { get; set; }
    public Usuario Postor { get; set; } = null!;
    public decimal Monto { get; set; }
    public DateTime CreadaEn { get; set; }
}
