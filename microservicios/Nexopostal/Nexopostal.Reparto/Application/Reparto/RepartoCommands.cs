using MediatR;
using Nexopostal.Shared.Cqrs;
using Nexopostal.Reparto.DTOs;
using Nexopostal.Reparto.Models;
using Nexopostal.Reparto.Repositories;
using Nexopostal.Reparto.Services;

namespace Nexopostal.Reparto.Application.Reparto;
public sealed record CrearRepartidorCommand(CrearRepartidorDto Dto) : ICommand<RepartidorResumenDto>;
public sealed class CrearRepartidorCommandHandler(IRepartoCommands service) : IRequestHandler<CrearRepartidorCommand, RepartidorResumenDto>
{
    public Task<RepartidorResumenDto> Handle(CrearRepartidorCommand request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return service.CrearRepartidor(request.Dto);
    }
}

public sealed record EditarRepartidorCommand(int Id, EditarRepartidorDto Dto) : ICommand<(RepartidorResumenDto? Repartidor, string? Error)>;
public sealed class EditarRepartidorCommandHandler(IRepartoCommands service) : IRequestHandler<EditarRepartidorCommand, (RepartidorResumenDto? Repartidor, string? Error)>
{
    public Task<(RepartidorResumenDto? Repartidor, string? Error)> Handle(EditarRepartidorCommand request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return service.EditarRepartidor(request.Id, request.Dto);
    }
}

public sealed record DesactivarRepartidorCommand(int Id) : ICommand<(bool Ok, string? Error)>;
public sealed class DesactivarRepartidorCommandHandler(IRepartoCommands service) : IRequestHandler<DesactivarRepartidorCommand, (bool Ok, string? Error)>
{
    public Task<(bool Ok, string? Error)> Handle(DesactivarRepartidorCommand request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return service.DesactivarRepartidor(request.Id);
    }
}

public sealed record ReactivarRepartidorCommand(int Id) : ICommand<(bool Ok, string? Error)>;
public sealed class ReactivarRepartidorCommandHandler(IRepartoCommands service) : IRequestHandler<ReactivarRepartidorCommand, (bool Ok, string? Error)>
{
    public Task<(bool Ok, string? Error)> Handle(ReactivarRepartidorCommand request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return service.ReactivarRepartidor(request.Id);
    }
}

public sealed record CrearRutaCommand(CrearRutaRepartoDto Dto) : ICommand<RutaRepartoDetalleDto>;
public sealed class CrearRutaCommandHandler(IRepartoCommands service) : IRequestHandler<CrearRutaCommand, RutaRepartoDetalleDto>
{
    public Task<RutaRepartoDetalleDto> Handle(CrearRutaCommand request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return service.CrearRuta(request.Dto);
    }
}

public sealed record IniciarRutaCommand(int RutaId) : ICommand<RutaRepartoDetalleDto?>;
public sealed class IniciarRutaCommandHandler(IRepartoCommands service) : IRequestHandler<IniciarRutaCommand, RutaRepartoDetalleDto?>
{
    public Task<RutaRepartoDetalleDto?> Handle(IniciarRutaCommand request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return service.IniciarRuta(request.RutaId);
    }
}

public sealed record FinalizarRutaCommand(int RutaId, string? Observaciones = null) : ICommand<RutaRepartoDetalleDto?>;
public sealed class FinalizarRutaCommandHandler(IRepartoCommands service) : IRequestHandler<FinalizarRutaCommand, RutaRepartoDetalleDto?>
{
    public Task<RutaRepartoDetalleDto?> Handle(FinalizarRutaCommand request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return service.FinalizarRuta(request.RutaId, request.Observaciones);
    }
}

public sealed record CancelarRutaCommand(int RutaId) : ICommand<(bool Ok, string? Error)>;
public sealed class CancelarRutaCommandHandler(IRepartoCommands service) : IRequestHandler<CancelarRutaCommand, (bool Ok, string? Error)>
{
    public Task<(bool Ok, string? Error)> Handle(CancelarRutaCommand request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return service.CancelarRuta(request.RutaId);
    }
}

public sealed record ReactivarRutaCommand(int RutaId) : ICommand<(bool Ok, string? Error)>;
public sealed class ReactivarRutaCommandHandler(IRepartoCommands service) : IRequestHandler<ReactivarRutaCommand, (bool Ok, string? Error)>
{
    public Task<(bool Ok, string? Error)> Handle(ReactivarRutaCommand request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return service.ReactivarRuta(request.RutaId);
    }
}

public sealed record AgregarEntregaARutaCommand(int RutaId, AgregarEntregaDto Dto) : ICommand<EntregaPaqueteDto?>;
public sealed class AgregarEntregaARutaCommandHandler(IRepartoCommands service) : IRequestHandler<AgregarEntregaARutaCommand, EntregaPaqueteDto?>
{
    public Task<EntregaPaqueteDto?> Handle(AgregarEntregaARutaCommand request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return service.AgregarEntregaARuta(request.RutaId, request.Dto);
    }
}

public sealed record RegistrarEntregaCommand(int EntregaId, RegistrarEntregaDto Dto) : ICommand<EntregaPaqueteDto?>;
public sealed class RegistrarEntregaCommandHandler(IRepartoCommands service) : IRequestHandler<RegistrarEntregaCommand, EntregaPaqueteDto?>
{
    public Task<EntregaPaqueteDto?> Handle(RegistrarEntregaCommand request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return service.RegistrarEntrega(request.EntregaId, request.Dto);
    }
}

public sealed record AutoAsignarEntregaDesdeAdmisionCommand(AutoAsignacionEntregaDesdeAdmisionDto Dto) : ICommand<AutoAsignacionEntregaResultDto>;
public sealed class AutoAsignarEntregaDesdeAdmisionCommandHandler(IRepartoCommands service) : IRequestHandler<AutoAsignarEntregaDesdeAdmisionCommand, AutoAsignacionEntregaResultDto>
{
    public Task<AutoAsignacionEntregaResultDto> Handle(AutoAsignarEntregaDesdeAdmisionCommand request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return service.AutoAsignarEntregaDesdeAdmision(request.Dto);
    }
}

public sealed record RegistrarUbicacionRepartidorCommand(string IdentityUserId, double Latitud, double Longitud, int? RutaActivaId) : ICommand<Unit>;
public sealed class RegistrarUbicacionRepartidorCommandHandler(IRepartoCommands service) : IRequestHandler<RegistrarUbicacionRepartidorCommand, Unit>
{
    public async Task<Unit> Handle(RegistrarUbicacionRepartidorCommand request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await service.RegistrarUbicacionRepartidor(request.IdentityUserId, request.Latitud, request.Longitud, request.RutaActivaId);
        return Unit.Value;
    }
}

public sealed record ReasignarEntregaARutaCommand(int EntregaId, int NuevaRutaId) : ICommand<EntregaPaqueteDto?>;
public sealed class ReasignarEntregaARutaCommandHandler(IRepartoCommands service) : IRequestHandler<ReasignarEntregaARutaCommand, EntregaPaqueteDto?>
{
    public Task<EntregaPaqueteDto?> Handle(ReasignarEntregaARutaCommand request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return service.ReasignarEntregaARuta(request.EntregaId, request.NuevaRutaId);
    }
}
