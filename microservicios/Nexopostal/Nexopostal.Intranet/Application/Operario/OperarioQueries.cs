using MediatR;
using Nexopostal.Shared.Cqrs;
using Microsoft.AspNetCore.SignalR;
using Nexopostal.Intranet.DTOs;
using Nexopostal.Intranet.Hubs;
using Nexopostal.Intranet.Models;
using Nexopostal.Intranet.Repositories;
using Nexopostal.Intranet.Services;

namespace Nexopostal.Intranet.Application.Operario;
public sealed record ObtenerPorIdentityUserIdQuery(string IdentityUserId) : IQuery<OperarioCta?>;
public sealed class ObtenerPorIdentityUserIdQueryHandler(IOperarioQueries service) : IRequestHandler<ObtenerPorIdentityUserIdQuery, OperarioCta?>
{
    public Task<OperarioCta?> Handle(ObtenerPorIdentityUserIdQuery request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return service.ObtenerPorIdentityUserId(request.IdentityUserId);
    }
}

public sealed record ObtenerTodosPorIdentityUserIdQuery(string IdentityUserId) : IQuery<List<OperarioCta>>;
public sealed class ObtenerTodosPorIdentityUserIdQueryHandler(IOperarioQueries service) : IRequestHandler<ObtenerTodosPorIdentityUserIdQuery, List<OperarioCta>>
{
    public Task<List<OperarioCta>> Handle(ObtenerTodosPorIdentityUserIdQuery request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return service.ObtenerTodosPorIdentityUserId(request.IdentityUserId);
    }
}

public sealed record ObtenerMiCtaInfoQuery(string IdentityUserId) : IQuery<MiCtaInfoDto?>;
public sealed class ObtenerMiCtaInfoQueryHandler(IOperarioQueries service) : IRequestHandler<ObtenerMiCtaInfoQuery, MiCtaInfoDto?>
{
    public Task<MiCtaInfoDto?> Handle(ObtenerMiCtaInfoQuery request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return service.ObtenerMiCtaInfo(request.IdentityUserId);
    }
}

public sealed record ObtenerMisCtasInfoQuery(string IdentityUserId) : IQuery<MisCtasInfoDto?>;
public sealed class ObtenerMisCtasInfoQueryHandler(IOperarioQueries service) : IRequestHandler<ObtenerMisCtasInfoQuery, MisCtasInfoDto?>
{
    public Task<MisCtasInfoDto?> Handle(ObtenerMisCtasInfoQuery request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return service.ObtenerMisCtasInfo(request.IdentityUserId);
    }
}

public sealed record ObtenerOperariosCtaQuery(int CtaId) : IQuery<List<OperarioResumenDto>>;
public sealed class ObtenerOperariosCtaQueryHandler(IOperarioQueries service) : IRequestHandler<ObtenerOperariosCtaQuery, List<OperarioResumenDto>>
{
    public Task<List<OperarioResumenDto>> Handle(ObtenerOperariosCtaQuery request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return service.ObtenerOperariosCta(request.CtaId);
    }
}

public sealed record ObtenerDetalleQuery(int OperarioId) : IQuery<OperarioDetalleDto?>;
public sealed class ObtenerDetalleQueryHandler(IOperarioQueries service) : IRequestHandler<ObtenerDetalleQuery, OperarioDetalleDto?>
{
    public Task<OperarioDetalleDto?> Handle(ObtenerDetalleQuery request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return service.ObtenerDetalle(request.OperarioId);
    }
}

public sealed record ObtenerDetalleAdminPorIdentityUserIdQuery(string IdentityUserId) : IQuery<AdminOperarioDetalleDto?>;
public sealed class ObtenerDetalleAdminPorIdentityUserIdQueryHandler(IOperarioQueries service) : IRequestHandler<ObtenerDetalleAdminPorIdentityUserIdQuery, AdminOperarioDetalleDto?>
{
    public Task<AdminOperarioDetalleDto?> Handle(ObtenerDetalleAdminPorIdentityUserIdQuery request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return service.ObtenerDetalleAdminPorIdentityUserId(request.IdentityUserId);
    }
}
