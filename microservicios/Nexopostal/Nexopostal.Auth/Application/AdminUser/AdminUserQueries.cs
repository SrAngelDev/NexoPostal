using MediatR;
using Nexopostal.Shared.Cqrs;
using CSharpFunctionalExtensions;
using Nexopostal.Shared.Errors;
using NexoPostal.Auth.DTOs.Admin;
using NexoPostal.Auth.Errors;
using NexoPostal.Auth.Mappers;
using NexoPostal.Auth.Models;
using NexoPostal.Auth.Repositories;
using NexoPostal.Auth.Services;

namespace NexoPostal.Auth.Application.AdminUser;
public sealed record ListarUsuariosQuery(Rol? Rol, bool? Bloqueado, string? Q, bool IncluirEliminados = false) : IQuery<List<AdminUsuarioListItemDto>>;
public sealed class ListarUsuariosQueryHandler(IAdminUserQueries service) : IRequestHandler<ListarUsuariosQuery, List<AdminUsuarioListItemDto>>
{
    public Task<List<AdminUsuarioListItemDto>> Handle(ListarUsuariosQuery request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return service.ListarUsuariosAsync(request.Rol, request.Bloqueado, request.Q, request.IncluirEliminados);
    }
}

public sealed record ObtenerDetalleQuery(string Id) : IQuery<Result<AdminUsuarioListItemDto, DomainError>>;
public sealed class ObtenerDetalleQueryHandler(IAdminUserQueries service) : IRequestHandler<ObtenerDetalleQuery, Result<AdminUsuarioListItemDto, DomainError>>
{
    public Task<Result<AdminUsuarioListItemDto, DomainError>> Handle(ObtenerDetalleQuery request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return service.ObtenerDetalleAsync(request.Id);
    }
}
