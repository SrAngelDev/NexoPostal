using System.ComponentModel.DataAnnotations;

namespace Nexopostal.Ciudadano.Models;

/// <summary>
/// Dirección guardada en la agenda del usuario
/// </summary>
public class DireccionFavorita
{
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// ID del perfil al que pertenece esta dirección
    /// </summary>
    public int ClientePerfilId { get; set; }

    /// <summary>
    /// Alias de la dirección (ej: "Casa", "Trabajo", "Casa de mi madre")
    /// </summary>
    [MaxLength(100)]
    public string Alias { get; set; } = string.Empty;

    /// <summary>
    /// Nombre del destinatario
    /// </summary>
    [MaxLength(200)]
    public string NombreDestinatario { get; set; } = string.Empty;

    /// <summary>
    /// Dirección completa
    /// </summary>
    [MaxLength(500)]
    public string Direccion { get; set; } = string.Empty;

    /// <summary>
    /// Código postal
    /// </summary>
    [MaxLength(10)]
    public string CodigoPostal { get; set; } = string.Empty;

    /// <summary>
    /// Ciudad
    /// </summary>
    [MaxLength(100)]
    public string Ciudad { get; set; } = string.Empty;

    /// <summary>
    /// Provincia
    /// </summary>
    [MaxLength(100)]
    public string Provincia { get; set; } = string.Empty;

    /// <summary>
    /// Teléfono del destinatario
    /// </summary>
    [MaxLength(20)]
    public string? Telefono { get; set; }

    /// <summary>
    /// Perfil al que pertenece (navegación)
    /// </summary>
    public ClientePerfil? ClientePerfil { get; set; }
}
