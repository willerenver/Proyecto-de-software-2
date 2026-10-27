using System.ComponentModel.DataAnnotations;

namespace Pujalo.Api.Contratos;

// Los largos coinciden con las columnas del diccionario de datos.

public record RegistroRequest(
    [Required(ErrorMessage = "Escribe tu nombre."), StringLength(60, ErrorMessage = "Máximo 60 caracteres.")] string Nombre,
    [Required(ErrorMessage = "Escribe tu apellido."), StringLength(60, ErrorMessage = "Máximo 60 caracteres.")] string Apellido,
    [Required(ErrorMessage = "Escribe tu correo."), EmailAddress(ErrorMessage = "Escribe un correo válido, por ejemplo nombre@correo.com."), StringLength(120, ErrorMessage = "Máximo 120 caracteres.")] string Correo,
    [Required(ErrorMessage = "Escribe tu teléfono."), RegularExpression(@"^[\d\s-]{7,20}$", ErrorMessage = "Escribe un teléfono válido, por ejemplo 809-555-0142.")] string Telefono,
    [Required(ErrorMessage = "Escribe tu cédula."), RegularExpression(@"^\d{3}-?\d{7}-?\d$", ErrorMessage = "La cédula tiene 11 dígitos. Ejemplo: 001-1234567-8.")] string Cedula,
    [Required(ErrorMessage = "Escribe una contraseña."), RegularExpression(@"^(?=.*\d).{8,}$", ErrorMessage = "Mínimo 8 caracteres, con al menos un número.")] string Contrasena);

public record LoginRequest(
    [Required(ErrorMessage = "Escribe tu correo."), EmailAddress(ErrorMessage = "Escribe un correo válido.")] string Correo,
    [Required(ErrorMessage = "Escribe tu contraseña.")] string Contrasena);

public record SesionResponse(string Token, DateTime Expira, UsuarioResponse Usuario);

public record UsuarioResponse(long Id, string Nombre, string Apellido, string Iniciales, string Correo, string Rol, int MiembroDesde);

public record CategoriaResponse(int Id, string Nombre, IReadOnlyList<CategoriaResponse> Subcategorias);

public record SubastaResumenResponse(
    long Id, string Titulo, string? Foto, string Categoria, decimal PrecioSalida, decimal? PujaActual, int NumeroPujas, DateTime FechaInicio, DateTime FechaCierre, string Estado);

public record VendedorResponse(long Id, string Nombre, string Iniciales, int Ventas);

public record PujaResponse(long Id, string Postor, bool EsMia, decimal Monto, DateTime CreadaEn, bool Descartada, bool Automatica);

public record SubastaDetalleResponse(
    long Id, string Titulo, string Descripcion, string Condicion, string Categoria, string? Subcategoria,
    IReadOnlyList<string> Fotos, VendedorResponse Vendedor, bool PermiteRetiro, bool PermiteEnvio,
    decimal PrecioSalida, decimal IncrementoMinimo, bool TienePrecioReserva, bool ReservaAlcanzada,
    decimal? PujaActual, decimal MontoMinimo, int NumeroPujas, DateTime FechaInicio, DateTime FechaCierre,
    bool ExtenderAlFinal, string Estado, bool AceptaPujas, PujaResponse? PujaGanadora, IReadOnlyList<PujaResponse> Historial,
    DateTime? PlazoPago, bool PagoConfirmado, bool PuedeEditar, decimal? MiPujaAutomatica);

public record CrearSubastaRequest(
    [Required(ErrorMessage = "Escribe un título."), StringLength(80, MinimumLength = 10, ErrorMessage = "El título debe tener entre 10 y 80 caracteres.")] string Titulo,
    [Required(ErrorMessage = "Describe el artículo."), StringLength(2000, ErrorMessage = "Máximo 2000 caracteres.")] string Descripcion,
    [Required(ErrorMessage = "Elige el estado del artículo.")] string Condicion,
    [Range(1, int.MaxValue, ErrorMessage = "Elige una categoría.")] int CategoriaId,
    [Range(typeof(decimal), "0.01", "9999999999.99", ErrorMessage = "El precio de salida debe ser mayor que 0.")] decimal PrecioSalida,
    [Range(typeof(decimal), "0.01", "9999999999.99", ErrorMessage = "El incremento mínimo debe ser mayor que 0.")] decimal IncrementoMinimo,
    decimal? PrecioReserva,
    DateTime FechaInicio,
    int DuracionDias,
    bool ExtenderAlFinal = true,
    bool PermiteRetiro = true,
    bool PermiteEnvio = false);

public record PujaAutomaticaRequest([Range(typeof(decimal), "0.01", "9999999999.99", ErrorMessage = "Escribe un máximo mayor que 0.")] decimal Maximo);

public record CrearPujaRequest([Range(typeof(decimal), "0.01", "9999999999.99", ErrorMessage = "Escribe un monto mayor que 0.")] decimal Monto);

public record PujaRegistradaResponse(long Id, decimal Monto, decimal PujaActual, bool VasGanando, decimal MontoMinimoSiguiente, DateTime FechaCierre);

public record MiPujaResponse(
    long SubastaId, string Titulo, string? Foto, decimal MiPuja, decimal PujaActual, decimal MontoMinimo, DateTime FechaCierre, string Situacion);

public record ContactoResponse(string Nombre, string Correo, string? Telefono);

public record MiVentaResponse(
    long SubastaId, string Titulo, string? Foto, decimal PrecioSalida, decimal? PujaActual, int NumeroPujas,
    DateTime FechaInicio, DateTime FechaCierre, string Estado, ContactoResponse? Ganador,
    DateTime? PlazoPago, bool PagoConfirmado);

// Lo que ve cada parte de una subasta finalizada para coordinar el pago y la entrega.
public record ContactoSubastaResponse(string Rol, decimal Monto, ContactoResponse Contraparte, DateTime? PlazoPago, bool PagoConfirmado);

// Datos para el formulario de edición; solo los ve el vendedor (incluye el precio de reserva).
public record SubastaEdicionResponse(
    long Id, string Titulo, string Descripcion, string Condicion, int CategoriaId, decimal PrecioSalida, decimal IncrementoMinimo,
    decimal? PrecioReserva, DateTime FechaInicio, int DuracionDias, bool ExtenderAlFinal, bool PermiteRetiro, bool PermiteEnvio,
    string Estado, IReadOnlyList<string> Fotos);

public record CategoriaAdminResponse(int Id, string Nombre, int? CategoriaPadreId, bool Activa, int Articulos);

public record GuardarCategoriaRequest(
    [Required(ErrorMessage = "Escribe el nombre."), StringLength(60, ErrorMessage = "Máximo 60 caracteres.")] string Nombre,
    int? CategoriaPadreId,
    bool Activa = true);

public record RecuperarRequest(
    [Required(ErrorMessage = "Escribe tu correo."), EmailAddress(ErrorMessage = "Escribe un correo válido.")] string Correo);

public record RestablecerRequest(
    [Required(ErrorMessage = "El enlace no es válido.")] string Codigo,
    [Required(ErrorMessage = "Escribe una contraseña."), RegularExpression(@"^(?=.*\d).{8,}$", ErrorMessage = "Mínimo 8 caracteres, con al menos un número.")] string Contrasena);

public record UsuarioAdminResponse(long Id, string Nombre, string Correo, string Rol, bool Activo, DateTime CreadoEn);

public record CambiarActivoRequest(bool Activo);
