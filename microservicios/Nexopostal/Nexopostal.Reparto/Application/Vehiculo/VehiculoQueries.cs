using MediatR;
using Nexopostal.Shared.Cqrs;
using Microsoft.EntityFrameworkCore;
using Nexopostal.Reparto.Data;
using Nexopostal.Reparto.DTOs;
using Nexopostal.Reparto.Models;
using Nexopostal.Reparto.Repositories;
using Nexopostal.Reparto.Services;

namespace Nexopostal.Reparto.Application.Vehiculos;
public sealed record ListarQuery(bool IncluirInactivos = false, int? OficinaJsonId = null, int? RepartidorId = null) : IQuery<List<Vehiculo>>;
public sealed class ListarQueryHandler(IVehiculoQueries service) : IRequestHandler<ListarQuery, List<Vehiculo>>
{
    public Task<List<Vehiculo>> Handle(ListarQuery request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return service.ListarAsync(request.IncluirInactivos, request.OficinaJsonId, request.RepartidorId);
    }
}

public sealed record ObtenerQuery(int Id) : IQuery<Vehiculo?>;
public sealed class ObtenerQueryHandler(IVehiculoQueries service) : IRequestHandler<ObtenerQuery, Vehiculo?>
{
    public Task<Vehiculo?> Handle(ObtenerQuery request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return service.ObtenerAsync(request.Id);
    }
}
