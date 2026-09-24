using CSharpFunctionalExtensions;
using Nexopostal.Shared.Errors;
using NexoPostal.Auth.DTOs.Admin;
using NexoPostal.Auth.Errors;
using NexoPostal.Auth.Mappers;
using NexoPostal.Auth.Models;
using NexoPostal.Auth.Repositories;
using NexoPostal.Auth.Services;

namespace NexoPostal.Auth.Application.AdminUser;
/// <summary>Contrato de operaciones que modifican estado para AdminUser.</summary>
public interface IAdminUserCommands
{
    Task<UnitResult<DomainError>> CambiarRolAsync(string id, Rol nuevoRol, string adminId);
    Task<UnitResult<DomainError>> BloquearAsync(string id, string adminId);
    Task<UnitResult<DomainError>> DesbloquearAsync(string id);
    Task<UnitResult<DomainError>> ResetPasswordAsync(string id, string nuevaPassword);
    Task<Result<AdminUsuarioListItemDto, DomainError>> CrearEmpleadoAsync(AdminCrearEmpleadoDto dto);
    Task<Result<AdminUsuarioListItemDto, DomainError>> EditarEmpleadoAsync(string id, AdminEditarEmpleadoDto dto, string adminId);
    Task<UnitResult<DomainError>> EliminarAsync(string id, string adminId);
    Task<UnitResult<DomainError>> RestaurarAsync(string id);
}
