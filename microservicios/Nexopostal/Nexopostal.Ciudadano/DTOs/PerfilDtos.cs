using System.ComponentModel.DataAnnotations;

namespace Nexopostal.Ciudadano.DTOs;

/// <summary>
/// Datos editables del perfil de un ciudadano dentro de su área privada.
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
/// Respuesta con la información persistida del perfil del ciudadano.
/// </summary>
public class PerfilDto
{
    /// <summary>Identificador del usuario en el módulo de autenticación.</summary>
    public string IdentityUserId { get; set; } = string.Empty;

    /// <summary>DNI asociado al perfil, cuando el cliente lo ha facilitado.</summary>
    public string? DNI { get; set; }

    /// <summary>Teléfono habitual de contacto.</summary>
    public string? Telefono { get; set; }

    /// <summary>Dirección que se propone por defecto al crear un envío.</summary>
    public string? DireccionPredeterminada { get; set; }

    /// <summary>Fecha en la que se creó el perfil de ciudadano.</summary>
    public DateTime FechaCreacion { get; set; }
}

/// <summary>
/// Datos necesarios para guardar una dirección frecuente en la agenda del cliente.
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
/// Dirección favorita ya guardada y lista para mostrarse en la agenda.
/// </summary>
public class DireccionFavoritaDto
{
    /// <summary>Identificador interno de la dirección guardada.</summary>
    public int Id { get; set; }

    /// <summary>Nombre corto que ayuda al cliente a reconocer la dirección.</summary>
    public string Alias { get; set; } = string.Empty;

    /// <summary>Persona destinataria asociada a esa dirección.</summary>
    public string NombreDestinatario { get; set; } = string.Empty;

    /// <summary>Dirección postal completa.</summary>
    public string Direccion { get; set; } = string.Empty;

    /// <summary>Código postal del destino.</summary>
    public string CodigoPostal { get; set; } = string.Empty;

    /// <summary>Ciudad del destinatario.</summary>
    public string Ciudad { get; set; } = string.Empty;

    /// <summary>Provincia del destinatario.</summary>
    public string Provincia { get; set; } = string.Empty;

    /// <summary>Teléfono opcional vinculado a la entrega.</summary>
    public string? Telefono { get; set; }
}
