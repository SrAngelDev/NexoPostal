using System.ComponentModel.DataAnnotations;

namespace Nexopostal.Intranet.Models;

/// <summary>
/// Oficina postal de NexoPostal — punto físico de atención al ciudadano y nodo logístico
/// donde los repartidores recogen/depositan paquetes.
///
/// Antes vivía en <c>Data/oficinas.json</c> como dato estático. Migrada a BD para permitir
/// gestión administrativa (alta, edición, desactivación) desde la intranet.
///
/// El Id se preserva del JSON original (1001+) para no romper las referencias lógicas
/// existentes en <see cref="OperarioOficina.OficinaJsonId"/> ni en
/// <c>Repartidor.OficinaJsonId</c> del microservicio Reparto.
/// </summary>
public class OficinaPostal
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(250)]
    public string Nombre { get; set; } = string.Empty;

    [Required]
    [MaxLength(300)]
    public string Direccion { get; set; } = string.Empty;

    [Required]
    [MaxLength(10)]
    public string CodigoPostal { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Ciudad { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Provincia { get; set; }

    [MaxLength(20)]
    public string? Telefono { get; set; }

    [MaxLength(200)]
    public string? Horario { get; set; }

    [MaxLength(500)]
    public string? Servicios { get; set; }

    public double? Latitud { get; set; }
    public double? Longitud { get; set; }

    public bool Activo { get; set; } = true;

    public DateTime FechaAlta { get; set; } = DateTime.UtcNow;
    public DateTime FechaModificacion { get; set; } = DateTime.UtcNow;

    [MaxLength(450)]
    public string? ModificadoPorUserId { get; set; }
}
