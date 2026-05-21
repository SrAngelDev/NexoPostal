namespace NexoPostal.Auth.DTOs.Admin;

public class AdminUsuarioListItemDto
{
    public string Id { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? CodigoEmpleado { get; set; }
    public string? PhoneNumber { get; set; }
    public string Rol { get; set; } = string.Empty;
    public DateTime FechaRegistro { get; set; }
    public bool Bloqueado { get; set; }
    public bool Eliminado { get; set; }
    public DateTime? EliminadoEnUtc { get; set; }
}
