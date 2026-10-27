using System.Net;
using System.Net.Mail;

namespace Pujalo.Api.Servicios;

public interface IEnviadorCorreo
{
    Task EnviarAsync(string para, string asunto, string texto, CancellationToken ct = default);
}

// Envía por SMTP con la sección «Smtp» de la configuración (Host, Puerto, Usuario, Contrasena, Remitente).
public class SmtpEnviadorCorreo(IConfiguration config) : IEnviadorCorreo
{
    public async Task EnviarAsync(string para, string asunto, string texto, CancellationToken ct = default)
    {
        var smtp = config.GetSection("Smtp");
        using var cliente = new SmtpClient(smtp["Host"], smtp.GetValue("Puerto", 587))
        {
            EnableSsl = smtp.GetValue("Ssl", true),
            Credentials = new NetworkCredential(smtp["Usuario"], smtp["Contrasena"]),
        };
        using var mensaje = new MailMessage(smtp["Remitente"] ?? smtp["Usuario"]!, para, asunto, texto);
        await cliente.SendMailAsync(mensaje, ct);
    }
}

// Sin SMTP configurado (desarrollo), el correo se escribe en el registro de la API.
public class RegistroEnviadorCorreo(ILogger<RegistroEnviadorCorreo> log) : IEnviadorCorreo
{
    public Task EnviarAsync(string para, string asunto, string texto, CancellationToken ct = default)
    {
        log.LogWarning("Correo sin enviar (no hay SMTP configurado) para {Para}: {Asunto}\n{Texto}", para, asunto, texto);
        return Task.CompletedTask;
    }
}
