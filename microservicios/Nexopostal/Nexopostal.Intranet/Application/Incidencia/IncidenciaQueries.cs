using MediatR;
using Nexopostal.Shared.Cqrs;
using Nexopostal.Intranet.DTOs;
using Nexopostal.Intranet.Models;
using Nexopostal.Intranet.Repositories;
using Nexopostal.Intranet.Services;

namespace Nexopostal.Intranet.Application.Incidencia;
public sealed record ObtenerIncidenciasCtaQuery(int CtaId, EstadoIncidencia? FiltroEstado = null) : IQuery<List<IncidenciaResumenDto>>;
public sealed class ObtenerIncidenciasCtaQueryHandler(IIncidenciaQueries service) : IRequestHandler<ObtenerIncidenciasCtaQuery, List<IncidenciaResumenDto>>
{
    public Task<List<IncidenciaResumenDto>> Handle(ObtenerIncidenciasCtaQuery request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return service.ObtenerIncidenciasCta(request.CtaId, request.FiltroEstado);
    }
}

public sealed record ObtenerIncidenciasGlobalesQuery(EstadoIncidencia? FiltroEstado = null, int? CtaId = null, TipoIncidencia? Tipo = null) : IQuery<List<IncidenciaResumenDto>>;
public sealed class ObtenerIncidenciasGlobalesQueryHandler(IIncidenciaQueries service) : IRequestHandler<ObtenerIncidenciasGlobalesQuery, List<IncidenciaResumenDto>>
{
    public Task<List<IncidenciaResumenDto>> Handle(ObtenerIncidenciasGlobalesQuery request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return service.ObtenerIncidenciasGlobales(request.FiltroEstado, request.CtaId, request.Tipo);
    }
}

public sealed record ObtenerDetalleQuery(int IncidenciaId) : IQuery<IncidenciaDetalleDto?>;
public sealed class ObtenerDetalleQueryHandler(IIncidenciaQueries service) : IRequestHandler<ObtenerDetalleQuery, IncidenciaDetalleDto?>
{
    public Task<IncidenciaDetalleDto?> Handle(ObtenerDetalleQuery request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return service.ObtenerDetalle(request.IncidenciaId);
    }
}

public sealed record ObtenerIncidenciasPaqueteQuery(string NumeroExpedicion) : IQuery<List<IncidenciaResumenDto>>;
public sealed class ObtenerIncidenciasPaqueteQueryHandler(IIncidenciaQueries service) : IRequestHandler<ObtenerIncidenciasPaqueteQuery, List<IncidenciaResumenDto>>
{
    public Task<List<IncidenciaResumenDto>> Handle(ObtenerIncidenciasPaqueteQuery request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return service.ObtenerIncidenciasPaquete(request.NumeroExpedicion);
    }
}
