using MediatR;
using Nexopostal.Shared.Cqrs;
using Nexopostal.Intranet.DTOs;
using Nexopostal.Intranet.Models;
using Nexopostal.Intranet.Repositories;
using Nexopostal.Intranet.Services;

namespace Nexopostal.Intranet.Application.AdminCta;
public sealed record CrearCtaCommand(CrearCtaDto Dto) : ICommand<(CtaDetalleDto? cta, string? error)>;
public sealed class CrearCtaCommandHandler(IAdminCtaCommands service) : IRequestHandler<CrearCtaCommand, (CtaDetalleDto? cta, string? error)>
{
    public Task<(CtaDetalleDto? cta, string? error)> Handle(CrearCtaCommand request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return service.CrearCta(request.Dto);
    }
}

public sealed record EditarCtaCommand(int Id, EditarCtaDto Dto) : ICommand<(CtaDetalleDto? cta, string? error)>;
public sealed class EditarCtaCommandHandler(IAdminCtaCommands service) : IRequestHandler<EditarCtaCommand, (CtaDetalleDto? cta, string? error)>
{
    public Task<(CtaDetalleDto? cta, string? error)> Handle(EditarCtaCommand request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return service.EditarCta(request.Id, request.Dto);
    }
}

public sealed record DesactivarCtaCommand(int Id) : ICommand<(bool ok, string? error)>;
public sealed class DesactivarCtaCommandHandler(IAdminCtaCommands service) : IRequestHandler<DesactivarCtaCommand, (bool ok, string? error)>
{
    public Task<(bool ok, string? error)> Handle(DesactivarCtaCommand request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return service.DesactivarCta(request.Id);
    }
}

public sealed record ReactivarCtaCommand(int Id) : ICommand<(bool ok, string? error)>;
public sealed class ReactivarCtaCommandHandler(IAdminCtaCommands service) : IRequestHandler<ReactivarCtaCommand, (bool ok, string? error)>
{
    public Task<(bool ok, string? error)> Handle(ReactivarCtaCommand request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return service.ReactivarCta(request.Id);
    }
}
