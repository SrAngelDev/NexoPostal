using System.ComponentModel.DataAnnotations;

namespace Nexopostal.Ciudadano.Models;

/// <summary>
/// Representa una oficina postal de NexoPostal
/// </summary>
public class Oficina
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Nombre { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Direccion { get; set; } = string.Empty;

    [Required]
    [MaxLength(5)]
    public string CodigoPostal { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Ciudad { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Provincia { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? Telefono { get; set; }

    [MaxLength(100)]
    public string? Email { get; set; }

    [MaxLength(500)]
    public string Horario { get; set; } = "Lunes a Viernes: 9:00 - 20:00, Sábados: 9:00 - 14:00";

    public bool Activa { get; set; } = true;

    public double? Latitud { get; set; }
    public double? Longitud { get; set; }

    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
}
