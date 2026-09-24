using CSharpFunctionalExtensions;
using Nexopostal.Shared.Errors;
using NexoPostal.Auth.DTOs.Admin;
using NexoPostal.Auth.Errors;
using NexoPostal.Auth.Mappers;
using NexoPostal.Auth.Models;
using NexoPostal.Auth.Repositories;
using NexoPostal.Auth.Services;

namespace NexoPostal.Auth.Application.AdminUser;
/// <summary>Contrato de lectura sin escrituras para AdminUser.</summary>
public interface IAdminUserQueries
{
    Task<List<AdminUsuarioListItemDto>> ListarUsuariosAsync(Rol? rol, bool? bloqueado, string? q, bool incluirEliminados = false);
    Task<Result<AdminUsuarioListItemDto, DomainError>> ObtenerDetalleAsync(string id);
}
