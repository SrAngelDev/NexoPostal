using Microsoft.AspNetCore.Identity;
using NexoPostal.Auth.Models;

namespace NexoPostal.Auth.Repositories;

/// <summary>
/// Implementación del repositorio de usuarios.
/// Encapsula UserManager de Identity para mayor cohesión y menor acoplamiento.
/// </summary>
public class UserRepository : IUserRepository
{
    private readonly UserManager<ApplicationUser> _userManager;

    public UserRepository(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    /// <inheritdoc />
    public async Task<ApplicationUser?> GetByEmailAsync(string email)
    {
        return await _userManager.FindByEmailAsync(email);
    }

    /// <inheritdoc />
    public async Task<ApplicationUser?> GetByIdAsync(string userId)
    {
        return await _userManager.FindByIdAsync(userId);
    }

    /// <inheritdoc />
    public async Task<bool> CheckPasswordAsync(ApplicationUser user, string password)
    {
        return await _userManager.CheckPasswordAsync(user, password);
    }

    /// <inheritdoc />
    public async Task<IdentityResult> CreateAsync(ApplicationUser user, string password)
    {
        return await _userManager.CreateAsync(user, password);
    }

    /// <inheritdoc />
    public async Task<IdentityResult> UpdateAsync(ApplicationUser user)
    {
        return await _userManager.UpdateAsync(user);
    }

    /// <inheritdoc />
    public async Task<IdentityResult> ChangePasswordAsync(ApplicationUser user, string currentPassword, string newPassword)
    {
        return await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
    }

    /// <inheritdoc />
    public async Task SetUserTokenAsync(ApplicationUser user, string loginProvider, string tokenName, string tokenValue)
    {
        await _userManager.SetAuthenticationTokenAsync(user, loginProvider, tokenName, tokenValue);
    }

    /// <inheritdoc />
    public async Task<string?> GetUserTokenAsync(ApplicationUser user, string loginProvider, string tokenName)
    {
        return await _userManager.GetAuthenticationTokenAsync(user, loginProvider, tokenName);
    }

    /// <inheritdoc />
    public async Task RemoveUserTokenAsync(ApplicationUser user, string loginProvider, string tokenName)
    {
        await _userManager.RemoveAuthenticationTokenAsync(user, loginProvider, tokenName);
    }

    /// <inheritdoc />
    public async Task<string> GeneratePasswordResetTokenAsync(ApplicationUser user)
    {
        return await _userManager.GeneratePasswordResetTokenAsync(user);
    }

    /// <inheritdoc />
    public async Task<IdentityResult> ResetPasswordAsync(ApplicationUser user, string token, string newPassword)
    {
        return await _userManager.ResetPasswordAsync(user, token, newPassword);
    }
}
