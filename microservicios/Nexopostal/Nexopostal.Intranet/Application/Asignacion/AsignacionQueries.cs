using MediatR;
using Nexopostal.Shared.Cqrs;
using Nexopostal.Intranet.DTOs;
using Nexopostal.Intranet.Models;
using Nexopostal.Intranet.Repositories;
using Nexopostal.Intranet.Services;

namespace Nexopostal.Intranet.Application.Asignacion;
public sealed record ObtenerTareasPendientesQuery(int OperarioId) : IQuery<List<AsignacionResumenDto>>;
public sealed class ObtenerTareasPendientesQueryHandler(IAsignacionQueries service) : IRequestHandler<ObtenerTareasPendientesQuery, List<AsignacionResumenDto>>
{
    public Task<List<AsignacionResumenDto>> Handle(ObtenerTareasPendientesQuery request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return service.ObtenerTareasPendientes(request.OperarioId);
    }
}

public sealed record ObtenerTareasEnProgresoQuery(int OperarioId) : IQuery<List<AsignacionResumenDto>>;
public sealed class ObtenerTareasEnProgresoQueryHandler(IAsignacionQueries service) : IRequestHandler<ObtenerTareasEnProgresoQuery, List<AsignacionResumenDto>>
{
    public Task<List<AsignacionResumenDto>> Handle(ObtenerTareasEnProgresoQuery request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return service.ObtenerTareasEnProgreso(request.OperarioId);
    }
}

public sealed record ObtenerTareasCompletadasQuery(int OperarioId, int Max = 50) : IQuery<List<AsignacionResumenDto>>;
public sealed class ObtenerTareasCompletadasQueryHandler(IAsignacionQueries service) : IRequestHandler<ObtenerTareasCompletadasQuery, List<AsignacionResumenDto>>
{
    public Task<List<AsignacionResumenDto>> Handle(ObtenerTareasCompletadasQuery request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return service.ObtenerTareasCompletadas(request.OperarioId, request.Max);
    }
}

public sealed record ObtenerAsignacionesCtaQuery(int CtaId, EstadoTarea? FiltroEstado = null) : IQuery<List<AsignacionResumenDto>>;
public sealed class ObtenerAsignacionesCtaQueryHandler(IAsignacionQueries service) : IRequestHandler<ObtenerAsignacionesCtaQuery, List<AsignacionResumenDto>>
{
    public Task<List<AsignacionResumenDto>> Handle(ObtenerAsignacionesCtaQuery request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return service.ObtenerAsignacionesCta(request.CtaId, request.FiltroEstado);
    }
}

public sealed record ObtenerDetalleQuery(int AsignacionId) : IQuery<AsignacionDetalleDto?>;
public sealed class ObtenerDetalleQueryHandler(IAsignacionQueries service) : IRequestHandler<ObtenerDetalleQuery, AsignacionDetalleDto?>
{
    public Task<AsignacionDetalleDto?> Handle(ObtenerDetalleQuery request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return service.ObtenerDetalle(request.AsignacionId);
    }
}

public sealed record BuscarEnMisTareasQuery(int OperarioId, string Codigo) : IQuery<AsignacionResumenDto?>;
public sealed class BuscarEnMisTareasQueryHandler(IAsignacionQueries service) : IRequestHandler<BuscarEnMisTareasQuery, AsignacionResumenDto?>
{
    public Task<AsignacionResumenDto?> Handle(BuscarEnMisTareasQuery request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return service.BuscarEnMisTareasAsync(request.OperarioId, request.Codigo);
    }
}

public sealed record ObtenerTareasPendientesOficinaQuery(int OperarioOficinaId) : IQuery<List<AsignacionResumenDto>>;
public sealed class ObtenerTareasPendientesOficinaQueryHandler(IAsignacionQueries service) : IRequestHandler<ObtenerTareasPendientesOficinaQuery, List<AsignacionResumenDto>>
{
    public Task<List<AsignacionResumenDto>> Handle(ObtenerTareasPendientesOficinaQuery request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return service.ObtenerTareasPendientesOficina(request.OperarioOficinaId);
    }
}

public sealed record ObtenerTareasEnProgresoOficinaQuery(int OperarioOficinaId) : IQuery<List<AsignacionResumenDto>>;
public sealed class ObtenerTareasEnProgresoOficinaQueryHandler(IAsignacionQueries service) : IRequestHandler<ObtenerTareasEnProgresoOficinaQuery, List<AsignacionResumenDto>>
{
    public Task<List<AsignacionResumenDto>> Handle(ObtenerTareasEnProgresoOficinaQuery request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return service.ObtenerTareasEnProgresoOficina(request.OperarioOficinaId);
    }
}

public sealed record ObtenerTareasCompletadasOficinaQuery(int OperarioOficinaId, int Max = 50) : IQuery<List<AsignacionResumenDto>>;
public sealed class ObtenerTareasCompletadasOficinaQueryHandler(IAsignacionQueries service) : IRequestHandler<ObtenerTareasCompletadasOficinaQuery, List<AsignacionResumenDto>>
{
    public Task<List<AsignacionResumenDto>> Handle(ObtenerTareasCompletadasOficinaQuery request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return service.ObtenerTareasCompletadasOficina(request.OperarioOficinaId, request.Max);
    }
}

public sealed record BuscarEnMisTareasOficinaQuery(int OperarioOficinaId, string Codigo) : IQuery<AsignacionResumenDto?>;
public sealed class BuscarEnMisTareasOficinaQueryHandler(IAsignacionQueries service) : IRequestHandler<BuscarEnMisTareasOficinaQuery, AsignacionResumenDto?>
{
    public Task<AsignacionResumenDto?> Handle(BuscarEnMisTareasOficinaQuery request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return service.BuscarEnMisTareasOficinaAsync(request.OperarioOficinaId, request.Codigo);
    }
}
