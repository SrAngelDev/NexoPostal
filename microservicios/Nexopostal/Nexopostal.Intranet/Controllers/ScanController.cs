using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexopostal.Intranet.DTOs;
using Nexopostal.Intranet.Services;

namespace Nexopostal.Intranet.Controllers;

/// <summary>
/// Controlador para el procesamiento de escaneos de códigos de barras.
/// Permite a los operarios escanear paquetes y avanzar automáticamente
/// su estado en el flujo logístico.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,OperarioJefe,OperarioLogistico,OperarioOficina")]
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
    /// El sistema determina automáticamente la acción a realizar
    /// basándose en el modo de operación seleccionado.
    /// </summary>
    /// <param name="request">Datos del escaneo: código, modo, contexto</param>
    /// <returns>Resultado del procesamiento con el nuevo estado</returns>
    [HttpPost("procesar")]
    public async Task<IActionResult> ProcesarEscaneo([FromBody] ScanRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.CodigoEscaneado))
            return BadRequest(new { message = "El código escaneado es obligatorio" });

        if (!ModosEscaneo.EsValido(request.ModoOperacion))
            return BadRequest(new { message = $"Modo de operación inválido: {request.ModoOperacion}" });

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

        var resultado = await _scanProcessor.ProcesarLote(request);

        _logger.LogInformation("Lote procesado: {Total} total, {Ok} exitosos, {Fail} fallidos",
            resultado.TotalEscaneados, resultado.Exitosos, resultado.Fallidos);

        return Ok(resultado);
    }

    /// <summary>
    /// Devuelve los modos de escaneo disponibles con sus descripciones.
    /// </summary>
    [HttpGet("modos")]
    [AllowAnonymous]
    public IActionResult ObtenerModos()
    {
        var modos = new[]
        {
            new { valor = ModosEscaneo.RecepcionOficina, etiqueta = "Recepción en oficina", icono = "store", requiere = "oficina" },
            new { valor = ModosEscaneo.RecepcionCta, etiqueta = "Recepción en CTA", icono = "warehouse", requiere = "cta" },
            new { valor = ModosEscaneo.Clasificacion, etiqueta = "Clasificación", icono = "sort", requiere = "cta" },
            new { valor = ModosEscaneo.DespachoTroncal, etiqueta = "Despacho troncal", icono = "local_shipping", requiere = "cta" },
            new { valor = ModosEscaneo.RecepcionTroncal, etiqueta = "Recepción troncal", icono = "move_to_inbox", requiere = "cta" },
            new { valor = ModosEscaneo.EntregaOficinaDestino, etiqueta = "Entrega a oficina destino", icono = "storefront", requiere = "oficina" },
            new { valor = ModosEscaneo.SalidaAReparto, etiqueta = "Salida a reparto", icono = "delivery_dining", requiere = "oficina" }
        };
        return Ok(modos);
    }
}
