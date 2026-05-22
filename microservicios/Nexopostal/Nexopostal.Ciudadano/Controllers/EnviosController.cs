using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexopostal.Ciudadano.DTOs;
using Nexopostal.Ciudadano.Models;
using Nexopostal.Ciudadano.Repositories;
using Nexopostal.Ciudadano.Services;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Nexopostal.Ciudadano.Controllers;

/// <summary>
/// Controlador para la gestión de envíos.
/// Implementa dos niveles de seguimiento:
///   - Público (NumeroSeguimiento): para clientes → /track/{numero}
///   - Interno (NumeroExpedicion): para operarios/repartidores → /interno/{expedicion}
/// </summary>
[Route("api/[controller]")]
[ApiController]
public class EnviosController : ControllerBase
{
    private readonly IEnvioRepository _envioRepo;
    private readonly ITrackingNumberGenerator _trackingGenerator;
    private readonly IFacturaPdfService _facturaPdfService;
    private readonly IEtiquetaPdfService _etiquetaPdfService;
    private readonly ITarifasService _tarifasService;
    private readonly ITrackingNotificacionService _trackingNotificacionService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<EnviosController> _logger;

    public EnviosController(
        IEnvioRepository envioRepo,
        ITrackingNumberGenerator trackingGenerator,
        IFacturaPdfService facturaPdfService,
        IEtiquetaPdfService etiquetaPdfService,
        ITarifasService tarifasService,
        ITrackingNotificacionService trackingNotificacionService,
        IConfiguration configuration,
        ILogger<EnviosController> logger)
    {
        _envioRepo = envioRepo;
        _trackingGenerator = trackingGenerator;
        _facturaPdfService = facturaPdfService;
        _etiquetaPdfService = etiquetaPdfService;
        _tarifasService = tarifasService;
        _trackingNotificacionService = trackingNotificacionService;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Cotiza el precio de un envío sin necesidad de autenticación
    /// Anteproyecto: "Motor de cálculo para determinar costes"
    /// </summary>
    /// <param name="dto">Datos del paquete (peso, dimensiones, origen, destino)</param>
    /// <returns>Precio estimado y tiempo de entrega</returns>
    [HttpPost("cotizar")]
    [ProducesResponseType(typeof(CotizacionResultadoDto), StatusCodes.Status200OK)]
    public IActionResult Cotizar([FromBody] CotizarEnvioDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var dimensiones = _tarifasService.ParseDimensiones(dto.Dimensiones);
        var tarifa = _tarifasService.Calcular(new TarifaCalculoInput(
            dto.Peso,
            dimensiones.Largo,
            dimensiones.Ancho,
            dimensiones.Alto,
            dto.CodigoPostalOrigen,
            dto.CodigoPostalDestino,
            "Estandar"));

        var resultado = new CotizacionResultadoDto
        {
            Precio = tarifa.PrecioTotal,
            Moneda = "EUR",
            TiempoEstimadoDias = tarifa.TiempoEstimadoDias,
            Observaciones = tarifa.TiempoEstimadoDias <= 2
                ? "Entrega rápida disponible"
                : "Entrega estándar"
        };

        _logger.LogInformation(
            "Cotización realizada: {Peso}kg, Zona={Zona}, Precio={Precio}€",
            tarifa.PesoFacturable,
            tarifa.Zona,
            resultado.Precio);

        return Ok(resultado);
    }

    /// <summary>
    /// Crea un nuevo envío (requiere autenticación)
    /// Anteproyecto: "Admisión de Envíos Digital"
    /// </summary>
    /// <param name="dto">Datos completos del envío</param>
    /// <returns>Datos del envío creado con número de seguimiento</returns>
    [Authorize]
    [HttpPost("crear")]
    [ProducesResponseType(typeof(EnvioCreadoDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CrearEnvio([FromBody] CrearEnvioDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        // Validación de modalidad de entrega + coherencia oficina destino
        if (!Enum.TryParse<TipoEntrega>(dto.TipoEntrega, ignoreCase: true, out var tipoEntrega))
            return BadRequest(new { mensaje = $"TipoEntrega no válido: {dto.TipoEntrega}. Valores: Domicilio, Oficina." });

        if (tipoEntrega == TipoEntrega.Oficina && (dto.OficinaDestinoId is null or <= 0))
            return BadRequest(new { mensaje = "OficinaDestinoId es obligatorio cuando TipoEntrega == Oficina." });

        if (tipoEntrega == TipoEntrega.Domicilio && dto.OficinaDestinoId is not null)
            return BadRequest(new { mensaje = "OficinaDestinoId debe ser null cuando TipoEntrega == Domicilio." });

        if (dto.OficinaOrigenId <= 0)
            return BadRequest(new { mensaje = "OficinaOrigenId es obligatorio." });

        // Obtenemos el ID del usuario desde el token JWT
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                     ?? User.FindFirst("sub")?.Value
                     ?? User.FindFirst("uid")?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning("No se pudo extraer el userId del token");
            return Unauthorized("Token inválido");
        }

        var dimensiones = _tarifasService.ParseDimensiones(dto.Dimensiones);
        var tarifa = _tarifasService.Calcular(new TarifaCalculoInput(
            dto.Peso,
            dimensiones.Largo,
            dimensiones.Ancho,
            dimensiones.Alto,
            dto.CodigoPostalOrigen,
            dto.CodigoPostalDestino,
            "Estandar"));

        // Creamos el envío
        var envio = new Envio
        {
            NumeroSeguimiento = _trackingGenerator.Generate(),
            NumeroExpedicion = _trackingGenerator.GenerateExpedicion(),
            IdentityUserId = userId,
            PesoKg = dto.Peso,
            Dimensiones = dto.Dimensiones,
            Origen = dto.Origen,
            Destino = dto.Destino,
            CodigoPostalOrigen = dto.CodigoPostalOrigen,
            CodigoPostalDestino = dto.CodigoPostalDestino,
            OficinaOrigenId = dto.OficinaOrigenId,
            OficinaDestinoId = dto.OficinaDestinoId,
            TipoEntrega = tipoEntrega,
            EstadoActual = EstadoEnvio.Admitido,
            EstadoInternoActual = EstadoInterno.PendienteRecogida,
            FechaCreacion = DateTime.UtcNow,
            CosteCalculado = tarifa.PrecioTotal,
            TipoTarifa = tarifa.TipoTarifa,
            TiempoEntregaEstimado = tarifa.TiempoEntregaEstimado,
            Pagado = false,
            Observaciones = dto.Observaciones,
            NombreRemitente = dto.NombreRemitente,
            TelefonoRemitente = dto.TelefonoRemitente ?? string.Empty,
            NombreDestinatario = dto.NombreDestinatario,
            TelefonoDestinatario = dto.TelefonoDestinatario ?? string.Empty
        };

        await _envioRepo.CreateAsync(envio);

        _logger.LogInformation(
            "Envío creado: {NumeroSeguimiento} por usuario {UserId} (TipoEntrega={TipoEntrega}, OficinaOrigen={OO}, OficinaDestino={OD})",
            envio.NumeroSeguimiento, userId, tipoEntrega, dto.OficinaOrigenId, dto.OficinaDestinoId);

        // Construimos la respuesta
        var respuesta = new EnvioCreadoDto
        {
            NumeroSeguimiento = envio.NumeroSeguimiento,
            NumeroExpedicion = envio.NumeroExpedicion,
            CosteCalculado = envio.CosteCalculado,
            EstadoActual = envio.EstadoActual.ToString(),
            TipoEntrega = envio.TipoEntrega.ToString(),
            OficinaOrigenId = envio.OficinaOrigenId,
            OficinaDestinoId = envio.OficinaDestinoId,
            FechaCreacion = envio.FechaCreacion,
            UrlEtiqueta = $"/api/etiquetas/{envio.NumeroSeguimiento}"
        };

        return CreatedAtAction(
            nameof(GetEnvioPorNumero),
            new { numero = envio.NumeroSeguimiento },
            respuesta);
    }

    /// <summary>
    /// Obtiene el tracking de un envío (público - no requiere autenticación)
    /// Anteproyecto: "Consulta del estado del envío mediante código único"
    /// </summary>
    /// <param name="numero">Número de seguimiento del envío</param>
    /// <returns>Información del estado actual del envío</returns>
    [HttpGet("track/{numero}")]
    [ProducesResponseType(typeof(EnvioTrackingDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEnvioPorNumero(string numero)
    {
        var envio = await _envioRepo.GetByTrackingAsync(numero);

        if (envio == null)
        {
            _logger.LogWarning("Intento de tracking de envío inexistente: {Numero}", numero);
            return NotFound(new { mensaje = "Envío no encontrado" });
        }

        var resultado = new EnvioTrackingDto
        {
            NumeroSeguimiento = envio.NumeroSeguimiento,
            EstadoActual = envio.EstadoActual.ToString(),
            Descripcion = ObtenerDescripcionEstado(envio.EstadoActual),
            FechaCreacion = envio.FechaCreacion,
            NumeroBultos = 1
        };

        return Ok(resultado);
    }

    /// <summary>
    /// Obtiene todos los envíos del usuario autenticado
    /// </summary>
    /// <returns>Lista de envíos del usuario</returns>
    [Authorize]
    [HttpGet("mis-envios")]
    [ProducesResponseType(typeof(IEnumerable<EnvioResumenDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMisEnvios()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                     ?? User.FindFirst("sub")?.Value
                     ?? User.FindFirst("uid")?.Value;

        if (string.IsNullOrEmpty(userId))
            return Unauthorized("Token inválido");

        var enviosList = await _envioRepo.GetByUserAsync(userId);
        var envios = enviosList.Select(e => new EnvioResumenDto
        {
            NumeroSeguimiento = e.NumeroSeguimiento,
            Estado = e.EstadoActual.ToString(),
            FechaCreacion = e.FechaCreacion,
            Destino = e.Destino,
            Precio = e.CosteCalculado,
            Pagado = e.Pagado,
            TipoTarifa = e.TipoTarifa
        }).ToList();

        return Ok(envios);
    }

    /// <summary>
    /// Descarga la factura de un envío pagado en formato PDF
    /// </summary>
    [Authorize]
    [HttpGet("factura/{numero}")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DescargarFactura(string numero)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? User.FindFirst("sub")?.Value
                     ?? User.FindFirst("uid")?.Value;

        if (string.IsNullOrEmpty(userId))
            return Unauthorized("Token inválido");

        var envio = await _envioRepo.GetByTrackingAndUserAsync(numero, userId);

        if (envio == null)
            return NotFound(new { mensaje = "Envío no encontrado" });

        if (!envio.Pagado)
            return BadRequest(new { mensaje = "El envío aún no ha sido pagado" });

        var pdfBytes = _facturaPdfService.GenerarFactura(envio);
        return File(pdfBytes, "application/pdf", $"Factura_{numero}.pdf");
    }

    /// <summary>
    /// Descarga la etiqueta de un envío pagado en formato PDF
    /// </summary>
    [Authorize]
    [HttpGet("etiqueta/{numero}")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DescargarEtiqueta(string numero)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? User.FindFirst("sub")?.Value
                     ?? User.FindFirst("uid")?.Value;

        if (string.IsNullOrEmpty(userId))
            return Unauthorized("Token inválido");

        var envio = await _envioRepo.GetByTrackingAndUserAsync(numero, userId);

        if (envio == null)
            return NotFound(new { mensaje = "Envío no encontrado" });

        if (!envio.Pagado)
            return BadRequest(new { mensaje = "El envío aún no ha sido pagado" });

        var pdfBytes = _etiquetaPdfService.GenerarEtiqueta(envio);
        return File(pdfBytes, "application/pdf", $"Etiqueta_{numero}.pdf");
    }

    // ===== ENDPOINTS INTERNOS (Intranet / Driver-App) =====

    /// <summary>
    /// Endpoint inter-servicio: devuelve los datos operativos esenciales de un envío
    /// para que otros microservicios (Intranet, Reparto) encadenen flujos sin duplicar datos.
    /// Auth por X-Service-Key.
    /// </summary>
    [AllowAnonymous]
    [HttpGet("interno/service/{expedicion}")]
    [ProducesResponseType(typeof(EnvioInternoServiceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEnvioInternoService(string expedicion)
    {
        if (!IsInternalServiceAuthorized())
            return StatusCode(StatusCodes.Status403Forbidden, new { mensaje = "Service key inválida" });

        var envio = await _envioRepo.GetByExpedicionAsync(expedicion);
        if (envio == null)
            return NotFound(new { mensaje = "Expedición no encontrada" });

        return Ok(new EnvioInternoServiceDto
        {
            NumeroSeguimiento = envio.NumeroSeguimiento,
            NumeroExpedicion = envio.NumeroExpedicion,
            EstadoPublico = envio.EstadoActual.ToString(),
            EstadoInterno = envio.EstadoInternoActual.ToString(),
            TipoEntrega = envio.TipoEntrega.ToString(),
            OficinaOrigenId = envio.OficinaOrigenId,
            OficinaDestinoId = envio.OficinaDestinoId,
            CodigoPostalOrigen = envio.CodigoPostalOrigen,
            CodigoPostalDestino = envio.CodigoPostalDestino,
            Origen = envio.Origen,
            Destino = envio.Destino,
            NombreDestinatario = envio.NombreDestinatario,
            ApellidosDestinatario = envio.ApellidosDestinatario,
            TelefonoDestinatario = envio.TelefonoDestinatario,
            EmailDestinatario = envio.EmailDestinatario,
            PesoKg = envio.PesoKg,
            Dimensiones = envio.Dimensiones,
            TipoTarifa = envio.TipoTarifa,
            Pagado = envio.Pagado,
            FechaCreacion = envio.FechaCreacion
        });
    }

    /// <summary>
    /// Obtiene el detalle interno completo de un envío por su NumeroExpedicion.
    /// Solo accesible con autenticación (operarios/repartidores).
    /// </summary>
    [Authorize]
    [HttpGet("interno/{expedicion}")]
    [ProducesResponseType(typeof(EnvioInternoDetalladoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEnvioInterno(string expedicion)
    {
        var envio = await _envioRepo.GetByExpedicionAsync(expedicion);

        if (envio == null)
        {
            _logger.LogWarning("Búsqueda interna de expedición inexistente: {Expedicion}", expedicion);
            return NotFound(new { mensaje = "Expedición no encontrada" });
        }

        return Ok(MapToInternoDetallado(envio));
    }

    /// <summary>
    /// Obtiene el detalle interno de un envío por su NumeroSeguimiento público.
    /// Solo accesible con autenticación (operarios/repartidores).
    /// </summary>
    [Authorize]
    [HttpGet("interno/por-seguimiento/{numero}")]
    [ProducesResponseType(typeof(EnvioInternoDetalladoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEnvioInternoPorSeguimiento(string numero)
    {
        var envio = await _envioRepo.GetByTrackingAsync(numero);

        if (envio == null)
            return NotFound(new { mensaje = "Envío no encontrado" });

        return Ok(MapToInternoDetallado(envio));
    }

    /// <summary>
    /// Lista todos los envíos con información interna (para intranet/driver-app).
    /// Soporta filtros por estado interno y código postal destino.
    /// </summary>
    [Authorize]
    [HttpGet("interno/listar")]
    [ProducesResponseType(typeof(IEnumerable<EnvioResumenInternoDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListarEnviosInternos(
        [FromQuery] string? estadoInterno = null,
        [FromQuery] string? codigoPostal = null)
    {
        EstadoInterno? estadoInternoEnum = null;
        if (!string.IsNullOrEmpty(estadoInterno) && Enum.TryParse<EstadoInterno>(estadoInterno, out var estado))
        {
            estadoInternoEnum = estado;
        }

        var enviosList = await _envioRepo.GetByEstadoInternoAsync(estadoInternoEnum, codigoPostal);

        var envios = enviosList.Select(e => new EnvioResumenInternoDto
        {
            NumeroSeguimiento = e.NumeroSeguimiento,
            NumeroExpedicion = e.NumeroExpedicion,
            EstadoPublico = e.EstadoActual.ToString(),
            EstadoInterno = e.EstadoInternoActual.ToString(),
            FechaCreacion = e.FechaCreacion,
            Origen = e.Origen,
            Destino = e.Destino,
            CodigoPostalDestino = e.CodigoPostalDestino,
            PesoKg = e.PesoKg,
            TipoTarifa = e.TipoTarifa,
            Pagado = e.Pagado,
            TipoEntrega = e.TipoEntrega.ToString(),
            OficinaOrigenId = e.OficinaOrigenId,
            OficinaDestinoId = e.OficinaDestinoId
        }).ToList();

        return Ok(envios);
    }

    /// <summary>
    /// Actualiza el estado interno de un envío (operarios/repartidores).
    /// El estado público se sincroniza automáticamente.
    /// </summary>
    [Authorize]
    [HttpPut("interno/{expedicion}/estado")]
    [ProducesResponseType(typeof(EnvioInternoDetalladoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ActualizarEstadoInterno(
        string expedicion,
        [FromBody] ActualizarEstadoInternoDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var envio = await _envioRepo.GetByExpedicionAsync(expedicion);

        if (envio == null)
            return NotFound(new { mensaje = "Expedición no encontrada" });

        if (!Enum.TryParse<EstadoInterno>(dto.NuevoEstadoInterno, out var nuevoEstado))
            return BadRequest(new { mensaje = $"Estado interno no válido: {dto.NuevoEstadoInterno}" });

        var descripcion = string.IsNullOrWhiteSpace(dto.Observaciones)
            ? ObtenerDescripcionEstadoInterno(nuevoEstado)
            : dto.Observaciones;

        await AplicarCambioEstadoInternoYNotificar(
            envio,
            nuevoEstado,
            dto.Observaciones,
            descripcion,
            tipoUbicacion: "OperacionInterna");

        _logger.LogInformation(
            "Estado interno de {Expedicion} actualizado a {Estado} (público: {EstadoPublico})",
            expedicion, nuevoEstado, envio.EstadoActual);

        return Ok(MapToInternoDetallado(envio));
    }

    /// <summary>
    /// Endpoint interno para publicar ubicación de reparto en el tracking público (SignalR).
    /// Consumido por el microservicio de Reparto.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("interno/tracking/ubicacion")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> NotificarUbicacionReparto([FromBody] TrackingUbicacionRepartoDto dto)
    {
        if (!IsInternalServiceAuthorized())
            return StatusCode(StatusCodes.Status403Forbidden, new { mensaje = "Service key inválida" });

        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var envio = await _envioRepo.GetByTrackingAsync(dto.NumeroSeguimiento);
        if (envio == null)
            return NotFound(new { mensaje = "Envío no encontrado" });

        var ubicacion = string.IsNullOrWhiteSpace(dto.Ubicacion)
            ? $"Lat {dto.Latitud:F5}, Lng {dto.Longitud:F5}"
            : dto.Ubicacion;

        var descripcion = string.IsNullOrWhiteSpace(dto.Descripcion)
            ? "El repartidor está en camino con tu envío"
            : dto.Descripcion;

        await _trackingNotificacionService.NotificarCambioUbicacion(
            dto.NumeroSeguimiento,
            ubicacion,
            dto.TipoUbicacion,
            descripcion,
            dto.Latitud,
            dto.Longitud);

        _logger.LogInformation(
            "Tracking ubicación emitido para {Seguimiento} ({Lat}, {Lng})",
            dto.NumeroSeguimiento,
            dto.Latitud,
            dto.Longitud);

        return Accepted(new { mensaje = "Ubicación de tracking publicada" });
    }

    /// <summary>
    /// Endpoint interno para sincronizar eventos de entrega de Reparto.
    /// Actualiza estado interno/público del envío y publica eventos realtime consistentes.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("interno/tracking/evento-entrega")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> NotificarEventoEntregaReparto([FromBody] TrackingEventoEntregaDto dto)
    {
        if (!IsInternalServiceAuthorized())
            return StatusCode(StatusCodes.Status403Forbidden, new { mensaje = "Service key inválida" });

        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var envio = await _envioRepo.GetByTrackingAsync(dto.NumeroSeguimiento);
        if (envio == null)
            return NotFound(new { mensaje = "Envío no encontrado" });

        if (!TryMapearEstadoInternoDesdeEntrega(dto, out var nuevoEstadoInterno))
            return BadRequest(new { mensaje = $"Estado de entrega no soportado: {dto.EstadoEntrega}" });

        var descripcion = ConstruirDescripcionEventoEntrega(dto, nuevoEstadoInterno);

        await AplicarCambioEstadoInternoYNotificar(
            envio,
            nuevoEstadoInterno,
            dto.Observaciones,
            descripcion,
            dto.Latitud,
            dto.Longitud,
            "EventoEntregaReparto");

        _logger.LogInformation(
            "Evento de reparto sincronizado para {Seguimiento}: {EstadoEntrega} -> {EstadoInterno} (público: {EstadoPublico})",
            dto.NumeroSeguimiento,
            dto.EstadoEntrega,
            envio.EstadoInternoActual,
            envio.EstadoActual);

        return Accepted(new
        {
            mensaje = "Evento de reparto sincronizado",
            estadoInterno = envio.EstadoInternoActual.ToString(),
            estadoPublico = envio.EstadoActual.ToString()
        });
    }

    // ===== MÉTODOS AUXILIARES =====

    private async Task AplicarCambioEstadoInternoYNotificar(
        Envio envio,
        EstadoInterno nuevoEstado,
        string? observaciones,
        string descripcionTracking,
        double? latitud = null,
        double? longitud = null,
        string tipoUbicacion = "OperacionInterna")
    {
        envio.EstadoInternoActual = nuevoEstado;
        envio.EstadoActual = DeducirEstadoPublico(nuevoEstado);

        if (!string.IsNullOrWhiteSpace(observaciones))
        {
            envio.Observaciones = ConstruirObservaciones(envio.Observaciones, observaciones);
        }

        await _envioRepo.UpdateAsync(envio);

        var ubicacion = latitud.HasValue && longitud.HasValue
            ? $"Lat {latitud:F5}, Lng {longitud:F5}"
            : null;

        await _trackingNotificacionService.NotificarCambioEstado(
            envio.NumeroSeguimiento,
            envio.EstadoActual.ToString(),
            envio.EstadoInternoActual.ToString(),
            descripcionTracking,
            ubicacion);

        if (latitud.HasValue && longitud.HasValue)
        {
            await _trackingNotificacionService.NotificarCambioUbicacion(
                envio.NumeroSeguimiento,
                ubicacion ?? string.Empty,
                tipoUbicacion,
                descripcionTracking,
                latitud,
                longitud);
        }

        if (envio.EstadoActual == EstadoEnvio.Entregado)
        {
            await _trackingNotificacionService.NotificarEntregaCompletada(
                envio.NumeroSeguimiento,
                envio.EstadoInternoActual.ToString(),
                descripcionTracking);
        }
        else if (envio.EstadoActual == EstadoEnvio.Incidencia || envio.EstadoActual == EstadoEnvio.Devuelto)
        {
            await _trackingNotificacionService.NotificarIncidencia(
                envio.NumeroSeguimiento,
                envio.EstadoInternoActual.ToString(),
                descripcionTracking);
        }
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

    private static bool TryMapearEstadoInternoDesdeEntrega(
        TrackingEventoEntregaDto dto,
        out EstadoInterno nuevoEstadoInterno)
    {
        var estadoEntrega = dto.EstadoEntrega?.Trim().ToUpperInvariant();

        nuevoEstadoInterno = estadoEntrega switch
        {
            "ENTREGADO" => EstadoInterno.EntregadoEnDomicilio,
            "ENTREGADOPUNTOALTERNATIVO" => EstadoInterno.EntregadoEnOficina,
            "AUSENTE" => dto.NumeroIntento >= 2
                ? EstadoInterno.SegundoIntentoFallido
                : EstadoInterno.PrimerIntentoFallido,
            "DIRECCIONINCORRECTA" => EstadoInterno.IncidenciaDireccionIncorrecta,
            "RECHAZADO" => EstadoInterno.IncidenciaDestinatarioRechaza,
            "DEVUELTOAOFICINA" => EstadoInterno.EnDevolucionAlRemitente,
            "ENCAMINO" => EstadoInterno.EnReparto,
            "PENDIENTE" => EstadoInterno.AsignadoARuta,
            _ => (EstadoInterno)(-999)
        };

        return Enum.IsDefined(typeof(EstadoInterno), nuevoEstadoInterno) && (int)nuevoEstadoInterno >= -1;
    }

    private static string ConstruirDescripcionEventoEntrega(
        TrackingEventoEntregaDto dto,
        EstadoInterno estadoInterno)
    {
        var baseEstado = estadoInterno switch
        {
            EstadoInterno.EntregadoEnDomicilio => "Entrega completada en domicilio",
            EstadoInterno.EntregadoEnOficina => "Entrega completada en punto alternativo/oficina",
            EstadoInterno.PrimerIntentoFallido => "Intento de entrega fallido: destinatario ausente",
            EstadoInterno.SegundoIntentoFallido => "Segundo intento de entrega fallido",
            EstadoInterno.IncidenciaDireccionIncorrecta => "Incidencia en reparto: dirección incorrecta",
            EstadoInterno.IncidenciaDestinatarioRechaza => "Incidencia en reparto: envío rechazado por destinatario",
            EstadoInterno.EnDevolucionAlRemitente => "Envío en devolución tras reparto",
            EstadoInterno.EnReparto => "Envío en reparto",
            _ => ObtenerDescripcionEstadoInterno(estadoInterno)
        };

        if (!string.IsNullOrWhiteSpace(dto.Observaciones))
        {
            return $"{baseEstado}. {dto.Observaciones}";
        }

        return baseEstado;
    }

    private static string ConstruirObservaciones(string? observacionesActuales, string nuevaNota)
    {
        var timestamp = DateTime.UtcNow.ToString("dd/MM/yyyy HH:mm");
        var nota = $"[{timestamp}] {nuevaNota}";

        return string.IsNullOrEmpty(observacionesActuales)
            ? nota
            : $"{observacionesActuales}\n{nota}";
    }

    /// <summary>
    /// Devuelve una descripción pública genérica del estado del envío
    /// sin revelar datos sensibles (origen, destino, direcciones).
    /// </summary>
    private static string ObtenerDescripcionEstado(EstadoEnvio estado) => estado switch
    {
        EstadoEnvio.PendientePago => "Envío pendiente de confirmación de pago",
        EstadoEnvio.Admitido => "Envío admitido en oficina de NexoPostal",
        EstadoEnvio.EnTransito => "Envío en tránsito hacia destino",
        EstadoEnvio.EnOficina => "Envío disponible en oficina de destino",
        EstadoEnvio.EnReparto => "Envío en reparto — pendiente de entrega",
        EstadoEnvio.Entregado => "Envío entregado al destinatario o autorizado en oficina",
        EstadoEnvio.Incidencia => "Incidencia registrada — contacte con atención al cliente",
        EstadoEnvio.Devuelto => "Envío devuelto al remitente",
        _ => "Estado desconocido"
    };

    /// <summary>
    /// Devuelve una descripción detallada del estado interno del envío.
    /// </summary>
    private static string ObtenerDescripcionEstadoInterno(EstadoInterno estado) => estado switch
    {
        EstadoInterno.PendientePago => "Pendiente de confirmación de pago",
        EstadoInterno.PendienteRecogida => "Pagado — esperando recogida en origen",
        EstadoInterno.RecogidoEnOrigen => "Recogido por repartidor en dirección de origen",
        EstadoInterno.RecibidoEnCentroOrigen => "Recibido en centro de clasificación de origen",
        EstadoInterno.EnClasificacionOrigen => "En proceso de clasificación en centro de origen",
        EstadoInterno.ClasificadoParaExpedicion => "Clasificado y listo para expedición",
        EstadoInterno.EnTransitoHaciaCentroDestino => "En tránsito hacia centro de destino",
        EstadoInterno.EnTransitoIntermedio => "En tránsito por centro intermedio",
        EstadoInterno.RecibidoEnCentroDestino => "Recibido en centro de clasificación de destino",
        EstadoInterno.EnClasificacionDestino => "En clasificación en centro de destino",
        EstadoInterno.AsignadoARuta => "Asignado a ruta de reparto",
        EstadoInterno.EnReparto => "En reparto — repartidor en ruta",
        EstadoInterno.PrimerIntentoFallido => "Primer intento de entrega fallido (ausente)",
        EstadoInterno.SegundoIntentoFallido => "Segundo intento de entrega fallido",
        EstadoInterno.DepositivoEnOficina => "Depositado en oficina para recogida",
        EstadoInterno.EntregadoEnDomicilio => "Entregado en domicilio del destinatario",
        EstadoInterno.EntregadoEnOficina => "Recogido por destinatario en oficina",
        EstadoInterno.EntregadoAAutorizado => "Entregado a persona autorizada",
        EstadoInterno.IncidenciaDireccionIncorrecta => "Incidencia: dirección incorrecta o incompleta",
        EstadoInterno.IncidenciaPaqueteDanado => "Incidencia: paquete dañado",
        EstadoInterno.IncidenciaDestinatarioRechaza => "Incidencia: destinatario rechaza el envío",
        EstadoInterno.IncidenciaOtra => "Incidencia registrada",
        EstadoInterno.EnDevolucionAlRemitente => "En proceso de devolución al remitente",
        EstadoInterno.DevueltoAlRemitente => "Devuelto y entregado al remitente",
        _ => "Estado desconocido"
    };

    /// <summary>
    /// Deduce el estado público simplificado a partir del estado interno detallado.
    /// El cliente solo ve el estado público, no el detalle operativo.
    /// </summary>
    private static EstadoEnvio DeducirEstadoPublico(EstadoInterno estadoInterno) => estadoInterno switch
    {
        EstadoInterno.PendientePago => EstadoEnvio.PendientePago,
        EstadoInterno.PendienteRecogida => EstadoEnvio.Admitido,
        EstadoInterno.RecogidoEnOrigen => EstadoEnvio.Admitido,
        EstadoInterno.RecibidoEnCentroOrigen => EstadoEnvio.EnTransito,
        EstadoInterno.EnClasificacionOrigen => EstadoEnvio.EnTransito,
        EstadoInterno.ClasificadoParaExpedicion => EstadoEnvio.EnTransito,
        EstadoInterno.EnTransitoHaciaCentroDestino => EstadoEnvio.EnTransito,
        EstadoInterno.EnTransitoIntermedio => EstadoEnvio.EnTransito,
        EstadoInterno.RecibidoEnCentroDestino => EstadoEnvio.EnTransito,
        EstadoInterno.EnClasificacionDestino => EstadoEnvio.EnTransito,
        EstadoInterno.AsignadoARuta => EstadoEnvio.EnReparto,
        EstadoInterno.EnReparto => EstadoEnvio.EnReparto,
        EstadoInterno.PrimerIntentoFallido => EstadoEnvio.EnReparto,
        EstadoInterno.SegundoIntentoFallido => EstadoEnvio.EnReparto,
        EstadoInterno.DepositivoEnOficina => EstadoEnvio.EnOficina,
        EstadoInterno.EntregadoEnDomicilio => EstadoEnvio.Entregado,
        EstadoInterno.EntregadoEnOficina => EstadoEnvio.Entregado,
        EstadoInterno.EntregadoAAutorizado => EstadoEnvio.Entregado,
        EstadoInterno.IncidenciaDireccionIncorrecta => EstadoEnvio.Incidencia,
        EstadoInterno.IncidenciaPaqueteDanado => EstadoEnvio.Incidencia,
        EstadoInterno.IncidenciaDestinatarioRechaza => EstadoEnvio.Incidencia,
        EstadoInterno.IncidenciaOtra => EstadoEnvio.Incidencia,
        EstadoInterno.EnDevolucionAlRemitente => EstadoEnvio.Devuelto,
        EstadoInterno.DevueltoAlRemitente => EstadoEnvio.Devuelto,
        _ => EstadoEnvio.Admitido
    };

    /// <summary>
    /// Mapea un Envio al DTO interno detallado
    /// </summary>
    private static EnvioInternoDetalladoDto MapToInternoDetallado(Envio envio) => new()
    {
        NumeroSeguimiento = envio.NumeroSeguimiento,
        NumeroExpedicion = envio.NumeroExpedicion,
        EstadoPublico = envio.EstadoActual.ToString(),
        EstadoInterno = envio.EstadoInternoActual.ToString(),
        DescripcionEstadoInterno = ObtenerDescripcionEstadoInterno(envio.EstadoInternoActual),
        PesoKg = envio.PesoKg,
        Dimensiones = envio.Dimensiones,
        Origen = envio.Origen,
        Destino = envio.Destino,
        CodigoPostalOrigen = envio.CodigoPostalOrigen,
        CodigoPostalDestino = envio.CodigoPostalDestino,
        NombreRemitente = envio.NombreRemitente,
        ApellidosRemitente = envio.ApellidosRemitente,
        TelefonoRemitente = envio.TelefonoRemitente,
        EmailRemitente = envio.EmailRemitente,
        DniRemitente = envio.DniRemitente,
        NombreDestinatario = envio.NombreDestinatario,
        ApellidosDestinatario = envio.ApellidosDestinatario,
        TelefonoDestinatario = envio.TelefonoDestinatario,
        EmailDestinatario = envio.EmailDestinatario,
        DniDestinatario = envio.DniDestinatario,
        TipoTarifa = envio.TipoTarifa,
        TiempoEntregaEstimado = envio.TiempoEntregaEstimado,
        CosteCalculado = envio.CosteCalculado,
        Pagado = envio.Pagado,
        FechaCreacion = envio.FechaCreacion,
        FechaPago = envio.FechaPago,
        Observaciones = envio.Observaciones,
        TipoEntrega = envio.TipoEntrega.ToString(),
        OficinaOrigenId = envio.OficinaOrigenId,
        OficinaDestinoId = envio.OficinaDestinoId
    };

    /// <summary>
    /// Calcula el tiempo estimado de entrega entre dos códigos postales
    /// (Lógica simplificada - en producción usarías un algoritmo real)
    /// </summary>
    private int CalcularTiempoEstimado(string cpOrigen, string cpDestino)
    {
        // Simplificación: si los primeros 2 dígitos son iguales, misma provincia
        if (cpOrigen.Length >= 2 && cpDestino.Length >= 2)
        {
            if (cpOrigen.Substring(0, 2) == cpDestino.Substring(0, 2))
                return 1; // Misma provincia: 1 día

            return 2; // Provincia diferente: 2 días
        }

        return 3; // Por defecto: 3 días
    }
}
