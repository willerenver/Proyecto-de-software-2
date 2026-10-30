namespace ElMejorPostor.Api.Dominio;

// Una oferta. Una vez registrada no se modifica ni se borra; solo se marca como descartada
// cuando su postor ganó y no pagó a tiempo.
public class Puja
{
    public long Id { get; set; }
    public long SubastaId { get; set; }
    public Subasta Subasta { get; set; } = null!;
    public long PostorId { get; set; }
    public Usuario Postor { get; set; } = null!;
    public decimal Monto { get; set; }
    public DateTime CreadaEn { get; set; }
    public bool Descartada { get; set; }

    // La registró el sistema por una puja automática.
    public bool Automatica { get; set; }
}
