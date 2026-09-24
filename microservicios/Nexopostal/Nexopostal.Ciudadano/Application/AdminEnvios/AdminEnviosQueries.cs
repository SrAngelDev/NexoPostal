using MediatR;
using Nexopostal.Shared.Cqrs;
using Nexopostal.Ciudadano.DTOs;
using Nexopostal.Ciudadano.Models;
using Nexopostal.Ciudadano.Repositories;
using Nexopostal.Ciudadano.Services;

namespace Nexopostal.Ciudadano.Application.AdminEnvios;
public sealed record ListarQuery(EstadoEnvio? Estado, EstadoInterno? EstadoInterno, DateTime? FechaDesde, DateTime? FechaHasta, string? Q, string? CodigoPostal, bool? Pagado, int Limit) : IQuery<List<AdminEnvioListItemDto>>;
public sealed class ListarQueryHandler(IAdminEnviosQueries service) : IRequestHandler<ListarQuery, List<AdminEnvioListItemDto>>
{
    public Task<List<AdminEnvioListItemDto>> Handle(ListarQuery request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return service.ListarAsync(request.Estado, request.EstadoInterno, request.FechaDesde, request.FechaHasta, request.Q, request.CodigoPostal, request.Pagado, request.Limit);
    }
}

public sealed record ObtenerQuery(string NumeroSeguimiento) : IQuery<AdminEnvioDetalleDto?>;
public sealed class ObtenerQueryHandler(IAdminEnviosQueries service) : IRequestHandler<ObtenerQuery, AdminEnvioDetalleDto?>
{
    public Task<AdminEnvioDetalleDto?> Handle(ObtenerQuery request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return service.ObtenerAsync(request.NumeroSeguimiento);
    }
}
