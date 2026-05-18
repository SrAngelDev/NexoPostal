using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexopostal.Intranet.DTOs;
using Nexopostal.Intranet.Services;

namespace Nexopostal.Intranet.Controllers;

/// <summary>
/// Controlador para la gestión de Centros de Tratamiento Automatizado (CTAs).
/// Permite consultar la red logística, resolver enrutamiento por código postal
/// y obtener estadísticas del dashboard.
/// 
/// Accesible por: Admin, OperarioJefe, OperarioLogistico, Operario
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,Supervisor,OperarioCTA,OperarioOficina")]
public class CtasController : ControllerBase
{
    private readonly IClasificacionService _clasificacionService;

    public CtasController(IClasificacionService clasificacionService)
    {
        _clasificacionService = clasificacionService;
    }

    /// <summary>
    /// Obtiene todos los CTAs de la red logística.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<CtaResumenDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<CtaResumenDto>>> ObtenerTodos()
    {
        var ctas = await _clasificacionService.ObtenerTodosCtas();
        return Ok(ctas);
    }

    /// <summary>
    /// Obtiene el detalle completo de un CTA con operarios y rutas asignadas.
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(CtaDetalleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CtaDetalleDto>> ObtenerDetalle(int id)
    {
        var cta = await _clasificacionService.ObtenerCtaDetalle(id);
        if (cta == null) return NotFound(new { message = "CTA no encontrado" });
        return Ok(cta);
    }

    /// <summary>
    /// Resuelve el CTA de destino para un código postal dado.
    /// Usa los 2 primeros dígitos del CP para determinar la provincia y el CTA.
    /// Ejemplo: "28001" → CTA-MAD (Madrid - Barajas)
    /// </summary>
    [HttpGet("resolver/{codigoPostal}")]
    [ProducesResponseType(typeof(ResolverCtaResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ResolverCtaResponseDto>> ResolverCta(string codigoPostal)
    {
        var resultado = await _clasificacionService.ResolverCtaDestino(codigoPostal);
        if (resultado == null) return NotFound(new { message = $"No se encontró CTA para el código postal: {codigoPostal}" });
        return Ok(resultado);
    }

    /// <summary>
    /// Obtiene el dashboard de estadísticas de un CTA.
    /// Incluye: tareas pendientes, en progreso, urgentes, movimientos, incidencias.
    /// </summary>
    [HttpGet("{id:int}/dashboard")]
    [Authorize(Roles = "Admin,Supervisor,OperarioCTA")]
    [ProducesResponseType(typeof(DashboardCtaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DashboardCtaDto>> ObtenerDashboard(int id)
    {
        var dashboard = await _clasificacionService.ObtenerDashboardCta(id);
        if (dashboard == null) return NotFound(new { message = "CTA no encontrado" });
        return Ok(dashboard);
    }

    /// <summary>
    /// Obtiene el dashboard global de administración con estadísticas agregadas de toda la red.
    /// Solo accesible por administradores.
    /// </summary>
    [HttpGet("dashboard-global")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(DashboardAdminDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<DashboardAdminDto>> ObtenerDashboardAdmin()
    {
        var dashboard = await _clasificacionService.ObtenerDashboardAdmin();
        return Ok(dashboard);
    }
}
