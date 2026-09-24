using MediatR;
using Nexopostal.Shared.Cqrs;
using Nexopostal.Reparto.DTOs;
using Nexopostal.Reparto.Models;
using Nexopostal.Reparto.Repositories;
using Nexopostal.Reparto.Services;

namespace Nexopostal.Reparto.Application.Reparto;
public sealed record ObtenerRepartidoresQuery(int? OficinaJsonId = null, bool IncluirInactivos = false) : IQuery<List<RepartidorResumenDto>>;
public sealed class ObtenerRepartidoresQueryHandler(IRepartoQueries service) : IRequestHandler<ObtenerRepartidoresQuery, List<RepartidorResumenDto>>
{
    public Task<List<RepartidorResumenDto>> Handle(ObtenerRepartidoresQuery request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return service.ObtenerRepartidores(request.OficinaJsonId, request.IncluirInactivos);
    }
}

public sealed record ObtenerRepartidorPorIdentityIdQuery(string IdentityUserId) : IQuery<RepartidorResumenDto?>;
public sealed class ObtenerRepartidorPorIdentityIdQueryHandler(IRepartoQueries service) : IRequestHandler<ObtenerRepartidorPorIdentityIdQuery, RepartidorResumenDto?>
{
    public Task<RepartidorResumenDto?> Handle(ObtenerRepartidorPorIdentityIdQuery request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return service.ObtenerRepartidorPorIdentityId(request.IdentityUserId);
    }
}

public sealed record ObtenerRutasQuery(DateOnly? Fecha = null, int? RepartidorId = null, int? OficinaJsonId = null) : IQuery<List<RutaRepartoResumenDto>>;
public sealed class ObtenerRutasQueryHandler(IRepartoQueries service) : IRequestHandler<ObtenerRutasQuery, List<RutaRepartoResumenDto>>
{
    public Task<List<RutaRepartoResumenDto>> Handle(ObtenerRutasQuery request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return service.ObtenerRutas(request.Fecha, request.RepartidorId, request.OficinaJsonId);
    }
}

public sealed record ObtenerRutaPorIdQuery(int Id) : IQuery<RutaRepartoDetalleDto?>;
public sealed class ObtenerRutaPorIdQueryHandler(IRepartoQueries service) : IRequestHandler<ObtenerRutaPorIdQuery, RutaRepartoDetalleDto?>
{
    public Task<RutaRepartoDetalleDto?> Handle(ObtenerRutaPorIdQuery request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return service.ObtenerRutaPorId(request.Id);
    }
}

public sealed record ObtenerRutaPorCodigoQuery(string Codigo) : IQuery<RutaRepartoDetalleDto?>;
public sealed class ObtenerRutaPorCodigoQueryHandler(IRepartoQueries service) : IRequestHandler<ObtenerRutaPorCodigoQuery, RutaRepartoDetalleDto?>
{
    public Task<RutaRepartoDetalleDto?> Handle(ObtenerRutaPorCodigoQuery request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return service.ObtenerRutaPorCodigo(request.Codigo);
    }
}

public sealed record ObtenerEntregasPorRutaQuery(int RutaId) : IQuery<List<EntregaPaqueteDto>>;
public sealed class ObtenerEntregasPorRutaQueryHandler(IRepartoQueries service) : IRequestHandler<ObtenerEntregasPorRutaQuery, List<EntregaPaqueteDto>>
{
    public Task<List<EntregaPaqueteDto>> Handle(ObtenerEntregasPorRutaQuery request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return service.ObtenerEntregasPorRuta(request.RutaId);
    }
}

public sealed record ObtenerEntregasPorSeguimientoQuery(string NumeroSeguimiento) : IQuery<List<EntregaPaqueteDto>>;
public sealed class ObtenerEntregasPorSeguimientoQueryHandler(IRepartoQueries service) : IRequestHandler<ObtenerEntregasPorSeguimientoQuery, List<EntregaPaqueteDto>>
{
    public Task<List<EntregaPaqueteDto>> Handle(ObtenerEntregasPorSeguimientoQuery request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return service.ObtenerEntregasPorSeguimiento(request.NumeroSeguimiento);
    }
}

public sealed record ObtenerDashboardQuery(int? OficinaJsonId = null) : IQuery<DashboardRepartoDto>;
public sealed class ObtenerDashboardQueryHandler(IRepartoQueries service) : IRequestHandler<ObtenerDashboardQuery, DashboardRepartoDto>
{
    public Task<DashboardRepartoDto> Handle(ObtenerDashboardQuery request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return service.ObtenerDashboard(request.OficinaJsonId);
    }
}

public sealed record ObtenerUbicacionesActivasQuery(int? OficinaJsonId = null, int VentanaMinutos = 10) : IQuery<List<UbicacionActivaDto>>;
public sealed class ObtenerUbicacionesActivasQueryHandler(IRepartoQueries service) : IRequestHandler<ObtenerUbicacionesActivasQuery, List<UbicacionActivaDto>>
{
    public Task<List<UbicacionActivaDto>> Handle(ObtenerUbicacionesActivasQuery request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return service.ObtenerUbicacionesActivas(request.OficinaJsonId, request.VentanaMinutos);
    }
}

public sealed record ObtenerEntregasPendientesAsignacionQuery(int? OficinaJsonId = null) : IQuery<List<EntregaPendienteAsignacionDto>>;
public sealed class ObtenerEntregasPendientesAsignacionQueryHandler(IRepartoQueries service) : IRequestHandler<ObtenerEntregasPendientesAsignacionQuery, List<EntregaPendienteAsignacionDto>>
{
    public Task<List<EntregaPendienteAsignacionDto>> Handle(ObtenerEntregasPendientesAsignacionQuery request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return service.ObtenerEntregasPendientesAsignacion(request.OficinaJsonId);
    }
}
