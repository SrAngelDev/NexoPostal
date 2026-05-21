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
    private readonly IAdminCtaService _adminCtaService;

    public CtasController(
        IClasificacionService clasificacionService,
        IAdminCtaService adminCtaService)
    {
        _clasificacionService = clasificacionService;
        _adminCtaService = adminCtaService;
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

    // ============================================================
    //  Endpoints administrativos (Admin) — CRUD de CTAs
    // ============================================================

    /// <summary>
    /// Crea un nuevo Centro de Tratamiento Automatizado. Solo Admin.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(CtaDetalleDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CtaDetalleDto>> Crear([FromBody] CrearCtaDto dto)
    {
        var (cta, error) = await _adminCtaService.CrearCta(dto);
        if (cta == null) return BadRequest(new { message = error ?? "No se pudo crear el CTA." });
        return CreatedAtAction(nameof(ObtenerDetalle), new { id = cta.Id }, cta);
    }

    /// <summary>
    /// Edita los datos de un CTA existente (excepto el código). Solo Admin.
    /// </summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(CtaDetalleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CtaDetalleDto>> Editar(int id, [FromBody] EditarCtaDto dto)
    {
        var (cta, error) = await _adminCtaService.EditarCta(id, dto);
        if (cta == null)
        {
            if (error == "CTA no encontrado.") return NotFound(new { message = error });
            return BadRequest(new { message = error });
        }
        return Ok(cta);
    }

    /// <summary>
    /// Desactiva un CTA (soft delete). Falla si hay operarios, tareas o movimientos activos.
    /// </summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Desactivar(int id)
    {
        var (ok, error) = await _adminCtaService.DesactivarCta(id);
        if (!ok) return BadRequest(new { message = error });
        return NoContent();
    }

    /// <summary>
    /// Reactiva un CTA previamente desactivado.
    /// </summary>
    [HttpPost("{id:int}/reactivar")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Reactivar(int id)
    {
        var (ok, error) = await _adminCtaService.ReactivarCta(id);
        if (!ok) return BadRequest(new { message = error });
        return NoContent();
    }
}
