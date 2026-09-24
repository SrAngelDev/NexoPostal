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
public sealed record CambiarRolCommand(string Id, Rol NuevoRol, string AdminId) : ICommand<UnitResult<DomainError>>;
public sealed class CambiarRolCommandHandler(IAdminUserCommands service) : IRequestHandler<CambiarRolCommand, UnitResult<DomainError>>
{
    public Task<UnitResult<DomainError>> Handle(CambiarRolCommand request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return service.CambiarRolAsync(request.Id, request.NuevoRol, request.AdminId);
    }
}

public sealed record BloquearCommand(string Id, string AdminId) : ICommand<UnitResult<DomainError>>;
public sealed class BloquearCommandHandler(IAdminUserCommands service) : IRequestHandler<BloquearCommand, UnitResult<DomainError>>
{
    public Task<UnitResult<DomainError>> Handle(BloquearCommand request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return service.BloquearAsync(request.Id, request.AdminId);
    }
}

public sealed record DesbloquearCommand(string Id) : ICommand<UnitResult<DomainError>>;
public sealed class DesbloquearCommandHandler(IAdminUserCommands service) : IRequestHandler<DesbloquearCommand, UnitResult<DomainError>>
{
    public Task<UnitResult<DomainError>> Handle(DesbloquearCommand request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return service.DesbloquearAsync(request.Id);
    }
}

public sealed record ResetPasswordCommand(string Id, string NuevaPassword) : ICommand<UnitResult<DomainError>>;
public sealed class ResetPasswordCommandHandler(IAdminUserCommands service) : IRequestHandler<ResetPasswordCommand, UnitResult<DomainError>>
{
    public Task<UnitResult<DomainError>> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return service.ResetPasswordAsync(request.Id, request.NuevaPassword);
    }
}

public sealed record CrearEmpleadoCommand(AdminCrearEmpleadoDto Dto) : ICommand<Result<AdminUsuarioListItemDto, DomainError>>;
public sealed class CrearEmpleadoCommandHandler(IAdminUserCommands service) : IRequestHandler<CrearEmpleadoCommand, Result<AdminUsuarioListItemDto, DomainError>>
{
    public Task<Result<AdminUsuarioListItemDto, DomainError>> Handle(CrearEmpleadoCommand request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return service.CrearEmpleadoAsync(request.Dto);
    }
}

public sealed record EditarEmpleadoCommand(string Id, AdminEditarEmpleadoDto Dto, string AdminId) : ICommand<Result<AdminUsuarioListItemDto, DomainError>>;
public sealed class EditarEmpleadoCommandHandler(IAdminUserCommands service) : IRequestHandler<EditarEmpleadoCommand, Result<AdminUsuarioListItemDto, DomainError>>
{
    public Task<Result<AdminUsuarioListItemDto, DomainError>> Handle(EditarEmpleadoCommand request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return service.EditarEmpleadoAsync(request.Id, request.Dto, request.AdminId);
    }
}

public sealed record EliminarCommand(string Id, string AdminId) : ICommand<UnitResult<DomainError>>;
public sealed class EliminarCommandHandler(IAdminUserCommands service) : IRequestHandler<EliminarCommand, UnitResult<DomainError>>
{
    public Task<UnitResult<DomainError>> Handle(EliminarCommand request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return service.EliminarAsync(request.Id, request.AdminId);
    }
}

public sealed record RestaurarCommand(string Id) : ICommand<UnitResult<DomainError>>;
public sealed class RestaurarCommandHandler(IAdminUserCommands service) : IRequestHandler<RestaurarCommand, UnitResult<DomainError>>
{
    public Task<UnitResult<DomainError>> Handle(RestaurarCommand request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return service.RestaurarAsync(request.Id);
    }
}
