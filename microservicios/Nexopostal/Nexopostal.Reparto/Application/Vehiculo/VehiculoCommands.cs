using MediatR;
using Nexopostal.Shared.Cqrs;
using Microsoft.EntityFrameworkCore;
using Nexopostal.Reparto.Data;
using Nexopostal.Reparto.DTOs;
using Nexopostal.Reparto.Models;
using Nexopostal.Reparto.Repositories;
using Nexopostal.Reparto.Services;

namespace Nexopostal.Reparto.Application.Vehiculos;
public sealed record CrearCommand(CrearVehiculoDto Dto, string? UserId) : ICommand<(Vehiculo? vehiculo, string? error)>;
public sealed class CrearCommandHandler(IVehiculoCommands service) : IRequestHandler<CrearCommand, (Vehiculo? vehiculo, string? error)>
{
    public Task<(Vehiculo? vehiculo, string? error)> Handle(CrearCommand request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return service.CrearAsync(request.Dto, request.UserId);
    }
}

public sealed record ActualizarCommand(int Id, ActualizarVehiculoDto Dto, string? UserId) : ICommand<(Vehiculo? vehiculo, string? error)>;
public sealed class ActualizarCommandHandler(IVehiculoCommands service) : IRequestHandler<ActualizarCommand, (Vehiculo? vehiculo, string? error)>
{
    public Task<(Vehiculo? vehiculo, string? error)> Handle(ActualizarCommand request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return service.ActualizarAsync(request.Id, request.Dto, request.UserId);
    }
}

public sealed record DesactivarCommand(int Id, string? UserId) : ICommand<(bool ok, string? error)>;
public sealed class DesactivarCommandHandler(IVehiculoCommands service) : IRequestHandler<DesactivarCommand, (bool ok, string? error)>
{
    public Task<(bool ok, string? error)> Handle(DesactivarCommand request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return service.DesactivarAsync(request.Id, request.UserId);
    }
}

public sealed record ReactivarCommand(int Id, string? UserId) : ICommand<(bool ok, string? error)>;
public sealed class ReactivarCommandHandler(IVehiculoCommands service) : IRequestHandler<ReactivarCommand, (bool ok, string? error)>
{
    public Task<(bool ok, string? error)> Handle(ReactivarCommand request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return service.ReactivarAsync(request.Id, request.UserId);
    }
}

public sealed record AsignarCommand(int VehiculoId, int? RepartidorId, string? UserId) : ICommand<(Vehiculo? vehiculo, string? error)>;
public sealed class AsignarCommandHandler(IVehiculoCommands service) : IRequestHandler<AsignarCommand, (Vehiculo? vehiculo, string? error)>
{
    public Task<(Vehiculo? vehiculo, string? error)> Handle(AsignarCommand request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return service.AsignarAsync(request.VehiculoId, request.RepartidorId, request.UserId);
    }
}

public sealed record ImportarDesdeRepartidoresCommand(string? UserId) : ICommand<ImportarDesdeRepartidoresResultDto>;
public sealed class ImportarDesdeRepartidoresCommandHandler(IVehiculoCommands service) : IRequestHandler<ImportarDesdeRepartidoresCommand, ImportarDesdeRepartidoresResultDto>
{
    public Task<ImportarDesdeRepartidoresResultDto> Handle(ImportarDesdeRepartidoresCommand request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return service.ImportarDesdeRepartidoresAsync(request.UserId);
    }
}
