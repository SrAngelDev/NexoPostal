using NexoPostal.Auth.DTOs.Admin;
using NexoPostal.Auth.Models;
using NexoPostal.Auth.Repositories;

namespace NexoPostal.Auth.Services;

public interface IAdminUserService
{
    Task<List<AdminUsuarioListItemDto>> ListarUsuariosAsync(Rol? rol, bool? bloqueado, string? q, bool incluirEliminados = false);
    Task<AdminUsuarioListItemDto?> ObtenerDetalleAsync(string id);
    Task<(bool Ok, string? Error)> CambiarRolAsync(string id, Rol nuevoRol, string adminId);
    Task<(bool Ok, string? Error)> BloquearAsync(string id, string adminId);
    Task<(bool Ok, string? Error)> DesbloquearAsync(string id);
    Task<(bool Ok, string? Error)> ResetPasswordAsync(string id, string nuevaPassword);
    Task<(AdminUsuarioListItemDto? User, string? Error)> CrearEmpleadoAsync(AdminCrearEmpleadoDto dto);
    Task<(AdminUsuarioListItemDto? User, string? Error)> EditarEmpleadoAsync(string id, AdminEditarEmpleadoDto dto, string adminId);
    Task<(bool Ok, string? Error)> EliminarAsync(string id, string adminId);
    Task<(bool Ok, string? Error)> RestaurarAsync(string id);
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

    public async Task<List<AdminUsuarioListItemDto>> ListarUsuariosAsync(Rol? rol, bool? bloqueado, string? q, bool incluirEliminados = false)
    {
        var usuarios = await _userRepository.GetAllAsync(rol, bloqueado, incluirEliminados);

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

        if (user.Eliminado)
            return (false, "El usuario está eliminado. Restaurálo antes de modificarlo.");

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

    public async Task<(AdminUsuarioListItemDto? User, string? Error)> EditarEmpleadoAsync(string id, AdminEditarEmpleadoDto dto, string adminId)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null)
            return (null, "Usuario no encontrado.");

        if (user.Eliminado)
            return (null, "El usuario está eliminado. Restáuralo antes de modificarlo.");

        if (dto.Rol == Rol.Cliente && user.Rol != Rol.Cliente)
            return (null, "No se puede degradar a Cliente desde la administración interna.");

        if (id == adminId && dto.Rol != user.Rol)
            return (null, "No puedes cambiar tu propio rol.");

        // Cambio de email — validar duplicados y actualizar UserName
        if (!string.Equals(user.Email, dto.Email, StringComparison.OrdinalIgnoreCase))
        {
            var existente = await _userRepository.GetByEmailAsync(dto.Email);
            if (existente != null && existente.Id != id)
                return (null, "Ya existe un usuario con ese email.");

            var setEmail = await _userRepository.SetEmailAsync(user, dto.Email);
            if (!setEmail.Succeeded)
                return (null, string.Join(", ", setEmail.Errors.Select(e => e.Description)));
        }

        user.NombreCompleto = dto.NombreCompleto.Trim();
        user.CodigoEmpleado = string.IsNullOrWhiteSpace(dto.CodigoEmpleado) ? null : dto.CodigoEmpleado.Trim();
        user.PhoneNumber    = string.IsNullOrWhiteSpace(dto.PhoneNumber) ? null : dto.PhoneNumber.Trim();
        user.Rol            = dto.Rol;

        var result = await _userRepository.UpdateAsync(user);
        if (!result.Succeeded)
            return (null, string.Join(", ", result.Errors.Select(e => e.Description)));

        return (MapToDto(user, DateTimeOffset.UtcNow), null);
    }

    public async Task<(bool Ok, string? Error)> EliminarAsync(string id, string adminId)
    {
        if (id == adminId)
            return (false, "No puedes eliminarte a ti mismo.");

        var user = await _userRepository.GetByIdAsync(id);
        if (user == null)
            return (false, "Usuario no encontrado.");

        if (user.Eliminado)
            return (true, null); // idempotente

        user.Eliminado       = true;
        user.EliminadoEnUtc  = DateTime.UtcNow;
        user.EliminadoPorId  = adminId;

        // Bloquear acceso indefinidamente
        var lockoutResult = await _userRepository.SetLockoutAsync(user, bloquear: true);
        if (!lockoutResult.Succeeded)
            return (false, string.Join(", ", lockoutResult.Errors.Select(e => e.Description)));

        var updateResult = await _userRepository.UpdateAsync(user);
        if (!updateResult.Succeeded)
            return (false, string.Join(", ", updateResult.Errors.Select(e => e.Description)));

        // Invalidar refresh tokens activos
        await _userRepository.RemoveUserTokenAsync(user, TokenProvider, RefreshTokenHashName);
        await _userRepository.RemoveUserTokenAsync(user, TokenProvider, RefreshTokenExpiryName);

        return (true, null);
    }

    public async Task<(bool Ok, string? Error)> RestaurarAsync(string id)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null)
            return (false, "Usuario no encontrado.");

        if (!user.Eliminado)
            return (true, null); // idempotente

        user.Eliminado       = false;
        user.EliminadoEnUtc  = null;
        user.EliminadoPorId  = null;

        // Desbloquear el acceso (el admin puede volver a bloquear si quiere)
        var lockoutResult = await _userRepository.SetLockoutAsync(user, bloquear: false);
        if (!lockoutResult.Succeeded)
            return (false, string.Join(", ", lockoutResult.Errors.Select(e => e.Description)));

        var updateResult = await _userRepository.UpdateAsync(user);
        return updateResult.Succeeded
            ? (true, null)
            : (false, string.Join(", ", updateResult.Errors.Select(e => e.Description)));
    }

    private static AdminUsuarioListItemDto MapToDto(ApplicationUser u, DateTimeOffset ahora) => new()
    {
        Id             = u.Id,
        NombreCompleto = u.NombreCompleto,
        Email          = u.Email ?? string.Empty,
        CodigoEmpleado = u.CodigoEmpleado,
        PhoneNumber    = u.PhoneNumber,
        Rol            = u.Rol.ToString(),
        FechaRegistro  = u.FechaRegistro,
        Bloqueado      = u.LockoutEnd != null && u.LockoutEnd > ahora,
        Eliminado      = u.Eliminado,
        EliminadoEnUtc = u.EliminadoEnUtc
    };
}
