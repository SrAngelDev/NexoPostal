using MediatR;
using Nexopostal.Shared.Cqrs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Nexopostal.Reparto.Data;
using Nexopostal.Reparto.DTOs;
using Nexopostal.Reparto.Hubs;
using Nexopostal.Reparto.Models;
using Nexopostal.Reparto.Services;

namespace Nexopostal.Reparto.Application.BandejaPendientes;
public sealed record ListarPendientesQuery(int? CtaId, bool IncluirAsignados = false) : IQuery<List<PaqueteBandejaDto>>;
public sealed class ListarPendientesQueryHandler(IBandejaPendientesQueries service) : IRequestHandler<ListarPendientesQuery, List<PaqueteBandejaDto>>
{
    public Task<List<PaqueteBandejaDto>> Handle(ListarPendientesQuery request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return service.ListarPendientesAsync(request.CtaId, request.IncluirAsignados);
    }
}
