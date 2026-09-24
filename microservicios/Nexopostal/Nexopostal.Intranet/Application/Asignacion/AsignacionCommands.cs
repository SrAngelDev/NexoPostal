using MediatR;
using Nexopostal.Shared.Cqrs;
using Nexopostal.Intranet.DTOs;
using Nexopostal.Intranet.Models;
using Nexopostal.Intranet.Repositories;
using Nexopostal.Intranet.Services;

namespace Nexopostal.Intranet.Application.Asignacion;
public sealed record CrearAsignacionCommand(CrearAsignacionDto Dto, int OperarioLogisticoId, int CtaId) : ICommand<AsignacionDetalleDto>;
public sealed class CrearAsignacionCommandHandler(IAsignacionCommands service) : IRequestHandler<CrearAsignacionCommand, AsignacionDetalleDto>
{
    public Task<AsignacionDetalleDto> Handle(CrearAsignacionCommand request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return service.CrearAsignacion(request.Dto, request.OperarioLogisticoId, request.CtaId);
    }
}

public sealed record CrearAsignacionOficinaCommand(string NumeroExpedicion, int OperarioOficinaId, TipoTarea TipoTarea, int? OficinaJsonId, string? OficinaNombre, bool EsUrgente = false, int? CreadorOperarioCtaId = null, string? Observaciones = null) : ICommand<AsignacionDetalleDto>;
public sealed class CrearAsignacionOficinaCommandHandler(IAsignacionCommands service) : IRequestHandler<CrearAsignacionOficinaCommand, AsignacionDetalleDto>
{
    public Task<AsignacionDetalleDto> Handle(CrearAsignacionOficinaCommand request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return service.CrearAsignacionOficina(request.NumeroExpedicion, request.OperarioOficinaId, request.TipoTarea, request.OficinaJsonId, request.OficinaNombre, request.EsUrgente, request.CreadorOperarioCtaId, request.Observaciones);
    }
}

public sealed record IniciarTareaCommand(int AsignacionId, int OperarioId) : ICommand<AsignacionDetalleDto?>;
public sealed class IniciarTareaCommandHandler(IAsignacionCommands service) : IRequestHandler<IniciarTareaCommand, AsignacionDetalleDto?>
{
    public Task<AsignacionDetalleDto?> Handle(IniciarTareaCommand request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return service.IniciarTarea(request.AsignacionId, request.OperarioId);
    }
}

public sealed record CompletarTareaCommand(int AsignacionId, int OperarioId) : ICommand<AsignacionDetalleDto?>;
public sealed class CompletarTareaCommandHandler(IAsignacionCommands service) : IRequestHandler<CompletarTareaCommand, AsignacionDetalleDto?>
{
    public Task<AsignacionDetalleDto?> Handle(CompletarTareaCommand request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return service.CompletarTarea(request.AsignacionId, request.OperarioId);
    }
}

public sealed record CancelarTareaCommand(int AsignacionId, int OperarioLogisticoId) : ICommand<bool>;
public sealed class CancelarTareaCommandHandler(IAsignacionCommands service) : IRequestHandler<CancelarTareaCommand, bool>
{
    public Task<bool> Handle(CancelarTareaCommand request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return service.CancelarTarea(request.AsignacionId, request.OperarioLogisticoId);
    }
}

public sealed record ReasignarTareaCommand(int AsignacionId, int NuevoOperarioId, int SupervisorOperarioId) : ICommand<AsignacionDetalleDto?>;
public sealed class ReasignarTareaCommandHandler(IAsignacionCommands service) : IRequestHandler<ReasignarTareaCommand, AsignacionDetalleDto?>
{
    public Task<AsignacionDetalleDto?> Handle(ReasignarTareaCommand request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return service.ReasignarTarea(request.AsignacionId, request.NuevoOperarioId, request.SupervisorOperarioId);
    }
}
