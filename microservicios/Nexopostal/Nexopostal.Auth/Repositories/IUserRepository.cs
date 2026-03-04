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
}
