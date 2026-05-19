using NexoPostal.Auth.DTOs;
using NexoPostal.Auth.Models;
using NexoPostal.Auth.Repositories;
using System.Globalization;

namespace NexoPostal.Auth.Services;

/// <summary>
/// Interfaz del servicio de autenticación.
/// </summary>
public interface IAuthService
{
    Task<TokenResponseDto?> LoginAsync(LoginDto dto);
    Task<(TokenResponseDto? Token, IEnumerable<string>? Errors)> RegisterAsync(RegisterDto dto);
    Task<TokenResponseDto?> RefreshTokenAsync(RefreshTokenRequestDto dto);
    Task<UsuarioInfoDto?> GetUserInfoAsync(string userId);
    Task<(UsuarioInfoDto? User, string? Error)> UpdateProfileAsync(string userId, ActualizarUsuarioDto dto);
    Task<(bool Success, string? Error)> ChangePasswordAsync(string userId, CambiarPasswordDto dto);
    Task SolicitarResetPasswordAsync(string email, string frontendUrl);
    Task<(bool Success, string? Error)> ResetPasswordAsync(ResetPasswordDto dto);
}

/// <summary>
/// Servicio de autenticación que encapsula toda la lógica de negocio de auth.
/// Inyecta IUserRepository para acceso a datos (patrón repositorio).
/// </summary>
public class AuthService : IAuthService
{
    private const string TokenProvider = "NexoPostal";
    private const string RefreshTokenHashName = "RefreshTokenHash";
    private const string RefreshTokenExpiryName = "RefreshTokenExpiryUtc";

    private readonly IUserRepository _userRepository;
    private readonly TokenService _tokenService;
    private readonly IEmailService _emailService;

    public AuthService(IUserRepository userRepository, TokenService tokenService, IEmailService emailService)
    {
        _userRepository = userRepository;
        _tokenService = tokenService;
        _emailService = emailService;
    }

    public async Task<TokenResponseDto?> LoginAsync(LoginDto dto)
    {
        var user = await _userRepository.GetByEmailAsync(dto.Email);
        if (user == null || !await _userRepository.CheckPasswordAsync(user, dto.Password))
            return null;

        if (await _userRepository.IsLockedOutAsync(user))
            return null;

        return await EmitTokenPairAsync(user);
    }

    public async Task<(TokenResponseDto? Token, IEnumerable<string>? Errors)> RegisterAsync(RegisterDto dto)
    {
        var user = new ApplicationUser
        {
            UserName = dto.Email,
            Email = dto.Email,
            NombreCompleto = dto.NombreCompleto,
            Rol = Rol.Cliente
        };

        var result = await _userRepository.CreateAsync(user, dto.Password);

        if (!result.Succeeded)
            return (null, result.Errors.Select(e => e.Description));

        var token = await EmitTokenPairAsync(user);
        return (token, null);
    }

