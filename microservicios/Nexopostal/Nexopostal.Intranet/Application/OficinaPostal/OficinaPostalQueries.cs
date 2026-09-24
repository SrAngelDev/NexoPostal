using MediatR;
using Nexopostal.Shared.Cqrs;
using Nexopostal.Intranet.DTOs;
using Nexopostal.Intranet.Models;
using Nexopostal.Intranet.Repositories;
using Nexopostal.Intranet.Services;

namespace Nexopostal.Intranet.Application.OficinaPostal;
public sealed record ResolverOficinaPorCpQuery(string CodigoPostal) : IQuery<ResolverOficinaCtaResponseDto?>;
public sealed class ResolverOficinaPorCpQueryHandler(IOficinaPostalQueries service) : IRequestHandler<ResolverOficinaPorCpQuery, ResolverOficinaCtaResponseDto?>
{
    public Task<ResolverOficinaCtaResponseDto?> Handle(ResolverOficinaPorCpQuery request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return service.ResolverOficinaPorCp(request.CodigoPostal);
    }
}

public sealed record ObtenerOperariosOficinaQuery(int OficinaJsonId) : IQuery<List<OperarioOficinaResumenDto>>;
public sealed class ObtenerOperariosOficinaQueryHandler(IOficinaPostalQueries service) : IRequestHandler<ObtenerOperariosOficinaQuery, List<OperarioOficinaResumenDto>>
{
    public Task<List<OperarioOficinaResumenDto>> Handle(ObtenerOperariosOficinaQuery request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return service.ObtenerOperariosOficina(request.OficinaJsonId);
    }
}

public sealed record ObtenerOficinasPorCtaQuery(int CtaId) : IQuery<List<OficinaJsonDto>>;
public sealed class ObtenerOficinasPorCtaQueryHandler(IOficinaPostalQueries service) : IRequestHandler<ObtenerOficinasPorCtaQuery, List<OficinaJsonDto>>
{
    public Task<List<OficinaJsonDto>> Handle(ObtenerOficinasPorCtaQuery request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return service.ObtenerOficinasPorCta(request.CtaId);
    }
}

public sealed record ObtenerMiOficinaQuery(string IdentityUserId) : IQuery<MiOficinaInfoDto?>;
public sealed class ObtenerMiOficinaQueryHandler(IOficinaPostalQueries service) : IRequestHandler<ObtenerMiOficinaQuery, MiOficinaInfoDto?>
{
    public Task<MiOficinaInfoDto?> Handle(ObtenerMiOficinaQuery request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return service.ObtenerMiOficina(request.IdentityUserId);
    }
}

public sealed record ObtenerOficinaAdminQuery(string IdentityUserId) : IQuery<MiOficinaInfoDto?>;
public sealed class ObtenerOficinaAdminQueryHandler(IOficinaPostalQueries service) : IRequestHandler<ObtenerOficinaAdminQuery, MiOficinaInfoDto?>
{
    public Task<MiOficinaInfoDto?> Handle(ObtenerOficinaAdminQuery request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return service.ObtenerOficinaAdmin(request.IdentityUserId);
    }
}
