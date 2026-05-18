using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexopostal.Intranet.DTOs;
using Nexopostal.Intranet.Services;

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
[Authorize(Roles = "Admin,OperarioCTA")]
public class AdmisionController : ControllerBase
{
    private readonly IAdmisionService _admisionService;

    private readonly IConfiguration _configuration;

    public AdmisionController(IAdmisionService admisionService, IConfiguration configuration)
    {
        _admisionService = admisionService;
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
}
