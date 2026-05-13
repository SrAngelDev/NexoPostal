using Stripe;
using Stripe.Checkout;
using Nexopostal.Ciudadano.Models;
using System.Text.RegularExpressions;

namespace Nexopostal.Ciudadano.Services;

/// <summary>
/// Servicio para integración con Stripe Checkout (modo test)
/// </summary>
public interface IStripeService
{
    /// <summary>
    /// Crea una sesión de Stripe Checkout para pagar un envío
    /// </summary>
    Task<(string SessionUrl, string SessionId)> CrearSesionCheckout(
        Envio envio, string successUrl, string cancelUrl);

    /// <summary>
    /// Verifica el estado de pago de una sesión de Stripe
    /// </summary>
    Task<bool> VerificarPagoSesion(string sessionId);
}

public class StripeService : IStripeService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<StripeService> _logger;

    public StripeService(IConfiguration configuration, ILogger<StripeService> logger)
    {
        _configuration = configuration;
        _logger = logger;

        // Configurar la API key de Stripe
        var secretKey = ResolveConfigValue(_configuration["Stripe:SecretKey"]);
        if (string.IsNullOrWhiteSpace(secretKey) || secretKey.Contains("${", StringComparison.Ordinal))
        {
            _logger.LogError("Stripe SecretKey no configurada correctamente.");
            throw new InvalidOperationException("Stripe SecretKey no configurada correctamente.");
        }

        StripeConfiguration.ApiKey = secretKey;
    }

    private static string ResolveConfigValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        return Regex.Replace(value, @"\$\{([^}]+)\}", match =>
            Environment.GetEnvironmentVariable(match.Groups[1].Value) ?? match.Value);
    }

    public async Task<(string SessionUrl, string SessionId)> CrearSesionCheckout(
        Envio envio, string successUrl, string cancelUrl)
    {
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
                                          $"{envio.PesoKg}kg — {envio.Dimensiones}cm — " +
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
