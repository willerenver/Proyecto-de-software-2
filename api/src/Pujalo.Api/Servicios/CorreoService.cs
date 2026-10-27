using System.Net;
using System.Net.Mail;
using System.Threading.Channels;

namespace Pujalo.Api.Servicios;

// Lo que usan los servicios para mandar un correo. No espera el envío: lo deja en la cola.
public interface IEnviadorCorreo
{
    Task EnviarAsync(string para, string asunto, string texto, CancellationToken ct = default);
}

// Quien entrega el correo de verdad (SMTP, o el registro en desarrollo).
public interface IServidorCorreo
{
    Task EntregarAsync(string para, string asunto, string texto, CancellationToken ct);
}

// Cola en memoria: la petición responde enseguida y el correo sale en segundo plano. Así la respuesta
// tarda lo mismo exista o no la cuenta, y una caída del SMTP no hace fallar la pantalla.
public class ColaDeCorreo : IEnviadorCorreo
{
    private readonly Channel<(string Para, string Asunto, string Texto)> _canal = Channel.CreateUnbounded<(string, string, string)>();

    public ChannelReader<(string Para, string Asunto, string Texto)> Lector => _canal.Reader;

    public Task EnviarAsync(string para, string asunto, string texto, CancellationToken ct = default) =>
        _canal.Writer.WriteAsync((para, asunto, texto), ct).AsTask();
}

public class EnvioDeCorreoWorker(ColaDeCorreo cola, IServidorCorreo servidor, ILogger<EnvioDeCorreoWorker> log) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        await foreach (var (para, asunto, texto) in cola.Lector.ReadAllAsync(ct))
        {
            try
            {
                await servidor.EntregarAsync(para, asunto, texto, ct);
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                log.LogError(e, "No se pudo enviar el correo «{Asunto}»", asunto);
            }
        }
    }
}

// Envía por SMTP con la sección «Smtp» de la configuración (Host, Puerto, Usuario, Contrasena, Remitente).
public class SmtpServidorCorreo(IConfiguration config) : IServidorCorreo
{
    public async Task EntregarAsync(string para, string asunto, string texto, CancellationToken ct)
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

// Solo en Development y sin SMTP: el correo se escribe en la consola para poder probar los enlaces.
// Program.cs impide usarlo en cualquier otro entorno, porque el texto lleva códigos de un solo uso.
public class RegistroServidorCorreo(ILogger<RegistroServidorCorreo> log) : IServidorCorreo
{
    public Task EntregarAsync(string para, string asunto, string texto, CancellationToken ct)
    {
        log.LogWarning("[Solo desarrollo] Correo para {Para}: {Asunto}\n{Texto}", para, asunto, texto);
        return Task.CompletedTask;
    }
}
