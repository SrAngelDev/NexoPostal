using System.ComponentModel.DataAnnotations;

namespace Nexopostal.Ciudadano.Models;

/// <summary>
/// Perfil del ciudadano registrado (datos adicionales al Identity)
/// </summary>
public class ClientePerfil
{
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// ID del usuario en la base de datos de Identity (AspNetUsers)
    /// Este es el vínculo entre ambos microservicios
    /// </summary>
    [Required]
    [MaxLength(450)]
    public string IdentityUserId { get; set; } = string.Empty;

    /// <summary>
    /// DNI/NIE del ciudadano
    /// </summary>
    [MaxLength(15)]
    public string? DNI { get; set; }

    /// <summary>
    /// Teléfono de contacto
    /// </summary>
    [MaxLength(20)]
    public string? Telefono { get; set; }

    /// <summary>
    /// Dirección predeterminada del usuario
    /// </summary>
    [MaxLength(500)]
    public string? DireccionPredeterminada { get; set; }

    /// <summary>
    /// Fecha de creación del perfil
    /// </summary>
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Agenda de direcciones favoritas del usuario
    /// </summary>
    public ICollection<DireccionFavorita> Agenda { get; set; } = new List<DireccionFavorita>();
}
