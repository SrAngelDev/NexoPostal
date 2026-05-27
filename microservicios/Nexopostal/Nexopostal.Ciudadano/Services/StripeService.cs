using Stripe;
using Stripe.Checkout;
using Nexopostal.Ciudadano.Models;
using System.Text.RegularExpressions;

namespace Nexopostal.Ciudadano.Services;

/// <summary>
/// Contrato del flujo de cobro con Stripe Checkout.
/// Aísla al resto del módulo de los detalles concretos del proveedor de pago.
/// </summary>
public interface IStripeService
{
    /// <summary>
    /// Prepara una sesión de pago para un envío y devuelve la URL a la que debe ir el cliente.
    /// </summary>
    Task<(string SessionUrl, string SessionId)> CrearSesionCheckout(
        Envio envio, string successUrl, string cancelUrl);

    /// <summary>
    /// Consulta en Stripe si una sesión concreta ya quedó pagada.
    /// </summary>
    Task<bool> VerificarPagoSesion(string sessionId);
}

/// <summary>
/// Implementación del cobro con Stripe Checkout.
/// Se encarga de construir la sesión de pago y de consultar su estado cuando el frontend vuelve del checkout.
/// </summary>
public class StripeService : IStripeService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<StripeService> _logger;

    public StripeService(IConfiguration configuration, ILogger<StripeService> logger)
    {
        _configuration = configuration;
        _logger = logger;

        // Cargamos la clave una sola vez para fallar pronto si el entorno está mal configurado.
        var secretKey = ResolveConfigValue(_configuration["Stripe:SecretKey"]);
        if (string.IsNullOrWhiteSpace(secretKey) || secretKey.Contains("${", StringComparison.Ordinal))
        {
            _logger.LogError("Stripe SecretKey no configurada correctamente.");
            throw new InvalidOperationException("Stripe SecretKey no configurada correctamente.");
        }

        StripeConfiguration.ApiKey = secretKey;
    }

    /// <summary>
    /// Resuelve secretos definidos como variables de entorno cuando la configuración usa el formato ${NOMBRE}.
    /// </summary>
    private static string ResolveConfigValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        return Regex.Replace(value, @"\$\{([^}]+)\}", match =>
            Environment.GetEnvironmentVariable(match.Groups[1].Value) ?? match.Value);
    }

    /// <summary>
    /// Normaliza el texto de dimensiones para que la descripción enviada a Stripe sea legible y consistente.
    /// </summary>
    private static string FormatearDimensiones(string? dimensiones)
    {
        if (string.IsNullOrWhiteSpace(dimensiones))
        {
            return "Dimensiones no informadas";
        }

        // Eliminamos repeticiones accidentales de "cm" para no generar descripciones raras en el checkout.
        var s = dimensiones.Trim();
        while (s.EndsWith("cm", StringComparison.OrdinalIgnoreCase))
            s = s[..^2].TrimEnd();

        return $"{s} cm";
    }

    /// <summary>
    /// Crea la sesión de Stripe con el importe calculado por el backend y la información esencial del envío.
    /// </summary>
    public async Task<(string SessionUrl, string SessionId)> CrearSesionCheckout(
        Envio envio, string successUrl, string cancelUrl)
    {
        var dimensionesFormateadas = FormatearDimensiones(envio.Dimensiones);

        var options = new SessionCreateOptions
        {
            PaymentMethodTypes = ["card"],
            LineItems =
            [
                new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        UnitAmountDecimal = envio.CosteCalculado * 100, // Stripe usa céntimos
                        Currency = "eur",
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = $"Envío {envio.TipoTarifa} NexoPostal",
                            Description = $"Envío {envio.TipoTarifa} ({envio.TiempoEntregaEstimado}) — " +
                                          $"{envio.PesoKg} kg — {dimensionesFormateadas} — " +
                                          $"Ref: {envio.NumeroSeguimiento}"
                        }
                    },
                    Quantity = 1
                }
            ],
            Mode = "payment",
            SuccessUrl = successUrl,
            CancelUrl = cancelUrl,
            CustomerEmail = envio.EmailRemitente,
            Metadata = new Dictionary<string, string>
            {
                { "NumeroSeguimiento", envio.NumeroSeguimiento },
                { "TipoTarifa", envio.TipoTarifa }
            }
        };

        var service = new SessionService();
        var session = await service.CreateAsync(options);

        _logger.LogInformation(
            "Sesión de Stripe creada: {SessionId} para envío {NumeroSeguimiento}",
            session.Id, envio.NumeroSeguimiento);

        return (session.Url, session.Id);
    }

    /// <summary>
    /// Consulta a Stripe si la sesión ya está pagada y registra cualquier incidencia de comunicación.
    /// </summary>
    public async Task<bool> VerificarPagoSesion(string sessionId)
    {
        try
        {
            var service = new SessionService();
            var session = await service.GetAsync(sessionId);

            _logger.LogInformation(
                "Estado de sesión Stripe {SessionId}: PaymentStatus={PaymentStatus}",
                sessionId, session.PaymentStatus);

            return session.PaymentStatus == "paid";
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Error al verificar sesión de Stripe: {SessionId}", sessionId);
            return false;
        }
    }
}
