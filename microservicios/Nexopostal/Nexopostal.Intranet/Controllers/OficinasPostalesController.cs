using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexopostal.Intranet.DTOs;
using Nexopostal.Intranet.Services;

namespace Nexopostal.Intranet.Controllers;

/// <summary>
/// Controlador para la consulta y resolución de oficinas postales.
/// 
/// Las oficinas se cargan desde el JSON estático de oficinas reales (Data/oficinas.json).
/// Este controlador permite:
///   - Buscar oficinas por CP o texto libre
///   - Resolver oficina + CTA para el flujo logístico automático
///   - Consultar operarios asignados a una oficina
/// 
/// Flujo automático:
///   CP 28919 → Oficina "NexoPostal Leganés" → CTA-MAD
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OficinasPostalesController : ControllerBase
{
    private readonly IOficinaPostalService _oficinaService;

    public OficinasPostalesController(IOficinaPostalService oficinaService)
    {
        _oficinaService = oficinaService;
    }

    /// <summary>
    /// Obtiene todas las oficinas del JSON estático.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<OficinaJsonDto>), 200)]
    public IActionResult ObtenerTodas()
    {
        var oficinas = _oficinaService.ObtenerTodas();
        return Ok(oficinas);
    }

    /// <summary>
    /// Busca oficinas por código postal o texto libre.
    /// </summary>
    /// <param name="codigoPostal">Código postal (exacto o parcial)</param>
    /// <param name="query">Texto libre (nombre, dirección, ciudad)</param>
    [HttpGet("buscar")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<OficinaJsonDto>), 200)]
    public IActionResult Buscar([FromQuery] string? codigoPostal, [FromQuery] string? query)
    {
        List<OficinaJsonDto> resultados;

        if (!string.IsNullOrWhiteSpace(codigoPostal))
            resultados = _oficinaService.BuscarPorCodigoPostal(codigoPostal);
        else if (!string.IsNullOrWhiteSpace(query))
            resultados = _oficinaService.BuscarPorTexto(query);
        else
            return BadRequest(new { mensaje = "Debe indicar 'codigoPostal' o 'query'." });

        return Ok(resultados);
    }

    /// <summary>
    /// Obtiene una oficina por su ID del JSON.
    /// </summary>
    /// <param name="id">ID de la oficina en el JSON</param>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(OficinaJsonDto), 200)]
    [ProducesResponseType(404)]
    public IActionResult ObtenerPorId(int id)
    {
        var oficina = _oficinaService.ObtenerPorId(id);
        if (oficina == null)
            return NotFound(new { mensaje = $"No se encontró la oficina con ID {id}." });
        return Ok(oficina);
    }

    /// <summary>
    /// Resuelve la oficina más cercana y el CTA asociado para un código postal.
    /// Endpoint clave del flujo logístico automático.
    /// 
    /// Ejemplo: GET /api/oficinaspostales/resolver/28919
    ///   → { OficinaId: 1042, OficinaNombre: "NexoPostal Leganés", CtaCodigo: "CTA-MAD", ... }
    /// </summary>
    /// <param name="codigoPostal">Código postal del origen o destino del envío</param>
    [HttpGet("resolver/{codigoPostal}")]
    [ProducesResponseType(typeof(ResolverOficinaCtaResponseDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> ResolverOficinaPorCp(string codigoPostal)
    {
        var resultado = await _oficinaService.ResolverOficinaPorCp(codigoPostal);
        if (resultado == null)
            return NotFound(new { mensaje = $"No se encontró oficina ni CTA para el CP {codigoPostal}." });
        return Ok(resultado);
    }

    /// <summary>
    /// Obtiene los operarios asignados a una oficina.
    /// </summary>
    /// <param name="oficinaJsonId">ID de la oficina en el JSON</param>
    [HttpGet("{oficinaJsonId:int}/operarios")]
    [ProducesResponseType(typeof(List<OperarioOficinaResumenDto>), 200)]
    public async Task<IActionResult> ObtenerOperarios(int oficinaJsonId)
    {
        var operarios = await _oficinaService.ObtenerOperariosOficina(oficinaJsonId);
        return Ok(operarios);
    }
}
