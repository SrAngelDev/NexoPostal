using MediatR;
using Nexopostal.Shared.Cqrs;
using Nexopostal.Intranet.DTOs;
using Nexopostal.Intranet.Models;
using Nexopostal.Intranet.Repositories;
using Nexopostal.Intranet.Services;

namespace Nexopostal.Intranet.Application.Incidencia;
public sealed record CrearIncidenciaCommand(CrearIncidenciaDto Dto, int OperarioJefeId, int CtaId) : ICommand<IncidenciaDetalleDto>;
public sealed class CrearIncidenciaCommandHandler(IIncidenciaCommands service) : IRequestHandler<CrearIncidenciaCommand, IncidenciaDetalleDto>
{
    public Task<IncidenciaDetalleDto> Handle(CrearIncidenciaCommand request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return service.CrearIncidencia(request.Dto, request.OperarioJefeId, request.CtaId);
    }
}

public sealed record ActualizarIncidenciaCommand(int IncidenciaId, ActualizarIncidenciaDto Dto) : ICommand<IncidenciaDetalleDto?>;
public sealed class ActualizarIncidenciaCommandHandler(IIncidenciaCommands service) : IRequestHandler<ActualizarIncidenciaCommand, IncidenciaDetalleDto?>
{
    public Task<IncidenciaDetalleDto?> Handle(ActualizarIncidenciaCommand request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return service.ActualizarIncidencia(request.IncidenciaId, request.Dto);
    }
}
