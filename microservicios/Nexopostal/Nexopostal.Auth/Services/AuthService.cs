using System.Globalization;
using CSharpFunctionalExtensions;
using Microsoft.Extensions.Logging;
using Nexopostal.Shared.Errors;
using NexoPostal.Auth.DTOs;
using NexoPostal.Auth.Errors;
using NexoPostal.Auth.Mappers;
using NexoPostal.Auth.Models;
using NexoPostal.Auth.Repositories;

namespace NexoPostal.Auth.Services;

/// <summary>
/// Servicio de autenticación. Todas las operaciones devuelven <see cref="Result{T,DomainError}"/>
/// o <see cref="UnitResult{DomainError}"/> siguiendo el patrón Railway Oriented Programming.
/// </summary>
public interface IAuthService
{
    Task<Result<TokenResponseDto, DomainError>> LoginAsync(LoginDto dto);
    Task<Result<TokenResponseDto, DomainError>> RegisterAsync(RegisterDto dto);
    Task<Result<TokenResponseDto, DomainError>> RefreshTokenAsync(RefreshTokenRequestDto dto);
    Task<Result<UsuarioInfoDto, DomainError>> GetUserInfoAsync(string userId);
    Task<Result<UsuarioInfoDto, DomainError>> UpdateProfileAsync(string userId, ActualizarUsuarioDto dto);
    Task<UnitResult<DomainError>> ChangePasswordAsync(string userId, CambiarPasswordDto dto);
    Task SolicitarResetPasswordAsync(string email, string frontendUrl);
    Task<UnitResult<DomainError>> ResetPasswordAsync(ResetPasswordDto dto);
}

