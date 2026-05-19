using Microsoft.AspNetCore.Identity;
using NexoPostal.Auth.Models;

namespace NexoPostal.Auth.Repositories;

/// <summary>
/// Repositorio para la gestión de usuarios del sistema.
/// Encapsula toda la lógica de acceso a datos de Identity (UserManager).
/// </summary>
public interface IUserRepository
{
    /// <summary>Busca un usuario por email</summary>
    Task<ApplicationUser?> GetByEmailAsync(string email);

    /// <summary>Busca un usuario por ID</summary>
    Task<ApplicationUser?> GetByIdAsync(string userId);

    /// <summary>Verifica la contraseña de un usuario</summary>
    Task<bool> CheckPasswordAsync(ApplicationUser user, string password);

    /// <summary>Crea un nuevo usuario con contraseña</summary>
    Task<IdentityResult> CreateAsync(ApplicationUser user, string password);

    /// <summary>Actualiza los datos de un usuario</summary>
    Task<IdentityResult> UpdateAsync(ApplicationUser user);

    /// <summary>Cambia la contraseña de un usuario</summary>
    Task<IdentityResult> ChangePasswordAsync(ApplicationUser user, string currentPassword, string newPassword);

    /// <summary>Guarda un token asociado al usuario (Identity token store)</summary>
    Task SetUserTokenAsync(ApplicationUser user, string loginProvider, string tokenName, string tokenValue);

    /// <summary>Obtiene un token asociado al usuario</summary>
    Task<string?> GetUserTokenAsync(ApplicationUser user, string loginProvider, string tokenName);

    /// <summary>Elimina un token asociado al usuario</summary>
    Task RemoveUserTokenAsync(ApplicationUser user, string loginProvider, string tokenName);

    /// <summary>Genera un token de restablecimiento de contraseña firmado por Identity</summary>
    Task<string> GeneratePasswordResetTokenAsync(ApplicationUser user);

    /// <summary>Restablece la contraseña usando el token generado por Identity</summary>
    Task<IdentityResult> ResetPasswordAsync(ApplicationUser user, string token, string newPassword);

    // ─── Gestión de usuarios (Admin) ───

    /// <summary>Lista todos los usuarios, con filtros opcionales por rol y estado de bloqueo.</summary>
    Task<List<ApplicationUser>> GetAllAsync(NexoPostal.Auth.Models.Rol? rol, bool? bloqueado);

    /// <summary>Comprueba si el usuario tiene el acceso bloqueado (lockout activo).</summary>
    Task<bool> IsLockedOutAsync(ApplicationUser user);

    /// <summary>Bloquea o desbloquea el acceso de un usuario mediante Identity lockout.</summary>
    Task<IdentityResult> SetLockoutAsync(ApplicationUser user, bool bloquear);

    /// <summary>Restablece la contraseña de un usuario directamente (flujo admin, sin token previo).</summary>
    Task<IdentityResult> AdminResetPasswordAsync(ApplicationUser user, string newPassword);
}
