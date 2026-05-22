using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexopostal.Intranet.DTOs;
using Nexopostal.Intranet.Models;
using Nexopostal.Intranet.Repositories;
using Nexopostal.Intranet.Services;
using System.Security.Claims;

namespace Nexopostal.Intranet.Controllers;

/// <summary>
/// Controlador para la admisión de paquetes en la red logística de NexoPostal.
/// 
/// Cuando un paquete entra en el sistema, este controlador:
///   1. Resuelve automáticamente el CTA de destino según el código postal
///      (ej: CP 28919 → prefijo "28" → Madrid → CTA-MAD)
///   2. Si el CP de origen corresponde a un CTA diferente, crea un movimiento troncal
///   3. Notifica en tiempo real vía SignalR a los OperarioLogisticos del CTA destino
///      para que asignen la tarea de clasificación a un Operario
/// 
/// Ejemplo real:
///   - Paquete con destino CP 08001 (Barcelona) admitido en oficina de Madrid
///   - CP origen "28..." → CTA-MAD | CP destino "08..." → CTA-BCN
///   - Se crea movimiento troncal CTA-MAD → CTA-BCN (Terrestre o Aéreo si urgente)
///   - Se notifica a los logísticos de CTA-BCN que tienen un paquete en camino
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,OperarioJefe,OperarioLogistico")]
public class AdmisionController : ControllerBase
{
    private readonly IAdmisionService _admisionService;
    private readonly ICiudadanoEnvioAltaService _envioAltaService;
    private readonly IOperarioOficinaRepository _operarioOficinaRepo;
    private readonly IConfiguration _configuration;

    public AdmisionController(
        IAdmisionService admisionService,
        ICiudadanoEnvioAltaService envioAltaService,
        IOperarioOficinaRepository operarioOficinaRepo,
        IConfiguration configuration)
    {
        _admisionService = admisionService;
        _envioAltaService = envioAltaService;
        _operarioOficinaRepo = operarioOficinaRepo;
        _configuration = configuration;
    }

