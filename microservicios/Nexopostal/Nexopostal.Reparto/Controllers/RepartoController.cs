using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexopostal.Reparto.DTOs;
using Nexopostal.Reparto.Services;
using System.Security.Claims;
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
    private readonly IBandejaPendientesService _bandejaService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<RepartoController> _logger;

    public RepartoController(
        IRepartoService repartoService,
        ICiudadanoTrackingNotifierService ciudadanoTrackingNotifier,
        IBandejaPendientesService bandejaService,
        IConfiguration configuration,
        ILogger<RepartoController> logger)
    {
        _repartoService = repartoService;
        _ciudadanoTrackingNotifier = ciudadanoTrackingNotifier;
        _bandejaService = bandejaService;
        _configuration = configuration;
        _logger = logger;
    }

    // ═══════════════════════════════════════════
    //  REPARTIDORES
    // ═══════════════════════════════════════════

    /// <summary>
    /// Obtiene la lista de repartidores, opcionalmente filtrada por oficina.
    /// Si el caller es JefeReparto (no Admin) se fuerza el filtro a su propia oficina.
    /// </summary>
    [HttpGet("repartidores")]
    [Authorize(Roles = "Admin,JefeReparto")]
    public async Task<IActionResult> ObtenerRepartidores([FromQuery] int? oficinaJsonId, [FromQuery] bool incluirInactivos = false)
    {
        var oficinaJefe = await GetOficinaSiJefeRepartoAsync();
        if (oficinaJefe.HasValue)
            oficinaJsonId = oficinaJefe.Value;

        var repartidores = await _repartoService.ObtenerRepartidores(oficinaJsonId, incluirInactivos);
        return Ok(repartidores);
    }

    /// <summary>
    /// Obtiene la ficha de un repartidor por su IdentityUserId (uso administrativo).
    /// </summary>
    [HttpGet("repartidores/identity/{userId}")]
    [Authorize(Roles = "Admin,JefeReparto")]
    public async Task<IActionResult> ObtenerRepartidorPorIdentity(string userId)
    {
        var repartidor = await _repartoService.ObtenerRepartidorPorIdentityId(userId);
        if (repartidor == null)
            return NotFound(new { message = "No existe perfil de repartidor para ese usuario" });
        return Ok(repartidor);
    }

    /// <summary>
    /// Obtiene el perfil de repartidor del usuario autenticado.
    /// Lo usan tanto driver-app (Repartidor) como el panel del JefeReparto
    /// para descubrir su propia OficinaJsonId.
    /// </summary>
    [HttpGet("mi-perfil")]
    [Authorize(Roles = "Repartidor,JefeReparto")]
    public async Task<IActionResult> ObtenerMiPerfil()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? User.FindFirst("sub")?.Value
                     ?? User.FindFirst("nameid")?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized(new { message = "No se pudo identificar al usuario" });

        var repartidor = await _repartoService.ObtenerRepartidorPorIdentityId(userId);
        if (repartidor == null)
            return NotFound(new { message = "No existe perfil de repartidor para este usuario" });

        return Ok(repartidor);
    }

    /// <summary>
    /// Crea un nuevo repartidor (solo JefeReparto y Admin).
    /// Si el caller es JefeReparto, se fuerza la oficina a la suya propia.
    /// </summary>
    [HttpPost("repartidores")]
    [Authorize(Roles = "Admin,JefeReparto")]
    public async Task<IActionResult> CrearRepartidor([FromBody] CrearRepartidorDto dto)
    {
        try
        {
            var oficinaJefe = await GetOficinaSiJefeRepartoAsync();
            if (oficinaJefe.HasValue && dto.OficinaJsonId != oficinaJefe.Value)
                return Forbid();

            var repartidor = await _repartoService.CrearRepartidor(dto);
            return CreatedAtAction(nameof(ObtenerRepartidores), null, repartidor);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear repartidor");
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Edita la ficha de un repartidor (oficina, vehículo, contacto).
    /// El JefeReparto solo puede editar repartidores de su misma oficina y
    /// no puede moverlos a otra oficina.
    /// </summary>
    [HttpPut("repartidores/{id:int}")]
    [Authorize(Roles = "Admin,JefeReparto")]
    public async Task<IActionResult> EditarRepartidor(int id, [FromBody] EditarRepartidorDto dto)
    {
        var oficinaJefe = await GetOficinaSiJefeRepartoAsync();
        if (oficinaJefe.HasValue)
        {
            if (!await PerteneceAOficinaAsync(id, oficinaJefe.Value))
                return Forbid();
            if (dto.OficinaJsonId != oficinaJefe.Value)
                return Forbid();
        }

        var (repartidor, error) = await _repartoService.EditarRepartidor(id, dto);
        if (repartidor == null)
            return BadRequest(new { message = error });
        return Ok(repartidor);
    }

    /// <summary>
    /// Desactiva un repartidor (soft). Falla si tiene rutas planificadas o en curso.
    /// </summary>
    [HttpDelete("repartidores/{id:int}")]
    [Authorize(Roles = "Admin,JefeReparto")]
    public async Task<IActionResult> DesactivarRepartidor(int id)
    {
        var oficinaJefe = await GetOficinaSiJefeRepartoAsync();
        if (oficinaJefe.HasValue && !await PerteneceAOficinaAsync(id, oficinaJefe.Value))
            return Forbid();

        var (ok, error) = await _repartoService.DesactivarRepartidor(id);
        return ok ? NoContent() : BadRequest(new { message = error });
    }

    /// <summary>
    /// Reactiva un repartidor previamente desactivado.
    /// </summary>
    [HttpPost("repartidores/{id:int}/reactivar")]
    [Authorize(Roles = "Admin,JefeReparto")]
    public async Task<IActionResult> ReactivarRepartidor(int id)
    {
        var oficinaJefe = await GetOficinaSiJefeRepartoAsync();
        if (oficinaJefe.HasValue && !await PerteneceAOficinaAsync(id, oficinaJefe.Value))
            return Forbid();

        var (ok, error) = await _repartoService.ReactivarRepartidor(id);
        return ok ? NoContent() : BadRequest(new { message = error });
    }

    // ═══════════════════════════════════════════
    //  RUTAS DE REPARTO
    // ═══════════════════════════════════════════

    /// <summary>
    /// Obtiene las rutas de reparto. El JefeReparto solo ve rutas de su propia oficina.
    /// </summary>
    [HttpGet("rutas")]
    [Authorize(Roles = "Admin,JefeReparto")]
    public async Task<IActionResult> ObtenerRutas(
        [FromQuery] string? fecha,
        [FromQuery] int? repartidorId)
    {
        try
        {
            DateOnly? fechaParsed = null;
            if (!string.IsNullOrEmpty(fecha) && DateOnly.TryParse(fecha, out var f))
                fechaParsed = f;

            // El JefeReparto solo puede ver las rutas de su oficina
            var oficinaJefe = await GetOficinaSiJefeRepartoAsync();

            var rutas = await _repartoService.ObtenerRutas(fechaParsed, repartidorId, oficinaJefe);
            return Ok(rutas);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo rutas (fecha={Fecha}, repartidorId={RepartidorId})", fecha, repartidorId);
            return StatusCode(500, new { message = "Error obteniendo rutas de reparto.", detail = ex.Message });
        }
    }

    /// <summary>
    /// Cancela una ruta planificada. Solo JefeReparto (de la misma oficina) y Admin.
    /// </summary>
    [HttpPost("rutas/{id:int}/cancelar")]
    [Authorize(Roles = "Admin,JefeReparto")]
    public async Task<IActionResult> CancelarRuta(int id)
    {
        var ruta = await _repartoService.ObtenerRutaPorId(id);
        if (ruta == null)
            return NotFound(new { message = "Ruta no encontrada" });

        var oficinaJefe = await GetOficinaSiJefeRepartoAsync();
        if (oficinaJefe.HasValue && ruta.OficinaOrigenJsonId != oficinaJefe.Value)
            return Forbid();

        var (ok, error) = await _repartoService.CancelarRuta(id);
        return ok ? NoContent() : BadRequest(new { message = error });
    }

    /// <summary>
    /// Reactiva una ruta cancelada a estado Planificada. Solo JefeReparto (misma oficina) y Admin.
    /// </summary>
    [HttpPost("rutas/{id:int}/reactivar")]
    [Authorize(Roles = "Admin,JefeReparto")]
    public async Task<IActionResult> ReactivarRuta(int id)
    {
        var ruta = await _repartoService.ObtenerRutaPorId(id);
        if (ruta == null)
            return NotFound(new { message = "Ruta no encontrada" });

        var oficinaJefe = await GetOficinaSiJefeRepartoAsync();
        if (oficinaJefe.HasValue && ruta.OficinaOrigenJsonId != oficinaJefe.Value)
            return Forbid();

        var (ok, error) = await _repartoService.ReactivarRuta(id);
        return ok ? NoContent() : BadRequest(new { message = error });
    }

    /// <summary>
    /// Obtiene la ruta activa del repartidor autenticado (para driver-app).
    /// </summary>
    [HttpGet("ruta")]
    [Authorize(Roles = "Repartidor")]
    public async Task<IActionResult> ObtenerMiRuta()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                         ?? User.FindFirst("sub")?.Value
                         ?? User.FindFirst("nameid")?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var repartidor = await _repartoService.ObtenerRepartidorPorIdentityId(userId);
            if (repartidor == null)
                return NotFound(new { message = "No existe perfil de repartidor" });

            var hoy = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd");
            var rutas = await _repartoService.ObtenerRutas(DateOnly.FromDateTime(DateTime.UtcNow), repartidor.Id);

            return Ok(rutas);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo ruta del repartidor autenticado");
            return StatusCode(500, new { message = "Error obteniendo ruta del repartidor.", detail = ex.Message });
        }
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
    [Authorize(Roles = "Repartidor")]
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
    [Authorize(Roles = "Repartidor")]
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
    [Authorize(Roles = "Repartidor")]
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
    [Authorize(Roles = "Repartidor")]
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
        try
        {
            var dashboard = await _repartoService.ObtenerDashboard(oficinaJsonId);
            return Ok(dashboard);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo dashboard de reparto (oficinaJsonId={OficinaJsonId})", oficinaJsonId);
            return StatusCode(500, new { message = "Error obteniendo dashboard de reparto.", detail = ex.Message });
        }
    }

    // ═══════════════════════════════════════════
    //  TRACKING TIEMPO REAL (JefeReparto)
    // ═══════════════════════════════════════════

    /// <summary>
    /// Devuelve la última ubicación conocida de cada repartidor activo
    /// (que ha enviado una posición en los últimos N minutos).
    /// Pensado para el mapa en tiempo real del JefeReparto.
    /// </summary>
    [HttpGet("ubicaciones-activas")]
    [Authorize(Roles = "Admin,JefeReparto")]
    public async Task<IActionResult> ObtenerUbicacionesActivas(
        [FromQuery] int? oficinaJsonId,
        [FromQuery] int ventanaMinutos = 10)
    {
        try
        {
            var ubicaciones = await _repartoService.ObtenerUbicacionesActivas(oficinaJsonId, ventanaMinutos);
            return Ok(ubicaciones);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo ubicaciones activas");
            return StatusCode(500, new { message = "Error obteniendo ubicaciones activas.", detail = ex.Message });
        }
    }

    // ═══════════════════════════════════════════
    //  ASIGNACIÓN MANUAL DE PARADAS (JefeReparto)
    // ═══════════════════════════════════════════

    /// <summary>
    /// Lista las entregas pendientes de rutas planificadas del día actual,
    /// para que el JefeReparto pueda redistribuirlas entre repartidores.
    /// </summary>
    [HttpGet("entregas/pendientes-asignacion")]
    [Authorize(Roles = "Admin,JefeReparto")]
    public async Task<IActionResult> ObtenerEntregasPendientesAsignacion([FromQuery] int? oficinaJsonId)
    {
        try
        {
            var pendientes = await _repartoService.ObtenerEntregasPendientesAsignacion(oficinaJsonId);
            return Ok(pendientes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo entregas pendientes de asignación");
            return StatusCode(500, new { message = "Error obteniendo entregas pendientes.", detail = ex.Message });
        }
    }

    /// <summary>
    /// Reasigna una entrega pendiente a otra ruta planificada del día.
    /// </summary>
    [HttpPatch("entregas/{entregaId:int}/reasignar")]
    [Authorize(Roles = "Admin,JefeReparto")]
    public async Task<IActionResult> ReasignarEntrega(int entregaId, [FromBody] ReasignarEntregaDto dto)
    {
        var entrega = await _repartoService.ReasignarEntregaARuta(entregaId, dto.NuevaRutaId);
        if (entrega == null)
            return BadRequest(new { message = "No se pudo reasignar la entrega. Verifique que está pendiente y que la ruta destino existe y está planificada." });
        return Ok(entrega);
    }

    /// <summary>
    /// Registra la ubicación en tiempo real del repartidor (para tracking).
    /// Además de notificar al servicio Ciudadano para el seguimiento público,
    /// persiste la última ubicación del repartidor para el mapa del JefeReparto.
    /// </summary>
    [HttpPost("ubicacion")]
    [Authorize(Roles = "Repartidor")]
    public async Task<IActionResult> RegistrarUbicacion([FromBody] UbicacionRepartidorRequest request)
    {
        _logger.LogInformation("Ubicación recibida de repartidor: lat={Lat}, lng={Lng}",
            request.Latitud, request.Longitud);

        // Persistir última ubicación del repartidor autenticado
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? User.FindFirst("sub")?.Value
                     ?? User.FindFirst("nameid")?.Value;
        if (!string.IsNullOrEmpty(userId))
        {
            await _repartoService.RegistrarUbicacionRepartidor(
                userId,
                request.Latitud,
                request.Longitud,
                request.RutaId);
        }

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

    // ═══════════════════════════════════════════
    //  BANDEJA DEL JEFEREPARTO (paquetes DisponibleParaReparto)
    // ═══════════════════════════════════════════

    /// <summary>
    /// Endpoint interno usado por Intranet al escanear DisponibleParaReparto.
    /// Registra el paquete en la bandeja del JefeReparto del CTA destino.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("interno/bandeja/registrar")]
    [ProducesResponseType(typeof(RegistrarPaqueteBandejaResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> RegistrarPaqueteEnBandeja([FromBody] RegistrarPaqueteBandejaRequestDto dto)
    {
        if (!IsInternalServiceAuthorized())
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Service key inválida" });

        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var resultado = await _bandejaService.RegistrarPaqueteAsync(dto);
        if (!resultado.Success)
            return BadRequest(resultado);

        return Ok(resultado);
    }

    /// <summary>
    /// Lista los paquetes en la bandeja del JefeReparto.
    /// Por defecto filtra por CTA recibido en query y oculta los ya asignados a ruta.
    /// </summary>
    [HttpGet("bandeja")]
    [Authorize(Roles = "Admin,JefeReparto")]
    public async Task<IActionResult> ObtenerBandeja(
        [FromQuery] int? ctaId,
        [FromQuery] bool incluirAsignados = false)
    {
        var pendientes = await _bandejaService.ListarPendientesAsync(ctaId, incluirAsignados);
        return Ok(pendientes);
    }

    /// <summary>
    /// El JefeReparto añade un pendiente de la bandeja a una ruta planificada.
    /// Crea la EntregaPaquete asociada y marca el pendiente como asignado.
    /// </summary>
    [HttpPost("bandeja/{pendienteId:int}/asignar-a-ruta")]
    [Authorize(Roles = "Admin,JefeReparto")]
    public async Task<IActionResult> AsignarPendienteARuta(int pendienteId, [FromBody] AsignarPendienteARutaDto dto)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? User.FindFirst("sub")?.Value
                     ?? User.FindFirst("nameid")?.Value;

        var (pendiente, entrega, error) = await _bandejaService.AsignarARutaAsync(pendienteId, dto, userId);
        if (error is not null)
            return BadRequest(new { message = error });

        return Ok(new { pendiente, entrega });
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

    /// <summary>
    /// Si el usuario autenticado es JefeReparto (y NO Admin), devuelve su OficinaJsonId
    /// para limitar la operación a su propia oficina. Devuelve null si es Admin o no se
    /// puede resolver el perfil.
    /// </summary>
    private async Task<int?> GetOficinaSiJefeRepartoAsync()
    {
        if (User.IsInRole("Admin")) return null;
        if (!User.IsInRole("JefeReparto")) return null;

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? User.FindFirst("sub")?.Value
                     ?? User.FindFirst("nameid")?.Value;
        if (string.IsNullOrEmpty(userId)) return null;

        var perfil = await _repartoService.ObtenerRepartidorPorIdentityId(userId);
        return perfil?.OficinaJsonId;
    }

    private async Task<bool> PerteneceAOficinaAsync(int repartidorId, int oficinaJsonId)
    {
        var lista = await _repartoService.ObtenerRepartidores(oficinaJsonId, incluirInactivos: true);
        return lista.Any(r => r.Id == repartidorId);
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
