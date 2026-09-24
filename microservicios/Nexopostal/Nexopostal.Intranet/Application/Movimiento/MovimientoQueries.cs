using MediatR;
using Nexopostal.Shared.Cqrs;
using Nexopostal.Intranet.DTOs;
using Nexopostal.Intranet.Models;
using Nexopostal.Intranet.Repositories;
using Nexopostal.Intranet.Services;

namespace Nexopostal.Intranet.Application.Movimiento;
public sealed record ObtenerMovimientosCtaQuery(int CtaId, EstadoMovimiento? FiltroEstado = null) : IQuery<List<MovimientoResumenDto>>;
public sealed class ObtenerMovimientosCtaQueryHandler(IMovimientoQueries service) : IRequestHandler<ObtenerMovimientosCtaQuery, List<MovimientoResumenDto>>
{
    public Task<List<MovimientoResumenDto>> Handle(ObtenerMovimientosCtaQuery request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return service.ObtenerMovimientosCta(request.CtaId, request.FiltroEstado);
    }
}

public sealed record ObtenerMovimientosGlobalesQuery(EstadoMovimiento? FiltroEstado = null, int? CtaOrigenId = null, int? CtaDestinoId = null) : IQuery<List<MovimientoResumenDto>>;
public sealed class ObtenerMovimientosGlobalesQueryHandler(IMovimientoQueries service) : IRequestHandler<ObtenerMovimientosGlobalesQuery, List<MovimientoResumenDto>>
{
    public Task<List<MovimientoResumenDto>> Handle(ObtenerMovimientosGlobalesQuery request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return service.ObtenerMovimientosGlobales(request.FiltroEstado, request.CtaOrigenId, request.CtaDestinoId);
    }
}

public sealed record ObtenerDetalleQuery(int MovimientoId) : IQuery<MovimientoDetalleDto?>;
public sealed class ObtenerDetalleQueryHandler(IMovimientoQueries service) : IRequestHandler<ObtenerDetalleQuery, MovimientoDetalleDto?>
{
    public Task<MovimientoDetalleDto?> Handle(ObtenerDetalleQuery request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return service.ObtenerDetalle(request.MovimientoId);
    }
}

public sealed record ObtenerHistorialPaqueteQuery(string NumeroExpedicion) : IQuery<List<MovimientoResumenDto>>;
public sealed class ObtenerHistorialPaqueteQueryHandler(IMovimientoQueries service) : IRequestHandler<ObtenerHistorialPaqueteQuery, List<MovimientoResumenDto>>
{
    public Task<List<MovimientoResumenDto>> Handle(ObtenerHistorialPaqueteQuery request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return service.ObtenerHistorialPaquete(request.NumeroExpedicion);
    }
}
