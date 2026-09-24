using MediatR;
using Nexopostal.Shared.Cqrs;
using Nexopostal.Intranet.DTOs;
using Nexopostal.Intranet.Models;
using Nexopostal.Intranet.Repositories;
using Nexopostal.Intranet.Services;

namespace Nexopostal.Intranet.Application.Clasificacion;
public sealed record ResolverCtaDestinoQuery(string CodigoPostal) : IQuery<ResolverCtaResponseDto?>;
public sealed class ResolverCtaDestinoQueryHandler(IClasificacionQueries service) : IRequestHandler<ResolverCtaDestinoQuery, ResolverCtaResponseDto?>
{
    public Task<ResolverCtaResponseDto?> Handle(ResolverCtaDestinoQuery request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return service.ResolverCtaDestino(request.CodigoPostal);
    }
}

public sealed record DeterminarTipoTransporteQuery(int CtaOrigenId, int CtaDestinoId, bool EsUrgente) : IQuery<TipoTransporte>;
public sealed class DeterminarTipoTransporteQueryHandler(IClasificacionQueries service) : IRequestHandler<DeterminarTipoTransporteQuery, TipoTransporte>
{
    public Task<TipoTransporte> Handle(DeterminarTipoTransporteQuery request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return service.DeterminarTipoTransporte(request.CtaOrigenId, request.CtaDestinoId, request.EsUrgente);
    }
}

public sealed record ObtenerTodosCtasQuery() : IQuery<List<CtaResumenDto>>;
public sealed class ObtenerTodosCtasQueryHandler(IClasificacionQueries service) : IRequestHandler<ObtenerTodosCtasQuery, List<CtaResumenDto>>
{
    public Task<List<CtaResumenDto>> Handle(ObtenerTodosCtasQuery request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return service.ObtenerTodosCtas();
    }
}

public sealed record ObtenerCtaDetalleQuery(int CtaId) : IQuery<CtaDetalleDto?>;
public sealed class ObtenerCtaDetalleQueryHandler(IClasificacionQueries service) : IRequestHandler<ObtenerCtaDetalleQuery, CtaDetalleDto?>
{
    public Task<CtaDetalleDto?> Handle(ObtenerCtaDetalleQuery request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return service.ObtenerCtaDetalle(request.CtaId);
    }
}

public sealed record ObtenerDashboardCtaQuery(int CtaId) : IQuery<DashboardCtaDto?>;
public sealed class ObtenerDashboardCtaQueryHandler(IClasificacionQueries service) : IRequestHandler<ObtenerDashboardCtaQuery, DashboardCtaDto?>
{
    public Task<DashboardCtaDto?> Handle(ObtenerDashboardCtaQuery request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return service.ObtenerDashboardCta(request.CtaId);
    }
}

public sealed record ObtenerDashboardAdminQuery() : IQuery<DashboardAdminDto>;
public sealed class ObtenerDashboardAdminQueryHandler(IClasificacionQueries service) : IRequestHandler<ObtenerDashboardAdminQuery, DashboardAdminDto>
{
    public Task<DashboardAdminDto> Handle(ObtenerDashboardAdminQuery request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return service.ObtenerDashboardAdmin();
    }
}