/// <summary>
/// Implementación del servicio de autenticación con primary constructor.
/// </summary>
public class AuthService(
    IUserRepository userRepository,
    TokenService tokenService,
    IEmailService emailService,
    ILogger<AuthService> logger) : IAuthService
{
    private const string TokenProvider = "NexoPostal";
    private const string RefreshTokenHashName = "RefreshTokenHash";
    private const string RefreshTokenExpiryName = "RefreshTokenExpiryUtc";

    public async Task<Result<TokenResponseDto, DomainError>> LoginAsync(LoginDto dto)
    {
        var user = await userRepository.GetByEmailAsync(dto.Email);
        if (user is null || !await userRepository.CheckPasswordAsync(user, dto.Password))
            return Result.Failure<TokenResponseDto, DomainError>(AuthError.InvalidCredentials());

        if (user.Eliminado || await userRepository.IsLockedOutAsync(user))
            return Result.Failure<TokenResponseDto, DomainError>(AuthError.UserBlocked());

        var token = await EmitTokenPairAsync(user);
        logger.LogInformation("Login OK para {Email}", dto.Email);
        return Result.Success<TokenResponseDto, DomainError>(token);
    }

    public async Task<Result<TokenResponseDto, DomainError>> RegisterAsync(RegisterDto dto)
    {
        var existente = await userRepository.GetByEmailAsync(dto.Email);
        if (existente is not null)
            return Result.Failure<TokenResponseDto, DomainError>(AuthError.EmailAlreadyExists(dto.Email));

        var user = new ApplicationUser
        {
            UserName = dto.Email,
            Email = dto.Email,
            NombreCompleto = dto.NombreCompleto,
            Rol = Rol.Cliente
        };

        var result = await userRepository.CreateAsync(user, dto.Password);
        if (!result.Succeeded)
            return Result.Failure<TokenResponseDto, DomainError>(
                AuthError.IdentityErrors(result.Errors.Select(e => e.Description)));

        var token = await EmitTokenPairAsync(user);
        return Result.Success<TokenResponseDto, DomainError>(token);
    }

    public async Task<Result<TokenResponseDto, DomainError>> RefreshTokenAsync(RefreshTokenRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.RefreshToken))
            return Result.Failure<TokenResponseDto, DomainError>(AuthError.InvalidRefreshToken());

        if (!tokenService.TryExtractUserId(dto.RefreshToken, out var userId))
            return Result.Failure<TokenResponseDto, DomainError>(AuthError.InvalidRefreshToken());

        var user = await userRepository.GetByIdAsync(userId);
        if (user is null)
            return Result.Failure<TokenResponseDto, DomainError>(AuthError.InvalidRefreshToken());

        if (user.Eliminado || await userRepository.IsLockedOutAsync(user))
        {
            await RevokeRefreshTokenAsync(user);
            return Result.Failure<TokenResponseDto, DomainError>(AuthError.InvalidRefreshToken());
        }

        var storedHash = await userRepository.GetUserTokenAsync(user, TokenProvider, RefreshTokenHashName);
        var storedExpiry = await userRepository.GetUserTokenAsync(user, TokenProvider, RefreshTokenExpiryName);
        if (string.IsNullOrWhiteSpace(storedHash) || string.IsNullOrWhiteSpace(storedExpiry))
            return Result.Failure<TokenResponseDto, DomainError>(AuthError.InvalidRefreshToken());

        if (!DateTime.TryParse(storedExpiry, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var expiryUtc))
        {
            await RevokeRefreshTokenAsync(user);
            return Result.Failure<TokenResponseDto, DomainError>(AuthError.InvalidRefreshToken());
        }

        if (expiryUtc <= DateTime.UtcNow)
        {
            await RevokeRefreshTokenAsync(user);
            return Result.Failure<TokenResponseDto, DomainError>(AuthError.InvalidRefreshToken());
        }

        var incomingHash = tokenService.HashToken(dto.RefreshToken);
        if (!tokenService.SecureEquals(incomingHash, storedHash))
            return Result.Failure<TokenResponseDto, DomainError>(AuthError.InvalidRefreshToken());

        var newPair = await EmitTokenPairAsync(user);
        return Result.Success<TokenResponseDto, DomainError>(newPair);
    }

    public async Task<Result<UsuarioInfoDto, DomainError>> GetUserInfoAsync(string userId)
    {
        var user = await userRepository.GetByIdAsync(userId);
        return user is null
            ? Result.Failure<UsuarioInfoDto, DomainError>(AuthError.UserNotFound(userId))
            : Result.Success<UsuarioInfoDto, DomainError>(user.ToInfoDto());
    }

    public async Task<Result<UsuarioInfoDto, DomainError>> UpdateProfileAsync(string userId, ActualizarUsuarioDto dto)
    {
        var user = await userRepository.GetByIdAsync(userId);
        if (user is null)
            return Result.Failure<UsuarioInfoDto, DomainError>(AuthError.UserNotFound(userId));

        user.NombreCompleto = dto.NombreCompleto;
        user.PhoneNumber = dto.PhoneNumber;

        if (!string.Equals(user.Email, dto.Email, StringComparison.OrdinalIgnoreCase))
        {
            var emailExists = await userRepository.GetByEmailAsync(dto.Email);
            if (emailExists is not null && emailExists.Id != userId)
                return Result.Failure<UsuarioInfoDto, DomainError>(AuthError.EmailAlreadyExists(dto.Email));

            user.Email = dto.Email;
            user.UserName = dto.Email;
        }

        var result = await userRepository.UpdateAsync(user);
        return result.Succeeded
            ? Result.Success<UsuarioInfoDto, DomainError>(user.ToInfoDto())
            : Result.Failure<UsuarioInfoDto, DomainError>(
                AuthError.IdentityErrors(result.Errors.Select(e => e.Description)));
    }

    public async Task<UnitResult<DomainError>> ChangePasswordAsync(string userId, CambiarPasswordDto dto)
    {
        var user = await userRepository.GetByIdAsync(userId);
        if (user is null)
            return UnitResult.Failure<DomainError>(AuthError.UserNotFound(userId));

        var result = await userRepository.ChangePasswordAsync(user, dto.PasswordActual, dto.NuevaPassword);
        if (!result.Succeeded)
            return UnitResult.Failure<DomainError>(
                AuthError.IdentityErrors(result.Errors.Select(e => e.Description)));

        await RevokeRefreshTokenAsync(user);
        return UnitResult.Success<DomainError>();
    }

    public async Task SolicitarResetPasswordAsync(string email, string frontendUrl)
    {
        var user = await userRepository.GetByEmailAsync(email);
        // No revelamos si el email está registrado.
        if (user is null) return;

        var token = await userRepository.GeneratePasswordResetTokenAsync(user);
        var encodedToken = Uri.EscapeDataString(token);
        var encodedEmail = Uri.EscapeDataString(email);
        var resetLink = $"{frontendUrl.TrimEnd('/')}/reset-password?email={encodedEmail}&token={encodedToken}";

        await emailService.SendPasswordResetEmailAsync(user.Email!, user.NombreCompleto, resetLink);
    }

    public async Task<UnitResult<DomainError>> ResetPasswordAsync(ResetPasswordDto dto)
    {
        var user = await userRepository.GetByEmailAsync(dto.Email);
        if (user is null)
            return UnitResult.Failure<DomainError>(AuthError.ResetPasswordFailed(string.Empty));

        var result = await userRepository.ResetPasswordAsync(user, dto.Token, dto.NuevaPassword);
        if (!result.Succeeded)
        {
            var detail = result.Errors.FirstOrDefault()?.Description ?? string.Empty;
            return UnitResult.Failure<DomainError>(AuthError.ResetPasswordFailed(detail));
        }

        await RevokeRefreshTokenAsync(user);
        return UnitResult.Success<DomainError>();
    }

    // ─── Helpers internos ───

    private async Task<TokenResponseDto> EmitTokenPairAsync(ApplicationUser user)
    {
        var (accessToken, accessExpirationUtc) = tokenService.GenerateAccessToken(user);
        var refreshToken = tokenService.GenerateRefreshToken(user.Id);
        var refreshTokenHash = tokenService.HashToken(refreshToken);
        var refreshExpirationUtc = DateTime.UtcNow.AddDays(tokenService.GetRefreshTokenExpiryDays());

        await userRepository.SetUserTokenAsync(user, TokenProvider, RefreshTokenHashName, refreshTokenHash);
        await userRepository.SetUserTokenAsync(
            user, TokenProvider, RefreshTokenExpiryName,
            refreshExpirationUtc.ToString("O", CultureInfo.InvariantCulture));

        return user.ToTokenResponseDto(accessToken, accessExpirationUtc, refreshToken, refreshExpirationUtc);
    }

    private async Task RevokeRefreshTokenAsync(ApplicationUser user)
    {
        await userRepository.RemoveUserTokenAsync(user, TokenProvider, RefreshTokenHashName);
        await userRepository.RemoveUserTokenAsync(user, TokenProvider, RefreshTokenExpiryName);
    }
}
