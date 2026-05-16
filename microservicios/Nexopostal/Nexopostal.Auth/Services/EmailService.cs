using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace NexoPostal.Auth.Services;

public interface IEmailService
{
    Task SendPasswordResetEmailAsync(string toEmail, string toName, string resetLink);
}

public class SmtpEmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IConfiguration config, ILogger<SmtpEmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendPasswordResetEmailAsync(string toEmail, string toName, string resetLink)
    {
        var emailSettings = _config.GetSection("Email");
        var host = emailSettings["SmtpHost"] ?? string.Empty;
        var port = int.TryParse(emailSettings["SmtpPort"], out var p) ? p : 587;
        var user = emailSettings["SmtpUser"] ?? string.Empty;
        var pass = emailSettings["SmtpPass"] ?? string.Empty;
        var from = emailSettings["From"] ?? "noreply@nexopostal.com";
        var fromName = emailSettings["FromName"] ?? "NexoPostal";

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(fromName, from));
        message.To.Add(new MailboxAddress(toName, toEmail));
        message.Subject = "Recuperación de contraseña — NexoPostal";

        var bodyBuilder = new BodyBuilder
        {
            HtmlBody = BuildHtmlBody(toName, resetLink),
            TextBody = $"Hola {toName},\n\nRestablece tu contraseña en: {resetLink}\n\nEl enlace caduca en 2 horas.\n\nSi no solicitaste esto, ignora este correo.\n\n— NexoPostal"
        };

        message.Body = bodyBuilder.ToMessageBody();

        try
        {
            using var client = new SmtpClient();
            await client.ConnectAsync(host, port, SecureSocketOptions.StartTlsWhenAvailable);

            if (!string.IsNullOrEmpty(user) && !string.IsNullOrEmpty(pass))
                await client.AuthenticateAsync(user, pass);

            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            _logger.LogInformation("Email de recuperación enviado a {Email}", toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al enviar email de recuperación a {Email}", toEmail);
            // No relanzamos la excepción: el endpoint siempre responde 200 OK
            // para no revelar si el email está registrado ni exponer errores SMTP.
        }
    }

    private static string BuildHtmlBody(string nombre, string enlace) => $"""
        <!DOCTYPE html>
        <html lang="es">
        <head><meta charset="UTF-8"><meta name="viewport" content="width=device-width,initial-scale=1"></head>
        <body style="margin:0;padding:0;background:#f3f4f6;font-family:Arial,Helvetica,sans-serif">
          <table width="100%" cellpadding="0" cellspacing="0" style="background:#f3f4f6;padding:32px 16px">
            <tr><td align="center">
              <table width="600" cellpadding="0" cellspacing="0" style="max-width:600px;width:100%">

                <!-- HEADER -->
                <tr>
                  <td style="background:#1A237E;padding:32px 40px;border-radius:12px 12px 0 0;text-align:center">
                    <h1 style="color:#FFC107;font-size:2rem;margin:0;font-weight:700;letter-spacing:-0.5px">NexoPostal</h1>
                    <p style="color:rgba(255,255,255,0.75);margin:6px 0 0;font-size:0.9rem">Tu mensajería de confianza</p>
                  </td>
                </tr>

                <!-- BODY -->
                <tr>
                  <td style="background:#ffffff;padding:40px;border:1px solid #e5e7eb;border-top:none;border-radius:0 0 12px 12px">
                    <h2 style="color:#1A237E;margin:0 0 16px;font-size:1.4rem">Recuperar contraseña</h2>
                    <p style="color:#374151;margin:0 0 12px">Hola <strong>{nombre}</strong>,</p>
                    <p style="color:#374151;margin:0 0 12px">Hemos recibido una solicitud para restablecer la contraseña de tu cuenta de NexoPostal.</p>
                    <p style="color:#374151;margin:0 0 28px">Haz clic en el botón para crear una nueva contraseña. El enlace es válido durante <strong>2 horas</strong>.</p>

                    <div style="text-align:center;margin:0 0 32px">
                      <a href="{enlace}"
                         style="background:#1A237E;color:#ffffff;text-decoration:none;padding:16px 36px;border-radius:8px;font-weight:600;font-size:1rem;display:inline-block;letter-spacing:0.3px">
                        Restablecer contraseña →
                      </a>
                    </div>

                    <p style="color:#6b7280;font-size:0.85rem;margin:0 0 12px">Si no solicitaste esto, ignora este correo. Tu contraseña no cambiará.</p>
                    <p style="color:#6b7280;font-size:0.85rem;margin:0 0 24px">Si el botón no funciona, copia este enlace en tu navegador:<br>
                      <a href="{enlace}" style="color:#1A237E;word-break:break-all;font-size:0.8rem">{enlace}</a>
                    </p>

                    <hr style="border:none;border-top:1px solid #e5e7eb;margin:0 0 20px">
                    <p style="color:#9ca3af;font-size:0.75rem;text-align:center;margin:0">© 2026 NexoPostal. Todos los derechos reservados.</p>
                  </td>
                </tr>

              </table>
            </td></tr>
          </table>
        </body>
        </html>
        """;
}
