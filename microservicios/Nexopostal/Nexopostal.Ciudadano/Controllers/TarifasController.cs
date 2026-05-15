using Microsoft.AspNetCore.Mvc;
using Nexopostal.Ciudadano.Services;
using System.ComponentModel.DataAnnotations;

namespace Nexopostal.Ciudadano.Controllers;

/// <summary>
/// Controlador para la consulta de tarifas y precios
/// </summary>
[Route("api/[controller]")]
[ApiController]
public class TarifasController : ControllerBase
{
    private readonly ILogger<TarifasController> _logger;
    private readonly ITarifasService _tarifasService;

    public TarifasController(ILogger<TarifasController> logger, ITarifasService tarifasService)
    {
        _logger = logger;
        _tarifasService = tarifasService;
    }

    /// <summary>
    /// Consulta las tarifas disponibles según el tipo de servicio
    /// </summary>
    /// <param name="tipoServicio">Tipo de servicio: estandar, express, urgente</param>
    /// <param name="peso">Peso del paquete en kg (opcional)</param>
    /// <returns>Tarifas disponibles</returns>
    [HttpGet("consultar")]
    [ProducesResponseType(typeof(TarifasResponseDto), StatusCodes.Status200OK)]
    public IActionResult ConsultarTarifas(
        [FromQuery] string? tipoServicio = "todos",
        [FromQuery] decimal? peso = null,
        [FromQuery] decimal? largo = null,
        [FromQuery] decimal? ancho = null,
        [FromQuery] decimal? alto = null,
        [FromQuery] string? codigoPostalOrigen = null,
        [FromQuery] string? codigoPostalDestino = null)
    {
        try
        {
            var resultado = _tarifasService.Consultar(new TarifaConsultaInput(
                peso ?? 0.1m,
                largo,
                ancho,
                alto,
                codigoPostalOrigen,
                codigoPostalDestino));

            var tarifas = new TarifasResponseDto
            {
                TipoServicio = tipoServicio ?? "todos",
                Zona = resultado.Zona,
                PesoReal = resultado.PesoReal,
                PesoVolumetrico = resultado.PesoVolumetrico,
                PesoFacturable = resultado.PesoFacturable,
                AplicaRecargo = resultado.AplicaRecargo,
                RecargoPorcentaje = resultado.RecargoPorcentaje,
                Tarifas = resultado.Tarifas.Select((tarifa, index) => new TarifaDetalleDto
                {
                    Id = index + 1,
                    Nombre = tarifa.Nombre,
                    Descripcion = tarifa.Descripcion,
                    TiempoEntregaEstimado = tarifa.TiempoEntregaEstimado,
                    TiempoEstimadoDias = tarifa.TiempoEstimadoDias,
                    PrecioBase = tarifa.PrecioBase,
                    Recargo = tarifa.Recargo,
                    PrecioTotal = tarifa.PrecioTotal,
                    Activa = true,
                    PrecioEstimado = tarifa.PrecioTotal
                }).ToList()
            };

            if (!string.IsNullOrWhiteSpace(tipoServicio) && !tipoServicio.Equals("todos", StringComparison.OrdinalIgnoreCase))
            {
                tarifas.Tarifas = tarifas.Tarifas
                    .Where(t => t.Nombre.Contains(tipoServicio, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            _logger.LogInformation(
                "Consulta de tarifas: Tipo={TipoServicio}, Zona={Zona}, Peso={Peso}kg, Resultados={Count}",
                tipoServicio,
                resultado.Zona,
                resultado.PesoFacturable,
                tarifas.Tarifas.Count);

            return Ok(tarifas);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al consultar tarifas");
            return StatusCode(500, new { error = "Error al consultar tarifas", details = ex.Message });
        }
    }

    /// <summary>
    /// Calcula el precio estimado para un envío específico
    /// </summary>
    /// <param name="request">Parámetros del envío</param>
    /// <returns>Precio calculado</returns>
    [HttpPost("calcular")]
    [ProducesResponseType(typeof(CalculoPrecioDto), StatusCodes.Status200OK)]
    public IActionResult CalcularPrecio([FromBody] CalcularPrecioRequestDto request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var resultado = _tarifasService.Calcular(new TarifaCalculoInput(
                request.Peso,
                request.Largo,
                request.Ancho,
                request.Alto,
                request.CodigoPostalOrigen,
                request.CodigoPostalDestino,
                request.TipoTarifa));

            var dto = new CalculoPrecioDto
            {
                PrecioBase = resultado.PrecioBase,
                Recargo = resultado.Recargo,
                PrecioTotal = resultado.PrecioTotal,
                Moneda = "EUR",
                TiempoEntregaEstimado = resultado.TiempoEntregaEstimado,
                TiempoEstimadoDias = resultado.TiempoEstimadoDias,
                TipoTarifa = resultado.TipoTarifa,
                Zona = resultado.Zona,
                PesoFacturable = resultado.PesoFacturable,
                PesoVolumetrico = resultado.PesoVolumetrico,
                AplicaRecargo = resultado.AplicaRecargo,
                RecargoPorcentaje = resultado.RecargoPorcentaje
            };

            _logger.LogInformation(
                "Cálculo de precio: {Peso}kg, Tarifa={Tarifa}, Zona={Zona}, Total={Total}€",
                resultado.PesoFacturable,
                resultado.TipoTarifa,
                resultado.Zona,
                resultado.PrecioTotal);

            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al calcular precio");
            return StatusCode(500, new { error = "Error al calcular precio", details = ex.Message });
        }
    }
}

// === DTOs para Tarifas ===

public class TarifasResponseDto
{
    public string TipoServicio { get; set; } = string.Empty;
    public string Zona { get; set; } = string.Empty;
    public decimal PesoReal { get; set; }
    public decimal PesoVolumetrico { get; set; }
    public decimal PesoFacturable { get; set; }
    public bool AplicaRecargo { get; set; }
    public decimal RecargoPorcentaje { get; set; }
    public List<TarifaDetalleDto> Tarifas { get; set; } = new();
}

public class TarifaDetalleDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public string TiempoEntregaEstimado { get; set; } = string.Empty;
    public int TiempoEstimadoDias { get; set; }
    public decimal PrecioBase { get; set; }
    public decimal Recargo { get; set; }
    public decimal PrecioTotal { get; set; }
    public bool Activa { get; set; }
    public decimal? PrecioEstimado { get; set; }
}

public class CalcularPrecioRequestDto
{
    [Required(ErrorMessage = "El peso es requerido")]
    [Range(0.1, 30, ErrorMessage = "El peso debe estar entre 0.1 y 30 kg")]
    public decimal Peso { get; set; }
    
    public decimal? Alto { get; set; }
    public decimal? Ancho { get; set; }
    public decimal? Largo { get; set; }

    [Required(ErrorMessage = "El código postal de origen es requerido")]
    [MaxLength(10)]
    public string CodigoPostalOrigen { get; set; } = string.Empty;

    [Required(ErrorMessage = "El código postal de destino es requerido")]
    [MaxLength(10)]
    public string CodigoPostalDestino { get; set; } = string.Empty;

    public string? TipoTarifa { get; set; } = "Estandar";
}

public class CalculoPrecioDto
{
    public decimal PrecioBase { get; set; }
    public decimal Recargo { get; set; }
    public decimal PrecioTotal { get; set; }
    public string Moneda { get; set; } = "EUR";
    public string TiempoEntregaEstimado { get; set; } = string.Empty;
    public int TiempoEstimadoDias { get; set; }
    public string TipoTarifa { get; set; } = string.Empty;
    public string Zona { get; set; } = string.Empty;
    public decimal PesoFacturable { get; set; }
    public decimal PesoVolumetrico { get; set; }
    public bool AplicaRecargo { get; set; }
    public decimal RecargoPorcentaje { get; set; }
}
