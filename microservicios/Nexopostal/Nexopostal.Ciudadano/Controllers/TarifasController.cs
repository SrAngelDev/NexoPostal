using Microsoft.AspNetCore.Mvc;
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

    public TarifasController(ILogger<TarifasController> logger)
    {
        _logger = logger;
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
        [FromQuery] string? tipoServicio = "estandar",
        [FromQuery] decimal? peso = null)
    {
        try
        {
            var tarifas = new TarifasResponseDto
            {
                TipoServicio = tipoServicio ?? "estandar",
                Tarifas = new List<TarifaDetalleDto>
                {
                    new TarifaDetalleDto
                    {
                        Id = 1,
                        Nombre = "Estándar",
                        Descripcion = "Entrega en 3-5 días laborables",
                        PrecioBase = 5.00m,
                        PrecioPorKg = 2.00m,
                        TiempoEntregaDias = "3-5",
                        Activa = true
                    },
                    new TarifaDetalleDto
                    {
                        Id = 2,
                        Nombre = "Express",
                        Descripcion = "Entrega en 24-48 horas",
                        PrecioBase = 8.00m,
                        PrecioPorKg = 3.50m,
                        TiempoEntregaDias = "1-2",
                        Activa = true
                    },
                    new TarifaDetalleDto
                    {
                        Id = 3,
                        Nombre = "Urgente",
                        Descripcion = "Entrega en menos de 24 horas",
                        PrecioBase = 15.00m,
                        PrecioPorKg = 5.00m,
                        TiempoEntregaDias = "<1",
                        Activa = true
                    }
                }
            };

            // Si se proporciona peso, calcular precio estimado para cada tarifa
            if (peso.HasValue && peso.Value > 0)
            {
                foreach (var tarifa in tarifas.Tarifas)
                {
                    tarifa.PrecioEstimado = Math.Round(
                        tarifa.PrecioBase + (tarifa.PrecioPorKg * peso.Value), 
                        2
                    );
                }
            }

            // Filtrar por tipo de servicio si se especifica
            if (!string.IsNullOrEmpty(tipoServicio) && tipoServicio.ToLower() != "todos")
            {
                tarifas.Tarifas = tarifas.Tarifas
                    .Where(t => t.Nombre.ToLower().Contains(tipoServicio.ToLower()))
                    .ToList();
            }

            _logger.LogInformation("Consulta de tarifas: Tipo={TipoServicio}, Peso={Peso}kg, Resultados={Count}",
                tipoServicio, peso, tarifas.Tarifas.Count);

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
            decimal precioBase;
            decimal precioPorKg;
            int diasEntrega;

            // Determinar tarifas según tipo de servicio
            switch (request.TipoServicio?.ToLower())
            {
                case "express":
                    precioBase = 8.00m;
                    precioPorKg = 3.50m;
                    diasEntrega = 2;
                    break;
                case "urgente":
                    precioBase = 15.00m;
                    precioPorKg = 5.00m;
                    diasEntrega = 1;
                    break;
                default: // estándar
                    precioBase = 5.00m;
                    precioPorKg = 2.00m;
                    diasEntrega = 4;
                    break;
            }

            decimal precioTotal = precioBase + (precioPorKg * request.Peso);

            // Recargo por dimensiones grandes (opcional)
            if (request.Alto.HasValue && request.Ancho.HasValue && request.Largo.HasValue)
            {
                var volumen = request.Alto.Value * request.Ancho.Value * request.Largo.Value;
                if (volumen > 100000) // cm³ (ejemplo: 50x40x50)
                {
                    precioTotal += 3.00m; // Recargo por volumen
                }
            }

            var resultado = new CalculoPrecioDto
            {
                PrecioBase = precioBase,
                PrecioPorPeso = Math.Round(precioPorKg * request.Peso, 2),
                RecargoVolumen = precioTotal - (precioBase + (precioPorKg * request.Peso)),
                PrecioTotal = Math.Round(precioTotal, 2),
                Moneda = "EUR",
                TiempoEstimadoDias = diasEntrega,
                TipoServicio = request.TipoServicio ?? "estandar"
            };

            _logger.LogInformation("Cálculo de precio: {Peso}kg, Servicio={Servicio}, Total={Total}€",
                request.Peso, request.TipoServicio, resultado.PrecioTotal);

            return Ok(resultado);
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
    public List<TarifaDetalleDto> Tarifas { get; set; } = new();
}

public class TarifaDetalleDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public decimal PrecioBase { get; set; }
    public decimal PrecioPorKg { get; set; }
    public string TiempoEntregaDias { get; set; } = string.Empty;
    public bool Activa { get; set; }
    public decimal? PrecioEstimado { get; set; }
}

public class CalcularPrecioRequestDto
{
    [Required(ErrorMessage = "El peso es requerido")]
    [Range(0.1, 150, ErrorMessage = "El peso debe estar entre 0.1 y 150 kg")]
    public decimal Peso { get; set; }

    public string? TipoServicio { get; set; } = "estandar";
    
    public decimal? Alto { get; set; }
    public decimal? Ancho { get; set; }
    public decimal? Largo { get; set; }
}

public class CalculoPrecioDto
{
    public decimal PrecioBase { get; set; }
    public decimal PrecioPorPeso { get; set; }
    public decimal RecargoVolumen { get; set; }
    public decimal PrecioTotal { get; set; }
    public string Moneda { get; set; } = "EUR";
    public int TiempoEstimadoDias { get; set; }
    public string TipoServicio { get; set; } = string.Empty;
}
