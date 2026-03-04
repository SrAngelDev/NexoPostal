using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Nexopostal.Ciudadano.Models;

namespace Nexopostal.Ciudadano.Services;

/// <summary>
/// Servicio de email usando MailKit.
/// Envía la factura y etiqueta como PDFs adjuntos al correo del remitente.
/// Compatible con cualquier servidor SMTP (Gmail, Mailtrap, etc.)
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Envía el email de confirmación de envío con factura y etiqueta adjuntas
    /// </summary>
    Task EnviarConfirmacionEnvio(Envio envio, byte[] facturaPdf, byte[] etiquetaPdf);
}

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task EnviarConfirmacionEnvio(Envio envio, byte[] facturaPdf, byte[] etiquetaPdf)
    {
        var smtpSettings = _configuration.GetSection("SmtpSettings");
        var fromName = smtpSettings["FromName"] ?? "NexoPostal";
        var fromEmail = smtpSettings["FromEmail"] ?? "no-reply@nexopostal.es";

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(fromName, fromEmail));
        message.To.Add(new MailboxAddress(
            $"{envio.NombreRemitente} {envio.ApellidosRemitente}",
            envio.EmailRemitente));
        message.Subject = $"NexoPostal — Confirmación de envío {envio.NumeroSeguimiento}";

        var builder = new BodyBuilder
        {
            HtmlBody = GenerarHtmlEmail(envio)
        };

        // Adjuntar factura PDF
        builder.Attachments.Add(
            $"Factura_{envio.NumeroSeguimiento}.pdf",
            facturaPdf,
            ContentType.Parse("application/pdf"));

        // Adjuntar etiqueta PDF
        builder.Attachments.Add(
            $"Etiqueta_{envio.NumeroSeguimiento}.pdf",
            etiquetaPdf,
            ContentType.Parse("application/pdf"));

        message.Body = builder.ToMessageBody();

        try
        {
            using var client = new SmtpClient();

            var host = smtpSettings["Host"] ?? "localhost";
            var port = int.Parse(smtpSettings["Port"] ?? "587");
            var useSsl = bool.Parse(smtpSettings["UseSsl"] ?? "true");
            var username = smtpSettings["Username"] ?? "";
            var password = smtpSettings["Password"] ?? "";

            var secureOption = useSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None;
            await client.ConnectAsync(host, port, secureOption);

            if (!string.IsNullOrEmpty(username))
            {
                await client.AuthenticateAsync(username, password);
            }

            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            _logger.LogInformation(
                "Email de confirmación enviado a {Email} para envío {NumeroSeguimiento}",
                envio.EmailRemitente, envio.NumeroSeguimiento);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error al enviar email de confirmación a {Email} para envío {NumeroSeguimiento}",
                envio.EmailRemitente, envio.NumeroSeguimiento);
            // No lanzamos excepción para que el flujo de pago no falle por un error de email
        }
    }

    private static string GenerarHtmlEmail(Envio envio)
    {
        var fechaPago = envio.FechaPago?.ToString("dd/MM/yyyy HH:mm") ?? DateTime.UtcNow.ToString("dd/MM/yyyy HH:mm");

        return $$"""
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset="utf-8">
            <style>
                body { font-family: 'Segoe UI', Arial, sans-serif; background: #f5f5f5; margin: 0; padding: 20px; }
                .container { max-width: 600px; margin: 0 auto; background: white; border-radius: 12px; overflow: hidden; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }
                .header { background: #1A237E; color: white; padding: 30px; text-align: center; }
                .header h1 { margin: 0; font-size: 28px; }
                .header .subtitle { color: #FFC107; font-size: 14px; margin-top: 5px; }
                .content { padding: 30px; }
                .success { background: #e8f5e9; border-left: 4px solid #4caf50; padding: 15px; margin-bottom: 20px; border-radius: 4px; }
                .success h2 { color: #2e7d32; margin: 0 0 5px 0; font-size: 18px; }
                .info-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 15px; margin: 20px 0; }
                .info-item { background: #f5f5f5; padding: 12px; border-radius: 8px; }
                .info-item .label { font-size: 11px; color: #666; text-transform: uppercase; letter-spacing: 0.5px; }
                .info-item .value { font-size: 15px; font-weight: 600; color: #1A237E; margin-top: 4px; }
                .tracking { text-align: center; margin: 25px 0; padding: 20px; background: #e8eaf6; border-radius: 8px; }
                .tracking .number { font-size: 22px; font-weight: bold; color: #1A237E; font-family: monospace; letter-spacing: 2px; }
                .footer { background: #263238; color: #90a4ae; padding: 20px; text-align: center; font-size: 12px; }
                .note { font-size: 13px; color: #666; margin-top: 20px; padding: 15px; background: #fff3e0; border-radius: 8px; }
            </style>
        </head>
        <body>
            <div class="container">
                <div class="header">
                    <h1>Nexo<span style="color: #FFC107">Postal</span></h1>
                    <div class="subtitle">Operador Logístico Nacional</div>
                </div>
                <div class="content">
                    <div class="success">
                        <h2>✓ Pago confirmado</h2>
                        <p style="margin:0; color:#333;">Tu envío ha sido registrado correctamente.</p>
                    </div>

                    <div class="tracking">
                        <div style="font-size: 12px; color: #666; margin-bottom: 5px;">NÚMERO DE SEGUIMIENTO</div>
                        <div class="number">{{envio.NumeroSeguimiento}}</div>
                    </div>

                    <div class="info-grid">
                        <div class="info-item">
                            <div class="label">Tipo de envío</div>
                            <div class="value">{{envio.TipoTarifa}}</div>
                        </div>
                        <div class="info-item">
                            <div class="label">Entrega estimada</div>
                            <div class="value">{{envio.TiempoEntregaEstimado}}</div>
                        </div>
                        <div class="info-item">
                            <div class="label">Importe total</div>
                            <div class="value">{{envio.CosteCalculado:F2}} €</div>
                        </div>
                        <div class="info-item">
                            <div class="label">Fecha de pago</div>
                            <div class="value">{{fechaPago}}</div>
                        </div>
                    </div>

                    <div class="info-item" style="margin-bottom: 10px;">
                        <div class="label">Destinatario</div>
                        <div class="value">{{envio.NombreDestinatario}} {{envio.ApellidosDestinatario}}</div>
                        <div style="font-size: 13px; color: #555; margin-top: 2px;">{{envio.Destino}}</div>
                    </div>

                    <div class="note">
                        📎 Adjuntamos la <strong>factura</strong> y la <strong>etiqueta de envío</strong> en formato PDF.
                        Imprime la etiqueta y pégala en un lugar visible del paquete.
                    </div>
                </div>
                <div class="footer">
                    <p>© 2026 NexoPostal S.A. — Todos los derechos reservados</p>
                    <p>Este email es una notificación automática, por favor no responda a este mensaje.</p>
                </div>
            </div>
        </body>
        </html>
        """;
    }
}