    public async Task<TokenResponseDto?> RefreshTokenAsync(RefreshTokenRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.RefreshToken))
            return null;

        if (!_tokenService.TryExtractUserId(dto.RefreshToken, out var userId))
            return null;

        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
            return null;

        var storedHash = await _userRepository.GetUserTokenAsync(user, TokenProvider, RefreshTokenHashName);
        var storedExpiry = await _userRepository.GetUserTokenAsync(user, TokenProvider, RefreshTokenExpiryName);

        if (string.IsNullOrWhiteSpace(storedHash) || string.IsNullOrWhiteSpace(storedExpiry))
            return null;

        if (!DateTime.TryParse(
                storedExpiry,
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out var expiryUtc))
        {
            await RevokeRefreshTokenAsync(user);
            return null;
        }

        if (expiryUtc <= DateTime.UtcNow)
        {
            await RevokeRefreshTokenAsync(user);
            return null;
        }

        var incomingHash = _tokenService.HashToken(dto.RefreshToken);
        if (!_tokenService.SecureEquals(incomingHash, storedHash))
            return null;

        return await EmitTokenPairAsync(user);
    }

    public async Task<UsuarioInfoDto?> GetUserInfoAsync(string userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null) return null;

        return new UsuarioInfoDto
        {
            Id = user.Id,
            Email = user.Email ?? "",
            NombreCompleto = user.NombreCompleto,
            PhoneNumber = user.PhoneNumber,
            FechaRegistro = user.FechaRegistro,
            Rol = user.Rol.ToString()
        };
    }

    public async Task<(UsuarioInfoDto? User, string? Error)> UpdateProfileAsync(string userId, ActualizarUsuarioDto dto)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null) return (null, "Usuario no encontrado");

        user.NombreCompleto = dto.NombreCompleto;
        user.PhoneNumber = dto.PhoneNumber;

        // Si cambia el email, verificar que no esté en uso
        if (!string.Equals(user.Email, dto.Email, StringComparison.OrdinalIgnoreCase))
        {
            var emailExists = await _userRepository.GetByEmailAsync(dto.Email);
            if (emailExists != null && emailExists.Id != userId)
                return (null, "El email ya está en uso por otro usuario");

            user.Email = dto.Email;
            user.UserName = dto.Email;
        }

        var result = await _userRepository.UpdateAsync(user);
        if (!result.Succeeded)
            return (null, result.Errors.FirstOrDefault()?.Description ?? "Error al actualizar");

        return (new UsuarioInfoDto
        {
            Id = user.Id,
            Email = user.Email ?? "",
            NombreCompleto = user.NombreCompleto,
            PhoneNumber = user.PhoneNumber,
            FechaRegistro = user.FechaRegistro,
            Rol = user.Rol.ToString()
        }, null);
    }

    public async Task<(bool Success, string? Error)> ChangePasswordAsync(string userId, CambiarPasswordDto dto)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null) return (false, "Usuario no encontrado");

        var result = await _userRepository.ChangePasswordAsync(user, dto.PasswordActual, dto.NuevaPassword);
        if (!result.Succeeded)
            return (false, result.Errors.FirstOrDefault()?.Description ?? "Error al cambiar la contraseña");

        await RevokeRefreshTokenAsync(user);

        return (true, null);
    }

    private async Task<TokenResponseDto> EmitTokenPairAsync(ApplicationUser user)
    {
        var (accessToken, accessExpirationUtc) = _tokenService.GenerateAccessToken(user);

        var refreshToken = _tokenService.GenerateRefreshToken(user.Id);
        var refreshTokenHash = _tokenService.HashToken(refreshToken);
        var refreshExpirationUtc = DateTime.UtcNow.AddDays(_tokenService.GetRefreshTokenExpiryDays());

        await _userRepository.SetUserTokenAsync(user, TokenProvider, RefreshTokenHashName, refreshTokenHash);
        await _userRepository.SetUserTokenAsync(
            user,
            TokenProvider,
            RefreshTokenExpiryName,
            refreshExpirationUtc.ToString("O", CultureInfo.InvariantCulture));

        return new TokenResponseDto
        {
            Token = accessToken,
            Expiration = accessExpirationUtc,
            RefreshToken = refreshToken,
            RefreshTokenExpiration = refreshExpirationUtc,
            User = user.NombreCompleto,
            Rol = user.Rol.ToString()
        };
    }

    private async Task RevokeRefreshTokenAsync(ApplicationUser user)
    {
        await _userRepository.RemoveUserTokenAsync(user, TokenProvider, RefreshTokenHashName);
        await _userRepository.RemoveUserTokenAsync(user, TokenProvider, RefreshTokenExpiryName);
    }

    public async Task SolicitarResetPasswordAsync(string email, string frontendUrl)
    {
        var user = await _userRepository.GetByEmailAsync(email);
        // Si el usuario no existe, respondemos igual para no revelar si el email está registrado
        if (user == null) return;

        var token = await _userRepository.GeneratePasswordResetTokenAsync(user);
        var encodedToken = Uri.EscapeDataString(token);
        var encodedEmail = Uri.EscapeDataString(email);
        var resetLink = $"{frontendUrl.TrimEnd('/')}/reset-password?email={encodedEmail}&token={encodedToken}";

        await _emailService.SendPasswordResetEmailAsync(user.Email!, user.NombreCompleto, resetLink);
    }

    public async Task<(bool Success, string? Error)> ResetPasswordAsync(ResetPasswordDto dto)
    {
        var user = await _userRepository.GetByEmailAsync(dto.Email);
        if (user == null)
            return (false, "El enlace de recuperación no es válido o ha expirado.");

        var result = await _userRepository.ResetPasswordAsync(user, dto.Token, dto.NuevaPassword);
        if (!result.Succeeded)
        {
            var error = result.Errors.FirstOrDefault()?.Description
                        ?? "Error al restablecer la contraseña.";
            return (false, error);
        }

        // Revocar refresh tokens existentes por seguridad
        await RevokeRefreshTokenAsync(user);

        return (true, null);
    }
}
