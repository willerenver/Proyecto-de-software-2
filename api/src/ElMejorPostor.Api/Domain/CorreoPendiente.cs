namespace ElMejorPostor.Api.Dominio;

// Un correo que todavía no se entregó. Se borra al enviarse; si el servidor falla, se reintenta más tarde.
// Como vive en la base, un reinicio de la API no lo pierde.
public class CorreoPendiente
{
    public const int MaximoDeIntentos = 8;

    public long Id { get; set; }
    public string Para { get; set; } = "";
    public string Asunto { get; set; } = "";
    public string Texto { get; set; } = "";
    public DateTime CreadoEn { get; set; }
    public int Intentos { get; set; }
    public DateTime ProximoIntento { get; set; }
}
