using Microsoft.AspNetCore.Mvc;
using Nexopostal.Ciudadano.Repositories;
using Nexopostal.Ciudadano.Services;

namespace Nexopostal.Ciudadano.Controllers;

/// <summary>
/// Controlador para la generación de etiquetas de envío (endpoint público por tracking)
/// Usa QuestPDF para generar etiquetas reales en PDF
/// </summary>
[Route("api/[controller]")]
[ApiController]
public class EtiquetasController : ControllerBase
{
    private readonly IEnvioRepository _envioRepo;
    private readonly IEtiquetaPdfService _etiquetaPdfService;
    private readonly ILogger<EtiquetasController> _logger;

    public EtiquetasController(
        IEnvioRepository envioRepo,
        IEtiquetaPdfService etiquetaPdfService,
        ILogger<EtiquetasController> logger)
    {
        _envioRepo = envioRepo;
        _etiquetaPdfService = etiquetaPdfService;
        _logger = logger;
    }

    /// <summary>
    /// Descarga la etiqueta de un envío en formato PDF
    /// </summary>
    /// <param name="numero">Número de seguimiento del envío</param>
    /// <returns>Archivo PDF con la etiqueta</returns>
    [HttpGet("{numero}")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DescargarEtiqueta(string numero)
    {
        var envio = await _envioRepo.GetByTrackingAsync(numero);

        if (envio == null)
        {
            _logger.LogWarning("Intento de descargar etiqueta de envío inexistente: {Numero}", numero);
            return NotFound(new { mensaje = "Envío no encontrado" });
        }

        var pdfBytes = _etiquetaPdfService.GenerarEtiqueta(envio);
        _logger.LogInformation("Etiqueta descargada: {NumeroSeguimiento}", numero);

        return File(pdfBytes, "application/pdf", $"Etiqueta_{numero}.pdf");
    }
}
