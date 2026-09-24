using MediatR;
using Nexopostal.Shared.Cqrs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Nexopostal.Reparto.Data;
using Nexopostal.Reparto.DTOs;
using Nexopostal.Reparto.Hubs;
using Nexopostal.Reparto.Models;
using Nexopostal.Reparto.Services;

namespace Nexopostal.Reparto.Application.BandejaPendientes;
public sealed record RegistrarPaqueteCommand(RegistrarPaqueteBandejaRequestDto Dto) : ICommand<RegistrarPaqueteBandejaResponseDto>;
public sealed class RegistrarPaqueteCommandHandler(IBandejaPendientesCommands service) : IRequestHandler<RegistrarPaqueteCommand, RegistrarPaqueteBandejaResponseDto>
{
    public Task<RegistrarPaqueteBandejaResponseDto> Handle(RegistrarPaqueteCommand request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return service.RegistrarPaqueteAsync(request.Dto);
    }
}

public sealed record AsignarARutaCommand(int PendienteId, AsignarPendienteARutaDto Dto, string? AsignadoPorIdentityUserId) : ICommand<(PaqueteBandejaDto? Pendiente, EntregaPaqueteDto? Entrega, string? Error)>;
public sealed class AsignarARutaCommandHandler(IBandejaPendientesCommands service) : IRequestHandler<AsignarARutaCommand, (PaqueteBandejaDto? Pendiente, EntregaPaqueteDto? Entrega, string? Error)>
{
    public Task<(PaqueteBandejaDto? Pendiente, EntregaPaqueteDto? Entrega, string? Error)> Handle(AsignarARutaCommand request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return service.AsignarARutaAsync(request.PendienteId, request.Dto, request.AsignadoPorIdentityUserId);
    }
}
