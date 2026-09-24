using MediatR;
using Nexopostal.Shared.Cqrs;
using Nexopostal.Ciudadano.DTOs;
using Nexopostal.Ciudadano.Models;
using Nexopostal.Ciudadano.Repositories;
using Nexopostal.Ciudadano.Services;

namespace Nexopostal.Ciudadano.Application.AdminEnvios;
public sealed record CambiarEstadoCommand(string NumeroSeguimiento, CambiarEstadoEnvioDto Dto, string? AdminUserId) : ICommand<(AdminEnvioDetalleDto? envio, string? error)>;
public sealed class CambiarEstadoCommandHandler(IAdminEnviosCommands service) : IRequestHandler<CambiarEstadoCommand, (AdminEnvioDetalleDto? envio, string? error)>
{
    public Task<(AdminEnvioDetalleDto? envio, string? error)> Handle(CambiarEstadoCommand request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return service.CambiarEstadoAsync(request.NumeroSeguimiento, request.Dto, request.AdminUserId);
    }
}

public sealed record AnularCommand(string NumeroSeguimiento, AccionEnvioDto Dto, string? AdminUserId) : ICommand<(AdminEnvioDetalleDto? envio, string? error)>;
public sealed class AnularCommandHandler(IAdminEnviosCommands service) : IRequestHandler<AnularCommand, (AdminEnvioDetalleDto? envio, string? error)>
{
    public Task<(AdminEnvioDetalleDto? envio, string? error)> Handle(AnularCommand request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return service.AnularAsync(request.NumeroSeguimiento, request.Dto, request.AdminUserId);
    }
}

public sealed record ReabrirCommand(string NumeroSeguimiento, AccionEnvioDto Dto, string? AdminUserId) : ICommand<(AdminEnvioDetalleDto? envio, string? error)>;
public sealed class ReabrirCommandHandler(IAdminEnviosCommands service) : IRequestHandler<ReabrirCommand, (AdminEnvioDetalleDto? envio, string? error)>
{
    public Task<(AdminEnvioDetalleDto? envio, string? error)> Handle(ReabrirCommand request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return service.ReabrirAsync(request.NumeroSeguimiento, request.Dto, request.AdminUserId);
    }
}
