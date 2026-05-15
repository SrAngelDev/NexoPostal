using System.Net.Http.Json;

namespace Nexopostal.Ciudadano.Services;

/// <summary>
/// Servicio para notificar al microservicio Intranet (logística)
/// cuando un envío ha sido pagado y debe entrar en la red logística.
/// 
/// Flujo: Ciudadano pago exitoso → HTTP POST → Intranet /api/admision/interno/paquete
///        → Intranet resuelve CTA por CP → SignalR notifica a OperarioLogisticos
/// </summary>
public interface ILogisticaNotifierService
{
    /// <summary>
    /// Notifica al microservicio de logística que un envío ha sido pagado
    /// y debe ser admitido en la red de CTAs.
    /// </summary>
    /// <param name="numeroExpedicion">Número de expedición interno (NXI-...)</param>
    /// <param name="codigoPostalDestino">CP destino para resolver el CTA</param>
    /// <param name="codigoPostalOrigen">CP origen para determinar si necesita movimiento troncal</param>
    /// <param name="remitente">Nombre del remitente</param>
    /// <param name="destinatario">Nombre del destinatario</param>
    /// <param name="esUrgente">Si el envío es urgente (tarifa Express)</param>
    /// <param name="numeroSeguimiento">Número de seguimiento externo (NXP-...)</param>
    /// <param name="direccionEntrega">Dirección completa de entrega de última milla</param>
    /// <param name="ciudadDestino">Ciudad destino de la entrega</param>
    /// <param name="telefonoDestinatario">Teléfono de contacto del destinatario</param>
    Task NotificarAdmisionAsync(
        string numeroExpedicion,
        string codigoPostalDestino,
        string? codigoPostalOrigen = null,
        string? remitente = null,
        string? destinatario = null,
        bool esUrgente = false,
        string? numeroSeguimiento = null,
        string? direccionEntrega = null,
        string? ciudadDestino = null,
        string? telefonoDestinatario = null);
}

public class LogisticaNotifierService : ILogisticaNotifierService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<LogisticaNotifierService> _logger;

    public LogisticaNotifierService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<LogisticaNotifierService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task NotificarAdmisionAsync(
        string numeroExpedicion,
        string codigoPostalDestino,
        string? codigoPostalOrigen = null,
        string? remitente = null,
        string? destinatario = null,
        bool esUrgente = false,
        string? numeroSeguimiento = null,
        string? direccionEntrega = null,
        string? ciudadDestino = null,
        string? telefonoDestinatario = null)
    {
        try
        {
            var serviceKey = _configuration["IntranetSettings:ServiceKey"]
                ?? "nexopostal-internal-service-key-2025";

            var payload = new
            {
                numeroExpedicion,
                numeroSeguimiento,
                codigoPostalDestino,
                codigoPostalOrigen,
                esUrgente,
                remitente,
                destinatario,
                nombreDestinatario = destinatario,
                direccionEntrega,
                ciudadDestino,
                telefonoDestinatario,
                observaciones = $"Admitido automáticamente tras pago confirmado"
            };

            var request = new HttpRequestMessage(HttpMethod.Post, "/api/admision/interno/paquete")
            {
                Content = JsonContent.Create(payload)
            };
            request.Headers.Add("X-Service-Key", serviceKey);

            var response = await _httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "✅ Notificación logística enviada · {Expedicion} → CP {CpDestino} (urgente: {Urgente})",
                    numeroExpedicion, codigoPostalDestino, esUrgente);
            }
            else
            {
                var body = await response.Content.ReadAsStringAsync();
                _logger.LogWarning(
                    "⚠️ Intranet respondió {StatusCode} al admitir {Expedicion}: {Body}",
                    (int)response.StatusCode, numeroExpedicion, body);
            }
        }
        catch (Exception ex)
        {
            // No lanzamos excepción: el pago ya se procesó correctamente.
            // La notificación logística es best-effort; se puede reintentar luego.
            _logger.LogError(ex,
                "❌ Error al notificar a Intranet sobre el envío {Expedicion}. " +
                "El pago se procesó correctamente pero el CTA no fue notificado.",
                numeroExpedicion);
        }
    }
}
