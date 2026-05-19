using NexoPostal.Auth.DTOs.Admin;
using NexoPostal.Auth.Models;
using NexoPostal.Auth.Repositories;

namespace NexoPostal.Auth.Services;

public interface IAdminUserService
{
    Task<List<AdminUsuarioListItemDto>> ListarUsuariosAsync(Rol? rol, bool? bloqueado, string? q);
    Task<AdminUsuarioListItemDto?> ObtenerDetalleAsync(string id);
    Task<(bool Ok, string? Error)> CambiarRolAsync(string id, Rol nuevoRol, string adminId);
    Task<(bool Ok, string? Error)> BloquearAsync(string id, string adminId);
    Task<(bool Ok, string? Error)> DesbloquearAsync(string id);
    Task<(bool Ok, string? Error)> ResetPasswordAsync(string id, string nuevaPassword);
    Task<(AdminUsuarioListItemDto? User, string? Error)> CrearEmpleadoAsync(AdminCrearEmpleadoDto dto);
}

public class AdminUserService : IAdminUserService
{
    private const string TokenProvider = "NexoPostal";
    private const string RefreshTokenHashName = "RefreshTokenHash";
    private const string RefreshTokenExpiryName = "RefreshTokenExpiryUtc";

    private readonly IUserRepository _userRepository;

    public AdminUserService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<List<AdminUsuarioListItemDto>> ListarUsuariosAsync(Rol? rol, bool? bloqueado, string? q)
    {
        var usuarios = await _userRepository.GetAllAsync(rol, bloqueado);

        if (!string.IsNullOrWhiteSpace(q))
        {
            q = q.Trim().ToLowerInvariant();
            usuarios = usuarios
                .Where(u => u.NombreCompleto.ToLowerInvariant().Contains(q)
                         || (u.Email ?? "").ToLowerInvariant().Contains(q)
                         || (u.CodigoEmpleado ?? "").ToLowerInvariant().Contains(q))
                .ToList();
        }

        var ahora = DateTimeOffset.UtcNow;
        return usuarios.Select(u => MapToDto(u, ahora)).ToList();
    }

    public async Task<AdminUsuarioListItemDto?> ObtenerDetalleAsync(string id)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null) return null;
        return MapToDto(user, DateTimeOffset.UtcNow);
    }

    public async Task<(bool Ok, string? Error)> CambiarRolAsync(string id, Rol nuevoRol, string adminId)
    {
        if (id == adminId)
            return (false, "No puedes cambiar tu propio rol.");

        var user = await _userRepository.GetByIdAsync(id);
        if (user == null)
            return (false, "Usuario no encontrado.");

        user.Rol = nuevoRol;
        var result = await _userRepository.UpdateAsync(user);
        return result.Succeeded
            ? (true, null)
            : (false, string.Join(", ", result.Errors.Select(e => e.Description)));
    }

    public async Task<(bool Ok, string? Error)> BloquearAsync(string id, string adminId)
    {
        if (id == adminId)
            return (false, "No puedes bloquearte a ti mismo.");

        var user = await _userRepository.GetByIdAsync(id);
        if (user == null)
            return (false, "Usuario no encontrado.");

        var result = await _userRepository.SetLockoutAsync(user, bloquear: true);
        if (!result.Succeeded)
            return (false, string.Join(", ", result.Errors.Select(e => e.Description)));

        await _userRepository.RemoveUserTokenAsync(user, TokenProvider, RefreshTokenHashName);
        await _userRepository.RemoveUserTokenAsync(user, TokenProvider, RefreshTokenExpiryName);

        return (true, null);
    }

    public async Task<(bool Ok, string? Error)> DesbloquearAsync(string id)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null)
            return (false, "Usuario no encontrado.");

        var result = await _userRepository.SetLockoutAsync(user, bloquear: false);
        return result.Succeeded
            ? (true, null)
            : (false, string.Join(", ", result.Errors.Select(e => e.Description)));
    }

    public async Task<(bool Ok, string? Error)> ResetPasswordAsync(string id, string nuevaPassword)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null)
            return (false, "Usuario no encontrado.");

        var result = await _userRepository.AdminResetPasswordAsync(user, nuevaPassword);
        return result.Succeeded
            ? (true, null)
            : (false, string.Join(", ", result.Errors.Select(e => e.Description)));
    }

    public async Task<(AdminUsuarioListItemDto? User, string? Error)> CrearEmpleadoAsync(AdminCrearEmpleadoDto dto)
    {
        if (dto.Rol == Rol.Cliente)
            return (null, "No se puede crear un empleado con rol Cliente.");

        var existente = await _userRepository.GetByEmailAsync(dto.Email);
        if (existente != null)
            return (null, "Ya existe un usuario con ese email.");

        var user = new ApplicationUser
        {
            UserName          = dto.Email,
            Email             = dto.Email,
            NombreCompleto    = dto.NombreCompleto,
            CodigoEmpleado    = dto.CodigoEmpleado,
            Rol               = dto.Rol,
            EmailConfirmed    = true,
            LockoutEnabled    = true
        };

        var result = await _userRepository.CreateAsync(user, dto.Password);
        if (!result.Succeeded)
            return (null, string.Join(", ", result.Errors.Select(e => e.Description)));

        return (MapToDto(user, DateTimeOffset.UtcNow), null);
    }

    private static AdminUsuarioListItemDto MapToDto(ApplicationUser u, DateTimeOffset ahora) => new()
    {
        Id             = u.Id,
        NombreCompleto = u.NombreCompleto,
        Email          = u.Email ?? string.Empty,
        CodigoEmpleado = u.CodigoEmpleado,
        Rol            = u.Rol.ToString(),
        FechaRegistro  = u.FechaRegistro,
        Bloqueado      = u.LockoutEnd != null && u.LockoutEnd > ahora
    };
}
