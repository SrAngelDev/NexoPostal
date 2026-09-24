using MediatR;
using Nexopostal.Shared.Cqrs;
using Microsoft.AspNetCore.SignalR;
using Nexopostal.Intranet.DTOs;
using Nexopostal.Intranet.Hubs;
using Nexopostal.Intranet.Models;
using Nexopostal.Intranet.Repositories;
using Nexopostal.Intranet.Services;

namespace Nexopostal.Intranet.Application.Operario;
public sealed record ActualizarCtaAdminCommand(string IdentityUserId, AdminActualizarCtaDto Dto) : ICommand<(bool Ok, string? Error, bool Conflict)>;
public sealed class ActualizarCtaAdminCommandHandler(IOperarioCommands service) : IRequestHandler<ActualizarCtaAdminCommand, (bool Ok, string? Error, bool Conflict)>
{
    public Task<(bool Ok, string? Error, bool Conflict)> Handle(ActualizarCtaAdminCommand request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return service.ActualizarCtaAdmin(request.IdentityUserId, request.Dto);
    }
}

public sealed record CrearOperarioCommand(CrearOperarioDto Dto) : ICommand<OperarioResumenDto>;
public sealed class CrearOperarioCommandHandler(IOperarioCommands service) : IRequestHandler<CrearOperarioCommand, OperarioResumenDto>
{
    public Task<OperarioResumenDto> Handle(CrearOperarioCommand request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return service.CrearOperario(request.Dto);
    }
}

public sealed record DesactivarOperarioCommand(int OperarioId) : ICommand<bool>;
public sealed class DesactivarOperarioCommandHandler(IOperarioCommands service) : IRequestHandler<DesactivarOperarioCommand, bool>
{
    public Task<bool> Handle(DesactivarOperarioCommand request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return service.DesactivarOperario(request.OperarioId);
    }
}

public sealed record ReactivarOperarioCommand(int OperarioId) : ICommand<bool>;
public sealed class ReactivarOperarioCommandHandler(IOperarioCommands service) : IRequestHandler<ReactivarOperarioCommand, bool>
{
    public Task<bool> Handle(ReactivarOperarioCommand request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return service.ReactivarOperario(request.OperarioId);
    }
}
