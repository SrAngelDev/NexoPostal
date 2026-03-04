using Microsoft.AspNetCore.Mvc;
using Nexopostal.Ciudadano.DTOs;
using Nexopostal.Ciudadano.Services;

namespace Nexopostal.Ciudadano.Controllers;

/// <summary>
/// Controlador para la búsqueda y consulta de oficinas postales.
/// Sirve los datos desde el archivo JSON estático de oficinas.
/// </summary>
[Route("api/[controller]")]
[ApiController]
public class OficinasController : ControllerBase
{
    private readonly OficinasJsonService _oficinasService;
    private readonly ILogger<OficinasController> _logger;

    public OficinasController(OficinasJsonService oficinasService, ILogger<OficinasController> logger)
    {
        _oficinasService = oficinasService;
        _logger = logger;
    }

    /// <summary>
    /// Busca oficinas por código postal
    /// </summary>
    /// <param name="codigoPostal">Código postal (completo o parcial)</param>
    /// <returns>Lista de oficinas encontradas</returns>
    [HttpGet("buscar")]
    [ProducesResponseType(typeof(List<OficinaDto>), StatusCodes.Status200OK)]
    public IActionResult BuscarOficinas([FromQuery] string? codigoPostal, [FromQuery] string? query)
    {
        try
        {
            List<OficinaDto> resultados;

            if (!string.IsNullOrWhiteSpace(codigoPostal))
            {
                resultados = _oficinasService.BuscarPorCodigoPostal(codigoPostal);
            }
            else if (!string.IsNullOrWhiteSpace(query))
            {
                resultados = _oficinasService.BuscarPorTexto(query);
            }
            else
            {
                return BadRequest(new { error = "Debe indicar 'codigoPostal' o 'query'" });
            }

            _logger.LogInformation(
                "Búsqueda de oficinas (CP={CP}, query={Query}): {Count} resultados",
                codigoPostal, query, resultados.Count);

            return Ok(resultados);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al buscar oficinas");
            return StatusCode(500, new { error = "Error al buscar oficinas", details = ex.Message });
        }
    }

    /// <summary>
    /// Obtiene todas las oficinas
    /// </summary>
    /// <returns>Lista completa de oficinas</returns>
    [HttpGet]
    [ProducesResponseType(typeof(List<OficinaDto>), StatusCodes.Status200OK)]
    public IActionResult ObtenerOficinas()
    {
        try
        {
            var oficinas = _oficinasService.ObtenerTodas();
            return Ok(oficinas);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener oficinas");
            return StatusCode(500, new { error = "Error al obtener oficinas", details = ex.Message });
        }
    }
}
