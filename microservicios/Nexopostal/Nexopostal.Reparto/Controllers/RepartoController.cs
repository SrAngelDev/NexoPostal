using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexopostal.Reparto.DTOs;
using Nexopostal.Reparto.Services;
using System.Security.Cryptography;
using System.Text;

namespace Nexopostal.Reparto.Controllers;

/// <summary>
/// Controlador para la gestión de rutas de reparto.
/// Usado por la intranet y la app de conductores.
/// </summary>
[ApiController]
[Route("api/reparto")]
[Authorize]
public class RepartoController : ControllerBase
{
    private readonly IRepartoService _repartoService;
    private readonly ICiudadanoTrackingNotifierService _ciudadanoTrackingNotifier;
    private readonly IConfiguration _configuration;
    private readonly ILogger<RepartoController> _logger;

    public RepartoController(
        IRepartoService repartoService,
        ICiudadanoTrackingNotifierService ciudadanoTrackingNotifier,
        IConfiguration configuration,
        ILogger<RepartoController> logger)
    {
        _repartoService = repartoService;
        _ciudadanoTrackingNotifier = ciudadanoTrackingNotifier;
        _configuration = configuration;
        _logger = logger;
    }

    // ═══════════════════════════════════════════
    //  REPARTIDORES
    // ═══════════════════════════════════════════

    /// <summary>
    /// Obtiene la lista de repartidores, opcionalmente filtrada por oficina.
    /// Solo JefeReparto y Admin pueden ver la nómina completa.
    /// </summary>
    [HttpGet("repartidores")]
    [Authorize(Roles = "Admin,JefeReparto")]
    public async Task<IActionResult> ObtenerRepartidores([FromQuery] int? oficinaJsonId)
    {
        var repartidores = await _repartoService.ObtenerRepartidores(oficinaJsonId);
        return Ok(repartidores);
    }

    /// <summary>
    /// Obtiene el perfil de repartidor del usuario autenticado (para driver-app).
    /// </summary>
    [HttpGet("mi-perfil")]
    [Authorize(Roles = "Repartidor,JefeReparto")]
    public async Task<IActionResult> ObtenerMiPerfil()
    {
        var userId = User.FindFirst("sub")?.Value ?? User.FindFirst("nameid")?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized(new { message = "No se pudo identificar al usuario" });

        var repartidor = await _repartoService.ObtenerRepartidorPorIdentityId(userId);
        if (repartidor == null)
            return NotFound(new { message = "No existe perfil de repartidor para este usuario" });

        return Ok(repartidor);
    }

    /// <summary>
    /// Crea un nuevo repartidor (solo JefeReparto y Admin).
    /// </summary>
    [HttpPost("repartidores")]
    [Authorize(Roles = "Admin,JefeReparto")]
    public async Task<IActionResult> CrearRepartidor([FromBody] CrearRepartidorDto dto)
    {
        try
        {
            var repartidor = await _repartoService.CrearRepartidor(dto);
            return CreatedAtAction(nameof(ObtenerRepartidores), null, repartidor);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear repartidor");
            return BadRequest(new { message = ex.Message });
        }
    }

    // ═══════════════════════════════════════════
    //  RUTAS DE REPARTO
    // ═══════════════════════════════════════════

    /// <summary>
    /// Obtiene las rutas de reparto. JefeReparto ve todas las rutas de su equipo.
    /// </summary>
    [HttpGet("rutas")]
    [Authorize(Roles = "Admin,JefeReparto")]
    public async Task<IActionResult> ObtenerRutas(
        [FromQuery] string? fecha,
        [FromQuery] int? repartidorId)
    {
        DateOnly? fechaParsed = null;
        if (!string.IsNullOrEmpty(fecha) && DateOnly.TryParse(fecha, out var f))
            fechaParsed = f;

        var rutas = await _repartoService.ObtenerRutas(fechaParsed, repartidorId);
        return Ok(rutas);
    }

    /// <summary>
    /// Obtiene la ruta activa del repartidor autenticado (para driver-app).
    /// </summary>
    [HttpGet("ruta")]
    [Authorize(Roles = "Repartidor,JefeReparto")]
    public async Task<IActionResult> ObtenerMiRuta()
    {
        var userId = User.FindFirst("sub")?.Value ?? User.FindFirst("nameid")?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var repartidor = await _repartoService.ObtenerRepartidorPorIdentityId(userId);
        if (repartidor == null)
            return NotFound(new { message = "No existe perfil de repartidor" });

        var hoy = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd");
        var rutas = await _repartoService.ObtenerRutas(DateOnly.FromDateTime(DateTime.UtcNow), repartidor.Id);

        return Ok(rutas);
    }

    /// <summary>
    /// Obtiene el detalle de una ruta por ID.
    /// </summary>
    [HttpGet("rutas/{id:int}")]
    [Authorize(Roles = "Admin,JefeReparto,Repartidor")]
    public async Task<IActionResult> ObtenerRutaPorId(int id)
    {
        var ruta = await _repartoService.ObtenerRutaPorId(id);
        if (ruta == null)
            return NotFound(new { message = "Ruta no encontrada" });
        return Ok(ruta);
    }

