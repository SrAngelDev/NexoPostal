using CSharpFunctionalExtensions;
using Nexopostal.Shared.Errors;
using NexoPostal.Auth.DTOs.Admin;
using NexoPostal.Auth.Errors;
using NexoPostal.Auth.Mappers;
using NexoPostal.Auth.Models;
using NexoPostal.Auth.Repositories;

namespace NexoPostal.Auth.Services;

/// <summary>
/// Gestión administrativa de usuarios. Devuelve <see cref="Result{T,DomainError}"/> /
/// <see cref="UnitResult{DomainError}"/> para que el controller mapee a HTTP de forma uniforme.
/// </summary>
public interface IAdminUserService
{
    Task<List<AdminUsuarioListItemDto>> ListarUsuariosAsync(Rol? rol, bool? bloqueado, string? q, bool incluirEliminados = false);
    Task<Result<AdminUsuarioListItemDto, DomainError>> ObtenerDetalleAsync(string id);
    Task<UnitResult<DomainError>> CambiarRolAsync(string id, Rol nuevoRol, string adminId);
    Task<UnitResult<DomainError>> BloquearAsync(string id, string adminId);
    Task<UnitResult<DomainError>> DesbloquearAsync(string id);
    Task<UnitResult<DomainError>> ResetPasswordAsync(string id, string nuevaPassword);
    Task<Result<AdminUsuarioListItemDto, DomainError>> CrearEmpleadoAsync(AdminCrearEmpleadoDto dto);
    Task<Result<AdminUsuarioListItemDto, DomainError>> EditarEmpleadoAsync(string id, AdminEditarEmpleadoDto dto, string adminId);
    Task<UnitResult<DomainError>> EliminarAsync(string id, string adminId);
    Task<UnitResult<DomainError>> RestaurarAsync(string id);
}

public class AdminUserService(IUserRepository userRepository) : IAdminUserService
{
    private const string TokenProvider = "NexoPostal";
    private const string RefreshTokenHashName = "RefreshTokenHash";
    private const string RefreshTokenExpiryName = "RefreshTokenExpiryUtc";

    public async Task<List<AdminUsuarioListItemDto>> ListarUsuariosAsync(Rol? rol, bool? bloqueado, string? q, bool incluirEliminados = false)
    {
        var usuarios = await userRepository.GetAllAsync(rol, bloqueado, incluirEliminados);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim().ToLowerInvariant();
            usuarios = usuarios
                .Where(u => u.NombreCompleto.ToLowerInvariant().Contains(term)
                         || (u.Email ?? "").ToLowerInvariant().Contains(term)
                         || (u.CodigoEmpleado ?? "").ToLowerInvariant().Contains(term))
                .ToList();
        }

