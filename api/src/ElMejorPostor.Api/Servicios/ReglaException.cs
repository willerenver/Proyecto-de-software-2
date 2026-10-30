namespace ElMejorPostor.Api.Servicios;

// Una regla de negocio no se cumplió. El mensaje se muestra tal cual al usuario.
public class ReglaException(string mensaje, int estadoHttp = StatusCodes.Status400BadRequest) : Exception(mensaje)
{
    public int EstadoHttp { get; } = estadoHttp;

    public static ReglaException Conflicto(string mensaje) => new(mensaje, StatusCodes.Status409Conflict);
    public static ReglaException NoEncontrado(string mensaje) => new(mensaje, StatusCodes.Status404NotFound);
    public static ReglaException Prohibido(string mensaje) => new(mensaje, StatusCodes.Status403Forbidden);
}
