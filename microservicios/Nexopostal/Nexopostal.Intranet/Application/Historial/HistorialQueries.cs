using MediatR;
using Nexopostal.Shared.Cqrs;
using Nexopostal.Intranet.DTOs;
using Nexopostal.Intranet.Models;
using Nexopostal.Intranet.Repositories;
using Nexopostal.Intranet.Services;

namespace Nexopostal.Intranet.Application.Historial;
public sealed record ObtenerHistorialInternoQuery(string NumeroExpedicion) : IQuery<List<HistorialEventoInternoDto>>;
public sealed class ObtenerHistorialInternoQueryHandler(IHistorialQueries service) : IRequestHandler<ObtenerHistorialInternoQuery, List<HistorialEventoInternoDto>>
{
    public Task<List<HistorialEventoInternoDto>> Handle(ObtenerHistorialInternoQuery request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return service.ObtenerHistorialInterno(request.NumeroExpedicion);
    }
}

public sealed record ObtenerHistorialPublicoQuery(string NumeroSeguimiento) : IQuery<List<HistorialEventoDto>>;
public sealed class ObtenerHistorialPublicoQueryHandler(IHistorialQueries service) : IRequestHandler<ObtenerHistorialPublicoQuery, List<HistorialEventoDto>>
{
    public Task<List<HistorialEventoDto>> Handle(ObtenerHistorialPublicoQuery request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return service.ObtenerHistorialPublico(request.NumeroSeguimiento);
    }
}

public sealed record ObtenerUltimoEventoQuery(string NumeroExpedicion) : IQuery<HistorialEventoInternoDto?>;
public sealed class ObtenerUltimoEventoQueryHandler(IHistorialQueries service) : IRequestHandler<ObtenerUltimoEventoQuery, HistorialEventoInternoDto?>
{
    public Task<HistorialEventoInternoDto?> Handle(ObtenerUltimoEventoQuery request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return service.ObtenerUltimoEvento(request.NumeroExpedicion);
    }
}
