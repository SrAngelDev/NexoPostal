using System.Globalization;
using CSharpFunctionalExtensions;
using Microsoft.Extensions.Logging;
using Nexopostal.Shared.Errors;
using NexoPostal.Auth.DTOs;
using NexoPostal.Auth.Errors;
using NexoPostal.Auth.Mappers;
using NexoPostal.Auth.Models;
using NexoPostal.Auth.Repositories;
using NexoPostal.Auth.Services;

namespace NexoPostal.Auth.Application.Auth;
/// <summary>Contrato de operaciones que modifican estado para Auth.</summary>
public interface IAuthCommands
{
    Task<Result<TokenResponseDto, DomainError>> LoginAsync(LoginDto dto);
    Task<Result<TokenResponseDto, DomainError>> RegisterAsync(RegisterDto dto);
    Task<Result<TokenResponseDto, DomainError>> RefreshTokenAsync(RefreshTokenRequestDto dto);
    Task<Result<UsuarioInfoDto, DomainError>> UpdateProfileAsync(string userId, ActualizarUsuarioDto dto);
    Task<UnitResult<DomainError>> ChangePasswordAsync(string userId, CambiarPasswordDto dto);
    Task SolicitarResetPasswordAsync(string email, string frontendUrl);
    Task<UnitResult<DomainError>> ResetPasswordAsync(ResetPasswordDto dto);
}