        return usuarios.ToListItemDtos(DateTimeOffset.UtcNow);
    }

    public async Task<Result<AdminUsuarioListItemDto, DomainError>> ObtenerDetalleAsync(string id)
    {
        var user = await userRepository.GetByIdAsync(id);
        return user is null
            ? Result.Failure<AdminUsuarioListItemDto, DomainError>(AuthError.UserNotFound(id))
            : Result.Success<AdminUsuarioListItemDto, DomainError>(user.ToListItemDto(DateTimeOffset.UtcNow));
    }

    public async Task<UnitResult<DomainError>> CambiarRolAsync(string id, Rol nuevoRol, string adminId)
    {
        if (id == adminId)
            return UnitResult.Failure<DomainError>(AuthError.CannotModifySelf("cambiar el rol"));

        var user = await userRepository.GetByIdAsync(id);
        if (user is null)
            return UnitResult.Failure<DomainError>(AuthError.UserNotFound(id));

        if (user.Eliminado)
            return UnitResult.Failure<DomainError>(AuthError.UserDeleted());

        user.Rol = nuevoRol;
        var result = await userRepository.UpdateAsync(user);
        return result.Succeeded
            ? UnitResult.Success<DomainError>()
            : UnitResult.Failure<DomainError>(AuthError.IdentityErrors(result.Errors.Select(e => e.Description)));
    }

    public async Task<UnitResult<DomainError>> BloquearAsync(string id, string adminId)
    {
        if (id == adminId)
            return UnitResult.Failure<DomainError>(AuthError.CannotModifySelf("bloquear"));

        var user = await userRepository.GetByIdAsync(id);
        if (user is null)
            return UnitResult.Failure<DomainError>(AuthError.UserNotFound(id));

        var result = await userRepository.SetLockoutAsync(user, bloquear: true);
        if (!result.Succeeded)
            return UnitResult.Failure<DomainError>(AuthError.IdentityErrors(result.Errors.Select(e => e.Description)));

        await userRepository.RemoveUserTokenAsync(user, TokenProvider, RefreshTokenHashName);
        await userRepository.RemoveUserTokenAsync(user, TokenProvider, RefreshTokenExpiryName);

        return UnitResult.Success<DomainError>();
    }

    public async Task<UnitResult<DomainError>> DesbloquearAsync(string id)
    {
        var user = await userRepository.GetByIdAsync(id);
        if (user is null)
            return UnitResult.Failure<DomainError>(AuthError.UserNotFound(id));

        var result = await userRepository.SetLockoutAsync(user, bloquear: false);
        return result.Succeeded
            ? UnitResult.Success<DomainError>()
            : UnitResult.Failure<DomainError>(AuthError.IdentityErrors(result.Errors.Select(e => e.Description)));
    }

    public async Task<UnitResult<DomainError>> ResetPasswordAsync(string id, string nuevaPassword)
    {
        var user = await userRepository.GetByIdAsync(id);
        if (user is null)
            return UnitResult.Failure<DomainError>(AuthError.UserNotFound(id));

        var result = await userRepository.AdminResetPasswordAsync(user, nuevaPassword);
        return result.Succeeded
            ? UnitResult.Success<DomainError>()
            : UnitResult.Failure<DomainError>(AuthError.IdentityErrors(result.Errors.Select(e => e.Description)));
    }

    public async Task<Result<AdminUsuarioListItemDto, DomainError>> CrearEmpleadoAsync(AdminCrearEmpleadoDto dto)
    {
        if (dto.Rol == Rol.Cliente)
            return Result.Failure<AdminUsuarioListItemDto, DomainError>(AuthError.CannotCreateClientAsEmployee());

        var existente = await userRepository.GetByEmailAsync(dto.Email);
        if (existente is not null)
            return Result.Failure<AdminUsuarioListItemDto, DomainError>(AuthError.EmailAlreadyExists(dto.Email));

        var user = new ApplicationUser
        {
            UserName       = dto.Email,
            Email          = dto.Email,
            NombreCompleto = dto.NombreCompleto,
            CodigoEmpleado = dto.CodigoEmpleado,
            Rol            = dto.Rol,
            EmailConfirmed = true,
            LockoutEnabled = true
        };

        var result = await userRepository.CreateAsync(user, dto.Password);
        if (!result.Succeeded)
            return Result.Failure<AdminUsuarioListItemDto, DomainError>(
                AuthError.IdentityErrors(result.Errors.Select(e => e.Description)));

        return Result.Success<AdminUsuarioListItemDto, DomainError>(user.ToListItemDto(DateTimeOffset.UtcNow));
    }

    public async Task<Result<AdminUsuarioListItemDto, DomainError>> EditarEmpleadoAsync(string id, AdminEditarEmpleadoDto dto, string adminId)
    {
        var user = await userRepository.GetByIdAsync(id);
        if (user is null)
            return Result.Failure<AdminUsuarioListItemDto, DomainError>(AuthError.UserNotFound(id));

        if (user.Eliminado)
            return Result.Failure<AdminUsuarioListItemDto, DomainError>(AuthError.UserDeleted());

        if (dto.Rol == Rol.Cliente && user.Rol != Rol.Cliente)
            return Result.Failure<AdminUsuarioListItemDto, DomainError>(AuthError.CannotDowngradeToClient());

        if (id == adminId && dto.Rol != user.Rol)
            return Result.Failure<AdminUsuarioListItemDto, DomainError>(AuthError.CannotModifySelf("cambiar el rol"));

        if (!string.Equals(user.Email, dto.Email, StringComparison.OrdinalIgnoreCase))
        {
            var existente = await userRepository.GetByEmailAsync(dto.Email);
            if (existente is not null && existente.Id != id)
                return Result.Failure<AdminUsuarioListItemDto, DomainError>(AuthError.EmailAlreadyExists(dto.Email));

            var setEmail = await userRepository.SetEmailAsync(user, dto.Email);
            if (!setEmail.Succeeded)
                return Result.Failure<AdminUsuarioListItemDto, DomainError>(
                    AuthError.IdentityErrors(setEmail.Errors.Select(e => e.Description)));
        }

        user.NombreCompleto = dto.NombreCompleto.Trim();
        user.CodigoEmpleado = string.IsNullOrWhiteSpace(dto.CodigoEmpleado) ? null : dto.CodigoEmpleado.Trim();
        user.PhoneNumber    = string.IsNullOrWhiteSpace(dto.PhoneNumber) ? null : dto.PhoneNumber.Trim();
        user.Rol            = dto.Rol;

        var result = await userRepository.UpdateAsync(user);
        if (!result.Succeeded)
            return Result.Failure<AdminUsuarioListItemDto, DomainError>(
                AuthError.IdentityErrors(result.Errors.Select(e => e.Description)));

        return Result.Success<AdminUsuarioListItemDto, DomainError>(user.ToListItemDto(DateTimeOffset.UtcNow));
    }

    public async Task<UnitResult<DomainError>> EliminarAsync(string id, string adminId)
    {
        if (id == adminId)
            return UnitResult.Failure<DomainError>(AuthError.CannotModifySelf("eliminar"));

        var user = await userRepository.GetByIdAsync(id);
        if (user is null)
            return UnitResult.Failure<DomainError>(AuthError.UserNotFound(id));

        if (user.Eliminado)
            return UnitResult.Success<DomainError>(); // idempotente

        user.Eliminado      = true;
        user.EliminadoEnUtc = DateTime.UtcNow;
        user.EliminadoPorId = adminId;

        var lockoutResult = await userRepository.SetLockoutAsync(user, bloquear: true);
        if (!lockoutResult.Succeeded)
            return UnitResult.Failure<DomainError>(AuthError.IdentityErrors(lockoutResult.Errors.Select(e => e.Description)));

        var updateResult = await userRepository.UpdateAsync(user);
        if (!updateResult.Succeeded)
            return UnitResult.Failure<DomainError>(AuthError.IdentityErrors(updateResult.Errors.Select(e => e.Description)));

        await userRepository.RemoveUserTokenAsync(user, TokenProvider, RefreshTokenHashName);
        await userRepository.RemoveUserTokenAsync(user, TokenProvider, RefreshTokenExpiryName);

        return UnitResult.Success<DomainError>();
    }

    public async Task<UnitResult<DomainError>> RestaurarAsync(string id)
    {
        var user = await userRepository.GetByIdAsync(id);
        if (user is null)
            return UnitResult.Failure<DomainError>(AuthError.UserNotFound(id));

        if (!user.Eliminado)
            return UnitResult.Success<DomainError>(); // idempotente

        user.Eliminado      = false;
        user.EliminadoEnUtc = null;
        user.EliminadoPorId = null;

        var lockoutResult = await userRepository.SetLockoutAsync(user, bloquear: false);
        if (!lockoutResult.Succeeded)
            return UnitResult.Failure<DomainError>(AuthError.IdentityErrors(lockoutResult.Errors.Select(e => e.Description)));

        var updateResult = await userRepository.UpdateAsync(user);
        return updateResult.Succeeded
            ? UnitResult.Success<DomainError>()
            : UnitResult.Failure<DomainError>(AuthError.IdentityErrors(updateResult.Errors.Select(e => e.Description)));
    }
}
