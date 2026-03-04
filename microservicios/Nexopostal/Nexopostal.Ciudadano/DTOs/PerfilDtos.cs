using System.ComponentModel.DataAnnotations;

namespace Nexopostal.Ciudadano.DTOs;

/// <summary>
/// DTO para crear o actualizar el perfil del ciudadano
/// </summary>
public class ActualizarPerfilDto
{
    [MaxLength(15)]
    public string? DNI { get; set; }

    [MaxLength(20)]
    [Phone(ErrorMessage = "Formato de teléfono inválido")]
    public string? Telefono { get; set; }

    [MaxLength(500)]
    public string? DireccionPredeterminada { get; set; }
}

/// <summary>
/// DTO de respuesta con datos del perfil
/// </summary>
public class PerfilDto
{
    public string IdentityUserId { get; set; } = string.Empty;
    public string? DNI { get; set; }
    public string? Telefono { get; set; }
    public string? DireccionPredeterminada { get; set; }
    public DateTime FechaCreacion { get; set; }
}

/// <summary>
/// DTO para agregar una dirección favorita a la agenda
/// </summary>
public class CrearDireccionFavoritaDto
{
    [Required]
    [MaxLength(100)]
    public string Alias { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string NombreDestinatario { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string Direccion { get; set; } = string.Empty;

    [Required]
    [MaxLength(10)]
    [RegularExpression(@"^\d{5}$", ErrorMessage = "El código postal debe tener exactamente 5 dígitos numéricos")]
    public string CodigoPostal { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Ciudad { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Provincia { get; set; } = string.Empty;

    [MaxLength(20)]
    [Phone]
    public string? Telefono { get; set; }
}

/// <summary>
/// DTO de respuesta con datos de una dirección favorita
/// </summary>
public class DireccionFavoritaDto
{
    public int Id { get; set; }
    public string Alias { get; set; } = string.Empty;
    public string NombreDestinatario { get; set; } = string.Empty;
    public string Direccion { get; set; } = string.Empty;
    public string CodigoPostal { get; set; } = string.Empty;
    public string Ciudad { get; set; } = string.Empty;
    public string Provincia { get; set; } = string.Empty;
    public string? Telefono { get; set; }
}
