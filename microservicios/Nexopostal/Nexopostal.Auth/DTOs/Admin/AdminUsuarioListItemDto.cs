namespace NexoPostal.Auth.DTOs.Admin;

/// <summary>
/// Resumen de usuario para los listados administrativos de empleados y cuentas.
/// Recoge la información que el panel necesita para mostrar estado y permisos.
/// </summary>
public class AdminUsuarioListItemDto
{
    /// <summary>Identificador interno del usuario en Identity.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Nombre completo que verá el personal de administración.</summary>
    public string NombreCompleto { get; set; } = string.Empty;

    /// <summary>Correo principal con el que la cuenta accede al sistema.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Código interno de empleado, si la cuenta pertenece a personal.</summary>
    public string? CodigoEmpleado { get; set; }

    /// <summary>Teléfono de contacto asociado a la cuenta.</summary>
    public string? PhoneNumber { get; set; }

    /// <summary>Rol operativo actual en formato legible para el frontend.</summary>
    public string Rol { get; set; } = string.Empty;

    /// <summary>Momento en que se dio de alta la cuenta.</summary>
    public DateTime FechaRegistro { get; set; }

    /// <summary>Indica si la cuenta está bloqueada y no puede iniciar sesión.</summary>
    public bool Bloqueado { get; set; }

    /// <summary>Marca lógica de borrado para ocultar usuarios sin perder trazabilidad.</summary>
    public bool Eliminado { get; set; }

    /// <summary>Fecha de borrado lógico, cuando exista.</summary>
    public DateTime? EliminadoEnUtc { get; set; }
}
