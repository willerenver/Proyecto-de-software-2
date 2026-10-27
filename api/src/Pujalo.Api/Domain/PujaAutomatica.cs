namespace Pujalo.Api.Dominio;

// «Subir por mí hasta un máximo»: el sistema puja en nombre del postor solo lo necesario para que vaya ganando.
// Una por postor y subasta; se desactiva cuando alguien supera su máximo o el postor la cancela.
public class PujaAutomatica
{
    public long Id { get; set; }
    public long SubastaId { get; set; }
    public long PostorId { get; set; }
    public Usuario Postor { get; set; } = null!;
    public decimal Maximo { get; set; }
    public DateTime CreadaEn { get; set; }
    public bool Activa { get; set; } = true;
}
