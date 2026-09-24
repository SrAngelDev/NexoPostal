using MediatR;
using Nexopostal.Ciudadano.Application.Pagos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexopostal.Ciudadano.DTOs;
using Nexopostal.Ciudadano.Models;
using Nexopostal.Ciudadano.Repositories;
using Nexopostal.Ciudadano.Services;
using System.Security.Claims;

namespace Nexopostal.Ciudadano.Controllers;

/// <summary>
/// Controlador para la gestión de pagos con Stripe Checkout.
/// Flujo: Crear sesión → Stripe Checkout → Verificar pago → Generar documentos → Email
/// </summary>
[Route("api/[controller]")]
[ApiController]
public class PagosController : ControllerBase
{
    private readonly ISender _sender;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PagosController> _logger;

    public PagosController(ISender sender, IConfiguration configuration, ILogger<PagosController> logger)
    {
        _sender = sender;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Crea un envío en estado PendientePago y genera una sesión de Stripe Checkout.
    /// El frontend redirige al usuario a la URL devuelta para completar el pago.
    /// </summary>
    [Authorize]
    [HttpPost("crear-sesion")]
    [ProducesResponseType(typeof(SesionPagoCreadaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CrearSesionPago([FromBody] CrearSesionPagoDto dto)
    {

        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = ObtenerUserId();
        if (string.IsNullOrEmpty(userId))
            return Unauthorized("Token inválido");

        // Validación TipoEntrega + OficinaDestinoId coherente
        if (!Enum.TryParse<TipoEntrega>(dto.TipoEntrega, ignoreCase: true, out var tipoEntrega))
            return BadRequest(new { mensaje = $"TipoEntrega no válido: {dto.TipoEntrega}." });

        // El remitente SIEMPRE entrega el paquete en una oficina postal (no realizamos
        // recogidas a domicilio). La OficinaOrigenId es por tanto obligatoria para todo
        // alta online.
        if (dto.OficinaOrigenId is null or <= 0)
            return BadRequest(new { mensaje = "OficinaOrigenId es obligatorio: el remitente debe entregar el paquete en una oficina postal." });

        if (tipoEntrega == TipoEntrega.Oficina && (dto.OficinaDestinoId is null or <= 0))
            return BadRequest(new { mensaje = "OficinaDestinoId es obligatorio cuando TipoEntrega == Oficina." });

        if (tipoEntrega == TipoEntrega.Domicilio && dto.OficinaDestinoId is not null)
            return BadRequest(new { mensaje = "OficinaDestinoId debe ser null cuando TipoEntrega == Domicilio." });

        return Ok(await _sender.Send(new CrearSesionPagoCommand(dto, userId, tipoEntrega), HttpContext?.RequestAborted ?? CancellationToken.None));
    }

    /// <summary>
    /// Verifica el estado de pago de una sesión de Stripe.
    /// Si el pago fue exitoso, marca el envío como pagado, genera PDFs y envía email.
    /// </summary>
    [Authorize]
    [HttpGet("verificar/{sessionId}")]
    [ProducesResponseType(typeof(VerificarPagoResultadoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> VerificarPago(string sessionId)
    {

        var userId = ObtenerUserId();
        if (string.IsNullOrEmpty(userId))
            return Unauthorized("Token inválido");

        var resultado = await _sender.Send(new VerificarPagoCommand(sessionId, userId), HttpContext?.RequestAborted ?? CancellationToken.None);
        return resultado is null ? NotFound(new { mensaje = "Sesión de pago no encontrada" }) : Ok(resultado);
    }

    /// <summary>
    /// Reintenta el pago de un envío en estado PendientePago.
    /// Crea una nueva sesión de Stripe Checkout para el mismo envío.
    /// </summary>
    [Authorize]
    [HttpPost("reintentar/{numero}")]
    [ProducesResponseType(typeof(SesionPagoCreadaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ReintentarPago(string numero, [FromBody] ReintentarPagoDto dto)
    {

        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = ObtenerUserId();
        if (string.IsNullOrEmpty(userId))
            return Unauthorized("Token inválido");

        var resultado = await _sender.Send(new ReintentarPagoCommand(numero, userId, dto), HttpContext?.RequestAborted ?? CancellationToken.None);
        return resultado.Error switch
        {
            ErrorReintento.EnvioNoEncontrado => NotFound(new { mensaje = "Envío no encontrado" }),
            ErrorReintento.EstadoNoPendiente => BadRequest(new { mensaje = "Este envío ya ha sido pagado o no está en estado pendiente" }),
            _ => Ok(resultado.Sesion)
        };
    }

    /// <summary>
    /// Webhook de Stripe para recibir notificaciones de pago.
    /// No requiere autenticación JWT (es llamado por Stripe directamente).
    /// </summary>
    [HttpPost("webhook")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> StripeWebhook()
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();

        try
        {
            var webhookSecret = _configuration["Stripe:WebhookSecret"]
                ?? Environment.GetEnvironmentVariable("STRIPE_WEBHOOK_SECRET");

            Stripe.Event stripeEvent;
            if (!string.IsNullOrWhiteSpace(webhookSecret)
                && webhookSecret != "whsec_TU_WEBHOOK_SECRET")
            {
                // PRODUCCIÓN: verificar que el evento viene realmente de Stripe
                var stripeSignature = Request.Headers["Stripe-Signature"];
                stripeEvent = Stripe.EventUtility.ConstructEvent(
                    json, stripeSignature, webhookSecret);
            }
            else
            {
                // DESARROLLO: sin webhook secret configurado, parsear sin verificar
                _logger.LogWarning("Webhook de Stripe sin verificación de firma (modo test/dev)");
                stripeEvent = Stripe.EventUtility.ParseEvent(json);
            }

            if (stripeEvent.Type == Stripe.EventTypes.CheckoutSessionCompleted)
            {
                var session = stripeEvent.Data.Object as Stripe.Checkout.Session;
                if (session != null)
                {
                    await _sender.Send(new ConfirmarPagoWebhookCommand(session.Id), HttpContext?.RequestAborted ?? CancellationToken.None);
                }
            }

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error procesando webhook de Stripe");
            return Ok(); // Siempre devolvemos 200 a Stripe para evitar reintentos
        }
    }

    // ===== MÉTODOS AUXILIARES =====

    /// <summary>
    /// Obtiene el usuario autenticado para limitar cada operación a sus envíos.
    /// </summary>
    private string? ObtenerUserId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value
               ?? User.FindFirst("sub")?.Value
               ?? User.FindFirst("uid")?.Value;
    }
}