    /// <summary>
    /// Admite un paquete en la red logística de NexoPostal.
    /// 
    /// El sistema resuelve automáticamente el CTA de destino a partir del código postal
    /// y notifica a los operarios logísticos del CTA correspondiente.
    /// 
    /// Si se proporciona el código postal de origen y corresponde a un CTA diferente,
    /// se crea automáticamente un movimiento troncal con el tipo de transporte óptimo.
    /// </summary>
    /// <param name="dto">Datos del paquete a admitir</param>
    /// <returns>Información del enrutamiento y CTA asignado</returns>
    [HttpPost("paquete")]
    [ProducesResponseType(typeof(AdmisionPaqueteResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AdmisionPaqueteResponseDto>> AdmitirPaquete([FromBody] AdmisionPaqueteDto dto)
    {
        try
        {
            var resultado = await _admisionService.AdmitirPaquete(dto);
            return Ok(resultado);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Endpoint interno para comunicación inter-servicio (Ciudadano → Intranet).
    /// Cuando un cliente paga un envío, el microservicio Ciudadano llama a este endpoint
    /// para que el sistema logístico resuelva el CTA, cree movimientos troncales
    /// y notifique en tiempo real a los OperarioLogisticos.
    /// 
    /// Autenticación: No usa JWT (es llamada entre microservicios).
    /// Usa un header X-Service-Key para validar que la petición viene de un servicio autorizado.
    /// </summary>
    [HttpPost("interno/paquete")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AdmisionPaqueteResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<AdmisionPaqueteResponseDto>> AdmitirPaqueteInterno([FromBody] AdmisionPaqueteDto dto)
    {
        // Validar service key para comunicación inter-servicio
        var expectedKey = _configuration["InterServiceSettings:ServiceKey"] ?? "nexopostal-internal-service-key-2025";
        var providedKey = Request.Headers["X-Service-Key"].FirstOrDefault();

        if (string.IsNullOrEmpty(providedKey) || providedKey != expectedKey)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Service key inválida" });
        }

        try
        {
            var resultado = await _admisionService.AdmitirPaquete(dto);
            return Ok(resultado);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Alta presencial de envío en oficina por parte de un <c>OperarioOficina</c>.
    /// 
    /// Flujo:
    ///   1. Se resuelve el <c>OperarioOficina</c> autenticado y su oficina.
    ///   2. Se invoca a Ciudadano (inter-servicio) para crear el envío con
    ///      <c>Pagado=true</c> y <c>EstadoInterno=RecogidoEnOrigen</c>.
    ///   3. Con la expedición devuelta se llama al pipeline normal de admisión
    ///      con <c>YaRecogidoEnOrigen=true</c>, generando la tarea
    ///      <c>SalidaOficinaACta</c> para el propio operario.
    /// </summary>
    [HttpPost("oficina/alta")]
    [Authorize(Roles = "Admin,OperarioOficina")]
    [ProducesResponseType(typeof(AltaEnvioOficinaResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AltaEnvioOficinaResponseDto>> AltaPresencialOficina(
        [FromBody] AltaEnvioOficinaIntranetDto dto,
        CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        // 1. Resolver OperarioOficina autenticado
        var identityUserId =
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(identityUserId))
            return Unauthorized(new { message = "Token inválido (sin sub)." });

        OperarioOficina? operario = null;

        // Admin puede operar como cualquier oficina si manda header
        var esAdmin = User.IsInRole("Admin");
        if (!esAdmin)
        {
            operario = await _operarioOficinaRepo.GetByIdentityUserIdAsync(identityUserId);
            if (operario is null)
                return StatusCode(StatusCodes.Status409Conflict,
                    new { message = "Tu usuario no está vinculado a ningún operario de oficina." });
            if (!operario.Activo)
                return StatusCode(StatusCodes.Status409Conflict,
                    new { message = "El operario está dado de baja." });
        }

        var oficinaOrigenId = operario?.OficinaJsonId
            ?? (int.TryParse(Request.Headers["X-Oficina-Origen-Id"].FirstOrDefault(), out var ofiHdr) ? ofiHdr : 0);

        if (oficinaOrigenId <= 0)
            return BadRequest(new { message = "No se pudo determinar OficinaOrigenId." });

        // 2. Crear envío en Ciudadano
        var creado = await _envioAltaService.CrearAsync(dto, oficinaOrigenId, ct);
        if (creado is null)
            return StatusCode(StatusCodes.Status502BadGateway,
                new { message = "No se pudo crear el envío en Ciudadano." });

        // 3. Admitir paquete con YaRecogidoEnOrigen=true → genera tarea SalidaOficinaACta
        var admisionDto = new AdmisionPaqueteDto
        {
            NumeroExpedicion = creado.NumeroExpedicion,
            NumeroSeguimiento = creado.NumeroSeguimiento,
            CodigoPostalOrigen = dto.CodigoPostalOrigen,
            CodigoPostalDestino = dto.CodigoPostalDestino,
            DireccionEntrega = dto.Destino,
            NombreDestinatario = dto.NombreDestinatario,
            TelefonoDestinatario = dto.TelefonoDestinatario,
            EsUrgente = false,
            Observaciones = dto.Observaciones,
            OficinaOrigenId = oficinaOrigenId,
            OficinaDestinoId = dto.OficinaDestinoId,
            TipoEntrega = dto.TipoEntrega,
            YaRecogidoEnOrigen = true,
            OperarioOficinaId = operario?.Id
        };

        AdmisionPaqueteResponseDto? admision;
        try
        {
            admision = await _admisionService.AdmitirPaquete(admisionDto);
        }
        catch (ArgumentException ex)
        {
            // El envío sí está creado en Ciudadano pero no se pudo enrutar.
            return Ok(new AltaEnvioOficinaResponseDto
            {
                NumeroExpedicion = creado.NumeroExpedicion,
                NumeroSeguimiento = creado.NumeroSeguimiento,
                CosteCalculado = creado.CosteCalculado,
                TipoEntrega = creado.TipoEntrega,
                OficinaOrigenId = creado.OficinaOrigenId,
                OficinaDestinoId = creado.OficinaDestinoId,
                Mensaje = "Envío creado, pero la admisión logística falló: " + ex.Message
            });
        }

        return Ok(new AltaEnvioOficinaResponseDto
        {
            NumeroExpedicion = creado.NumeroExpedicion,
            NumeroSeguimiento = creado.NumeroSeguimiento,
            CosteCalculado = creado.CosteCalculado,
            TipoEntrega = creado.TipoEntrega,
            OficinaOrigenId = creado.OficinaOrigenId,
            OficinaDestinoId = creado.OficinaDestinoId,
            CtaDestinoCodigo = admision.CtaDestinoCodigo,
            Mensaje = "Envío dado de alta y tarea SalidaOficinaACta asignada al operario."
        });
    }
}
