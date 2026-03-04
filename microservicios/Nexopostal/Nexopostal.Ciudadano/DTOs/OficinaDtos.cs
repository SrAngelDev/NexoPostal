using System.ComponentModel.DataAnnotations;

namespace Nexopostal.Ciudadano.DTOs;

/// <summary>
/// DTO para buscar oficinas
/// </summary>
public class BuscarOficinaDto
{
    /// <summary>
    /// Código postal para buscar oficinas cercanas
    /// </summary>
    [Required(ErrorMessage = "El código postal es requerido")]
    [StringLength(5, MinimumLength = 5, ErrorMessage = "El código postal debe tener 5 dígitos")]
    public string CodigoPostal { get; set; } = string.Empty;

    /// <summary>
    /// Radio de búsqueda en kilómetros (opcional)
    /// </summary>
    [Range(1, 50, ErrorMessage = "El radio debe estar entre 1 y 50 km")]
    public int? RadioKm { get; set; }
}

/// <summary>
/// DTO con la información de una oficina
/// </summary>
public class OficinaDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Direccion { get; set; } = string.Empty;
    public string CodigoPostal { get; set; } = string.Empty;
    public string Ciudad { get; set; } = string.Empty;
    public string Provincia { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public string? Email { get; set; }
    public string Horario { get; set; } = string.Empty;
    public string Servicios { get; set; } = string.Empty;
    public bool Activa { get; set; }
    public double? DistanciaKm { get; set; }
    public double? Latitud { get; set; }
    public double? Longitud { get; set; }
}
