using MediatR;
using Nexopostal.Shared.Cqrs;
using Nexopostal.Intranet.DTOs;
using Nexopostal.Intranet.Models;
using Nexopostal.Intranet.Repositories;
using Nexopostal.Intranet.Services;

namespace Nexopostal.Intranet.Application.Movimiento;
public sealed record CrearMovimientoCommand(CrearMovimientoDto Dto) : ICommand<MovimientoDetalleDto>;
public sealed class CrearMovimientoCommandHandler(IMovimientoCommands service) : IRequestHandler<CrearMovimientoCommand, MovimientoDetalleDto>
{
    public Task<MovimientoDetalleDto> Handle(CrearMovimientoCommand request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return service.CrearMovimiento(request.Dto);
    }
}

public sealed record DespacharMovimientoCommand(int MovimientoId) : ICommand<MovimientoDetalleDto?>;
public sealed class DespacharMovimientoCommandHandler(IMovimientoCommands service) : IRequestHandler<DespacharMovimientoCommand, MovimientoDetalleDto?>
{
    public Task<MovimientoDetalleDto?> Handle(DespacharMovimientoCommand request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return service.DespacharMovimiento(request.MovimientoId);
    }
}

public sealed record RecibirMovimientoCommand(int MovimientoId) : ICommand<MovimientoDetalleDto?>;
public sealed class RecibirMovimientoCommandHandler(IMovimientoCommands service) : IRequestHandler<RecibirMovimientoCommand, MovimientoDetalleDto?>
{
    public Task<MovimientoDetalleDto?> Handle(RecibirMovimientoCommand request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return service.RecibirMovimiento(request.MovimientoId);
    }
}

public sealed record CancelarMovimientoCommand(int MovimientoId) : ICommand<bool>;
public sealed class CancelarMovimientoCommandHandler(IMovimientoCommands service) : IRequestHandler<CancelarMovimientoCommand, bool>
{
    public Task<bool> Handle(CancelarMovimientoCommand request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return service.CancelarMovimiento(request.MovimientoId);
    }
}
