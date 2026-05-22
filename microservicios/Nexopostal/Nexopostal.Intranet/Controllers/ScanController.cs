using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexopostal.Intranet.DTOs;
using Nexopostal.Intranet.Services;
using System.Security.Claims;

namespace Nexopostal.Intranet.Controllers;

/// <summary>
/// Controlador para el procesamiento de escaneos de códigos de barras.
/// 
/// Separación de responsabilidades por rol:
///   - OperarioOficina: solo modos de oficina (RecepcionOficina, EntregaOficinaDestino, SalidaAReparto)
///   - OperarioCTA:     solo modos de CTA (RecepcionCta, Clasificacion, DespachoTroncal, RecepcionTroncal)
///   - Supervisor:      sin acceso a escaneo (solo supervisión y dashboards)
///   - Admin:           acceso a todos los modos
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,OperarioCTA,OperarioOficina")]
public class ScanController : ControllerBase
{
    private readonly IScanProcessorService _scanProcessor;
    private readonly ILogger<ScanController> _logger;

    public ScanController(IScanProcessorService scanProcessor, ILogger<ScanController> logger)
    {
        _scanProcessor = scanProcessor;
        _logger = logger;
    }

    /// <summary>
    /// Procesa un escaneo de código de barras individual.
    /// Valida que el modo solicitado sea compatible con el rol del operario.
    /// </summary>
    [HttpPost("procesar")]
    public async Task<IActionResult> ProcesarEscaneo([FromBody] ScanRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.CodigoEscaneado))
            return BadRequest(new { message = "El código escaneado es obligatorio" });

        if (!ModosEscaneo.EsValido(request.ModoOperacion))
            return BadRequest(new { message = $"Modo de operación inválido: {request.ModoOperacion}" });

        // Validar que el modo sea compatible con el rol del operario
        var rolUsuario = User.FindFirstValue(ClaimTypes.Role);
        if (!EsModoPermitidoParaRol(request.ModoOperacion, rolUsuario))
            return Forbid();

        var resultado = await _scanProcessor.ProcesarEscaneo(request);

        if (!resultado.Exito)
        {
            _logger.LogWarning("Escaneo fallido: {Codigo} · {Mensaje}",
                request.CodigoEscaneado, resultado.Mensaje);
            return UnprocessableEntity(resultado);
        }

        _logger.LogInformation("Escaneo exitoso: {Codigo} → {Estado}",
            request.CodigoEscaneado, resultado.EstadoNuevo);

        return Ok(resultado);
    }

    /// <summary>
    /// Procesa un lote de escaneos con el mismo modo de operación.
    /// Útil para operaciones masivas como carga de transporte troncal.
    /// </summary>
    [HttpPost("procesar-lote")]
    public async Task<IActionResult> ProcesarLote([FromBody] ScanBatchRequestDto request)
    {
        if (request.CodigosEscaneados.Count == 0)
            return BadRequest(new { message = "La lista de códigos no puede estar vacía" });

        if (!ModosEscaneo.EsValido(request.ModoOperacion))
            return BadRequest(new { message = $"Modo de operación inválido: {request.ModoOperacion}" });

        var rolUsuario = User.FindFirstValue(ClaimTypes.Role);
        if (!EsModoPermitidoParaRol(request.ModoOperacion, rolUsuario))
            return Forbid();

        var resultado = await _scanProcessor.ProcesarLote(request);

        _logger.LogInformation("Lote procesado: {Total} total, {Ok} exitosos, {Fail} fallidos",
            resultado.TotalEscaneados, resultado.Exitosos, resultado.Fallidos);

        return Ok(resultado);
    }

    /// <summary>
    /// Devuelve los modos de escaneo disponibles para el rol del usuario autenticado.
    /// </summary>
    [HttpGet("modos")]
    public IActionResult ObtenerModos()
    {
        var rolUsuario = User.FindFirstValue(ClaimTypes.Role);
        var todosModos = new[]
        {
            new { valor = ModosEscaneo.RecepcionOficina,    etiqueta = "Recepción en oficina",       icono = "store",          requiere = "oficina" },
            new { valor = ModosEscaneo.SalidaOficinaACta,   etiqueta = "Salida hacia CTA origen",     icono = "local_shipping", requiere = "oficina" },
            new { valor = ModosEscaneo.RecepcionCta,        etiqueta = "Recepción en CTA",            icono = "warehouse",      requiere = "cta"     },
            new { valor = ModosEscaneo.Clasificacion,       etiqueta = "Clasificación",               icono = "sort",           requiere = "cta"     },
            new { valor = ModosEscaneo.DespachoTroncal,     etiqueta = "Despacho troncal",            icono = "local_shipping", requiere = "cta"     },
            new { valor = ModosEscaneo.RecepcionTroncal,    etiqueta = "Recepción troncal",           icono = "move_to_inbox",  requiere = "cta"     },
            new { valor = ModosEscaneo.DisponibleParaReparto,etiqueta = "Disponible para reparto",    icono = "outbox",         requiere = "cta"     },
            new { valor = ModosEscaneo.EntregaOficinaDestino, etiqueta = "Entrega a oficina destino", icono = "storefront",     requiere = "oficina" },
            new { valor = ModosEscaneo.SalidaAReparto,      etiqueta = "Salida a reparto",            icono = "delivery_dining",requiere = "oficina" }
        };

        var modosFiltrados = todosModos.Where(m => EsModoPermitidoParaRol(m.valor, rolUsuario));
        return Ok(modosFiltrados);
    }

    // ─── Helpers privados ───

    private static bool EsModoPermitidoParaRol(string modo, string? rol)
    {
        if (rol == "Admin") return true;

        if (rol == "OperarioOficina")
            return ModosEscaneo.ModosOficina.Contains(modo);

        if (rol == "OperarioCTA")
            return ModosEscaneo.ModosCta.Contains(modo);

        return false;
    }
}
