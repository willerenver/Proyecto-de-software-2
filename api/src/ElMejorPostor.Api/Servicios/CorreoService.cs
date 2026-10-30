using System.Net;
using System.Net.Mail;
using System.Threading.Channels;
using Microsoft.EntityFrameworkCore;
using ElMejorPostor.Api.Datos;
using ElMejorPostor.Api.Dominio;

namespace ElMejorPostor.Api.Servicios;

// Lo que usan los servicios para mandar un correo. No espera el envío: lo deja en la cola.
// contieneSecreto: el texto lleva un código de un solo uso (recuperar la contraseña) y no se guarda en la base.
public interface IEnviadorCorreo
{
    Task EnviarAsync(string para, string asunto, string texto, bool contieneSecreto = false, CancellationToken ct = default);
}

public record CorreoEnCola(string Para, string Asunto, string Texto, bool ContieneSecreto = false);

// Quien entrega el correo de verdad (SMTP, o el registro en desarrollo).
public interface IServidorCorreo
{
    Task EntregarAsync(string para, string asunto, string texto, CancellationToken ct);
}

// Cola en memoria: la petición responde enseguida y el correo sale en segundo plano. Así la respuesta
// tarda lo mismo exista o no la cuenta, y una caída del SMTP no hace fallar la pantalla.
// EnvioDeCorreoWorker la vacía enseguida en la tabla correo_pendiente (BuzonDeCorreo).
public class ColaDeCorreo : IEnviadorCorreo
{
    private readonly Channel<CorreoEnCola> _canal = Channel.CreateUnbounded<CorreoEnCola>();

    public ChannelReader<CorreoEnCola> Lector => _canal.Reader;

    public Task EnviarAsync(string para, string asunto, string texto, bool contieneSecreto = false, CancellationToken ct = default) =>
        _canal.Writer.WriteAsync(new CorreoEnCola(para, asunto, texto, contieneSecreto), ct).AsTask();
}

// Guarda en la base los correos de la cola y entrega los que tocan. Si la API se reinicia o el SMTP
// falla, los que no salieron siguen en correo_pendiente y se reintentan: 1, 2, 4… minutos, hasta una hora.
public class BuzonDeCorreo(ElMejorPostorDbContext db, IServidorCorreo servidor, TimeProvider reloj, ILogger<BuzonDeCorreo> log)
{
    public const int PorVuelta = 20;

    // Los correos con un código secreto se entregan una sola vez y nunca se guardan: quien lea la base
    // no debe poder usar un enlace de recuperación. Si ese envío falla, la persona vuelve a pedir el enlace.
    public async Task RecibirAsync(IEnumerable<CorreoEnCola> correos, CancellationToken ct)
    {
        var ahora = reloj.GetUtcNow().UtcDateTime;
        foreach (var c in correos)
        {
            if (!c.ContieneSecreto)
            {
                db.CorreosPendientes.Add(new CorreoPendiente { Para = c.Para, Asunto = c.Asunto, Texto = c.Texto, CreadoEn = ahora, ProximoIntento = ahora });
                continue;
            }
            try
            {
                await servidor.EntregarAsync(c.Para, c.Asunto, c.Texto, ct);
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                log.LogError(e, "No se pudo enviar el correo «{Asunto}»; no se reintenta porque lleva un código secreto", c.Asunto);
            }
        }
        await db.SaveChangesAsync(ct);
    }

    // Devuelve cuántos intentó; si son PorVuelta, puede haber más esperando.
    public async Task<int> EntregarAsync(CancellationToken ct)
    {
        var ahora = reloj.GetUtcNow().UtcDateTime;
        var pendientes = await db.CorreosPendientes.Where(c => c.ProximoIntento <= ahora)
            .OrderBy(c => c.Id).Take(PorVuelta).ToListAsync(ct);
        foreach (var c in pendientes)
        {
            try
            {
                await servidor.EntregarAsync(c.Para, c.Asunto, c.Texto, ct);
                db.CorreosPendientes.Remove(c);
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                c.Intentos++;
                if (c.Intentos >= CorreoPendiente.MaximoDeIntentos)
                {
                    log.LogError(e, "Se descartó el correo «{Asunto}» después de {Intentos} intentos", c.Asunto, c.Intentos);
                    db.CorreosPendientes.Remove(c);
                }
                else
                {
                    c.ProximoIntento = ahora + TimeSpan.FromMinutes(Math.Min(60, Math.Pow(2, c.Intentos - 1)));
                    log.LogWarning(e, "No se pudo enviar el correo «{Asunto}»; se reintenta a las {Hora:HH:mm} UTC", c.Asunto, c.ProximoIntento);
                }
            }
            // Se guarda uno por uno: si la API se cae a mitad de la vuelta, no se reenvían los que ya salieron.
            await db.SaveChangesAsync(ct);
        }
        return pendientes.Count;
    }
}

public class EnvioDeCorreoWorker(ColaDeCorreo cola, IServiceScopeFactory scopes, ILogger<EnvioDeCorreoWorker> log) : BackgroundService
{
    private static readonly TimeSpan Revision = TimeSpan.FromSeconds(30);

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        // Los que salieron de la cola pero todavía no se guardaron (por ejemplo, si la base no respondió).
        var porGuardar = new List<CorreoEnCola>();
        while (!ct.IsCancellationRequested)
        {
            try
            {
                while (cola.Lector.TryRead(out var correo)) porGuardar.Add(correo);
                using var scope = scopes.CreateScope();
                var buzon = scope.ServiceProvider.GetRequiredService<BuzonDeCorreo>();
                if (porGuardar.Count > 0)
                {
                    await buzon.RecibirAsync(porGuardar, ct);
                    porGuardar.Clear();
                }
                while (await buzon.EntregarAsync(ct) == BuzonDeCorreo.PorVuelta) { }
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                log.LogError(e, "Falló el envío de correos; se reintenta en {Segundos} s", Revision.TotalSeconds);
                await Task.Delay(Revision, ct);
                continue;
            }

            // Despierta cuando llega un correo nuevo o, como tarde, a los 30 s para los reintentos.
            using var espera = CancellationTokenSource.CreateLinkedTokenSource(ct);
            espera.CancelAfter(Revision);
            try
            {
                await cola.Lector.WaitToReadAsync(espera.Token);
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
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
        // appsettings.json trae Remitente vacío: sin remitente propio se usa la cuenta SMTP.
        var remitente = string.IsNullOrWhiteSpace(smtp["Remitente"]) ? smtp["Usuario"]! : smtp["Remitente"]!;
        using var mensaje = new MailMessage(remitente, para, asunto, texto);
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
