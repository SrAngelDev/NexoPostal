using NexoPostal.Auth.DTOs;
using NexoPostal.Auth.Models;
using NexoPostal.Auth.Repositories;

namespace NexoPostal.Auth.Services;

/// <summary>
/// Interfaz del servicio de autenticación.
/// </summary>
public interface IAuthService
{
    Task<TokenResponseDto?> LoginAsync(LoginDto dto);
    Task<(TokenResponseDto? Token, IEnumerable<string>? Errors)> RegisterAsync(RegisterDto dto);
    Task<UsuarioInfoDto?> GetUserInfoAsync(string userId);
    Task<(UsuarioInfoDto? User, string? Error)> UpdateProfileAsync(string userId, ActualizarUsuarioDto dto);
    Task<(bool Success, string? Error)> ChangePasswordAsync(string userId, CambiarPasswordDto dto);
}

/// <summary>
/// Servicio de autenticación que encapsula toda la lógica de negocio de auth.
/// Inyecta IUserRepository para acceso a datos (patrón repositorio).
/// </summary>
public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly TokenService _tokenService;

    public AuthService(IUserRepository userRepository, TokenService tokenService)
    {
        _userRepository = userRepository;
        _tokenService = tokenService;
    }

    public async Task<TokenResponseDto?> LoginAsync(LoginDto dto)
    {
        var user = await _userRepository.GetByEmailAsync(dto.Email);
        if (user == null || !await _userRepository.CheckPasswordAsync(user, dto.Password))
            return null;

        var token = _tokenService.GenerateJwtToken(user);
        return new TokenResponseDto
        {
            Token = token,
            Expiration = DateTime.UtcNow.AddMinutes(60),
            User = user.NombreCompleto,
            Rol = user.Rol.ToString()
        };
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

        var token = _tokenService.GenerateJwtToken(user);
        return (new TokenResponseDto
        {
            Token = token,
            Expiration = DateTime.UtcNow.AddMinutes(60),
            User = user.NombreCompleto,
            Rol = user.Rol.ToString()
        }, null);
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

        return (true, null);
    }
}