    /// <summary>
    /// Obtiene el detalle de una ruta por código.
    /// </summary>
    [HttpGet("rutas/codigo/{codigo}")]
    [Authorize(Roles = "Admin,JefeReparto,Repartidor")]
    public async Task<IActionResult> ObtenerRutaPorCodigo(string codigo)
    {
        var ruta = await _repartoService.ObtenerRutaPorCodigo(codigo);
        if (ruta == null)
            return NotFound(new { message = "Ruta no encontrada" });
        return Ok(ruta);
    }

    /// <summary>
    /// Crea una nueva ruta de reparto. Solo JefeReparto y Admin pueden planificar rutas.
    /// </summary>
    [HttpPost("rutas")]
    [Authorize(Roles = "Admin,JefeReparto")]
    public async Task<IActionResult> CrearRuta([FromBody] CrearRutaRepartoDto dto)
    {
        try
        {
            var ruta = await _repartoService.CrearRuta(dto);
            return CreatedAtAction(nameof(ObtenerRutaPorId), new { id = ruta.Id }, ruta);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear ruta de reparto");
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Inicia una ruta de reparto (el repartidor sale de la oficina).
    /// </summary>
    [HttpPost("rutas/{id:int}/iniciar")]
    [Authorize(Roles = "Repartidor,JefeReparto")]
    public async Task<IActionResult> IniciarRuta(int id)
    {
        var ruta = await _repartoService.IniciarRuta(id);
        if (ruta == null)
            return BadRequest(new { message = "No se pudo iniciar la ruta. Verifique que existe y está en estado Planificada." });
        return Ok(ruta);
    }

    /// <summary>
    /// Finaliza una ruta de reparto (el repartidor regresa a la oficina).
    /// </summary>
    [HttpPost("rutas/{id:int}/finalizar")]
    [Authorize(Roles = "Repartidor,JefeReparto")]
    public async Task<IActionResult> FinalizarRuta(int id, [FromBody] FinalizarRutaRequest? request = null)
    {
        var ruta = await _repartoService.FinalizarRuta(id, request?.Observaciones);
        if (ruta == null)
            return BadRequest(new { message = "No se pudo finalizar la ruta. Verifique que existe y está en curso." });
        return Ok(ruta);
    }

    // ═══════════════════════════════════════════
    //  ENTREGAS
    // ═══════════════════════════════════════════

    /// <summary>
    /// Obtiene las entregas de una ruta.
    /// </summary>
    [HttpGet("entregas")]
    [Authorize(Roles = "Admin,JefeReparto,Repartidor")]
    public async Task<IActionResult> ObtenerEntregas([FromQuery] int? rutaId, [FromQuery] string? seguimiento)
    {
        if (rutaId.HasValue)
        {
            var entregas = await _repartoService.ObtenerEntregasPorRuta(rutaId.Value);
            return Ok(entregas);
        }

        if (!string.IsNullOrEmpty(seguimiento))
        {
            var entregas = await _repartoService.ObtenerEntregasPorSeguimiento(seguimiento);
            return Ok(entregas);
        }

        return BadRequest(new { message = "Debe especificar rutaId o seguimiento" });
    }

    /// <summary>
    /// Agrega un paquete a una ruta de reparto. Solo JefeReparto y Admin.
    /// </summary>
    [HttpPost("rutas/{rutaId:int}/entregas")]
    [Authorize(Roles = "Admin,JefeReparto")]
    public async Task<IActionResult> AgregarEntrega(int rutaId, [FromBody] AgregarEntregaDto dto)
    {
        var entrega = await _repartoService.AgregarEntregaARuta(rutaId, dto);
        if (entrega == null)
            return BadRequest(new { message = "No se pudo agregar la entrega. Verifique que la ruta existe y está planificada." });
        return CreatedAtAction(nameof(ObtenerEntregas), new { rutaId }, entrega);
    }

    /// <summary>
    /// Registra el resultado de un intento de entrega (confirmar/ausente/rechazado...).
    /// Endpoint principal para el repartidor desde la driver-app.
    /// </summary>
    [HttpPost("confirmar")]
    [Authorize(Roles = "Repartidor,JefeReparto")]
    public async Task<IActionResult> ConfirmarEntrega([FromQuery] int entregaId, [FromBody] RegistrarEntregaDto dto)
    {
        var entrega = await _repartoService.RegistrarEntrega(entregaId, dto);
        if (entrega == null)
            return BadRequest(new { message = "No se pudo registrar la entrega. Verifique el ID y el estado." });

        await NotificarEventoEntregaTracking(entrega);
        return Ok(entrega);
    }

    /// <summary>
    /// Registra el resultado de entrega por ID en ruta.
    /// </summary>
    [HttpPut("entregas/{entregaId:int}/registrar")]
    [Authorize(Roles = "Repartidor,JefeReparto")]
    public async Task<IActionResult> RegistrarEntrega(int entregaId, [FromBody] RegistrarEntregaDto dto)
    {
        var entrega = await _repartoService.RegistrarEntrega(entregaId, dto);
        if (entrega == null)
            return BadRequest(new { message = "No se pudo registrar la entrega." });

        await NotificarEventoEntregaTracking(entrega);
        return Ok(entrega);
    }

    // ═══════════════════════════════════════════
    //  DASHBOARD
    // ═══════════════════════════════════════════

    /// <summary>
    /// Dashboard de reparto del día actual. Solo JefeReparto y Admin.
    /// </summary>
    [HttpGet("dashboard")]
    [Authorize(Roles = "Admin,JefeReparto")]
    public async Task<IActionResult> ObtenerDashboard([FromQuery] int? oficinaJsonId)
    {
        var dashboard = await _repartoService.ObtenerDashboard(oficinaJsonId);
        return Ok(dashboard);
    }

    /// <summary>
    /// Registra la ubicación en tiempo real del repartidor (para tracking).
    /// </summary>
    [HttpPost("ubicacion")]
    [Authorize(Roles = "Repartidor,JefeReparto")]
    public async Task<IActionResult> RegistrarUbicacion([FromBody] UbicacionRepartidorRequest request)
    {
        _logger.LogInformation("Ubicación recibida de repartidor: lat={Lat}, lng={Lng}",
            request.Latitud, request.Longitud);

        var seguimientos = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (!string.IsNullOrWhiteSpace(request.NumeroSeguimiento))
        {
            seguimientos.Add(request.NumeroSeguimiento.Trim().ToUpperInvariant());
        }

        if (request.RutaId.HasValue)
        {
            var entregas = await _repartoService.ObtenerEntregasPorRuta(request.RutaId.Value);
            foreach (var entrega in entregas)
            {
                if (!string.IsNullOrWhiteSpace(entrega.NumeroSeguimiento))
                {
                    seguimientos.Add(entrega.NumeroSeguimiento.Trim().ToUpperInvariant());
                }
            }
        }

        foreach (var numeroSeguimiento in seguimientos)
        {
            await _ciudadanoTrackingNotifier.NotificarUbicacionAsync(
                numeroSeguimiento,
                request.Latitud,
                request.Longitud,
                request.TipoUbicacion,
                request.Descripcion,
                HttpContext.RequestAborted);
        }

        return Ok(new
        {
            message = "Ubicación registrada",
            trackingNotificados = seguimientos.Count
        });
    }

    /// <summary>
    /// Endpoint interno para auto-generar/asignar entrega de última milla
    /// a partir de una admisión logística.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("interno/admision/auto-asignar")]
    [ProducesResponseType(typeof(AutoAsignacionEntregaResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> AutoAsignarEntregaDesdeAdmision([FromBody] AutoAsignacionEntregaDesdeAdmisionDto dto)
    {
        if (!IsInternalServiceAuthorized())
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Service key inválida" });

        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var resultado = await _repartoService.AutoAsignarEntregaDesdeAdmision(dto);
        return Ok(resultado);
    }

    private bool IsInternalServiceAuthorized()
    {
        var expectedKey = _configuration["InterServiceSettings:ServiceKey"]
            ?? "nexopostal-internal-service-key-2025";
        var providedKey = Request.Headers["X-Service-Key"].FirstOrDefault();

        if (string.IsNullOrWhiteSpace(providedKey))
            return false;

        return SecureEquals(expectedKey, providedKey);
    }

    private static bool SecureEquals(string expected, string provided)
    {
        var expectedBytes = Encoding.UTF8.GetBytes(expected ?? string.Empty);
        var providedBytes = Encoding.UTF8.GetBytes(provided ?? string.Empty);

        if (expectedBytes.Length != providedBytes.Length)
            return false;

        return CryptographicOperations.FixedTimeEquals(expectedBytes, providedBytes);
    }

    private async Task NotificarEventoEntregaTracking(EntregaPaqueteDto entrega)
    {
        if (string.IsNullOrWhiteSpace(entrega.NumeroSeguimiento))
        {
            return;
        }

        var payload = new TrackingEventoEntregaPayload(
            entrega.NumeroSeguimiento.Trim().ToUpperInvariant(),
            entrega.NumeroExpedicion,
            entrega.Estado,
            entrega.NumeroIntento,
            entrega.Observaciones,
            entrega.ReceptorNombre,
            entrega.ReceptorDni,
            entrega.LatitudEntrega,
            entrega.LongitudEntrega,
            entrega.FirmaDigital,
            entrega.FotoEntrega);

        await _ciudadanoTrackingNotifier.NotificarEventoEntregaAsync(payload, HttpContext.RequestAborted);
    }
}

// ─── Request models auxiliares ───

public class FinalizarRutaRequest
{
    public string? Observaciones { get; set; }
}

public class UbicacionRepartidorRequest
{
    public double Latitud { get; set; }
    public double Longitud { get; set; }
    public int? RutaId { get; set; }
    public string? NumeroSeguimiento { get; set; }
    public string TipoUbicacion { get; set; } = "RepartidorEnRuta";
    public string? Descripcion { get; set; }
}
