using MediatR;
using Nexopostal.Shared.Cqrs;
using Nexopostal.Intranet.DTOs;
using Nexopostal.Intranet.Models;
using Nexopostal.Intranet.Repositories;
using Nexopostal.Intranet.Services;

namespace Nexopostal.Intranet.Application.Historial;
public sealed record RegistrarEventoCommand(CrearHistorialEventoDto Dto) : ICommand<HistorialEventoInternoDto>;
public sealed class RegistrarEventoCommandHandler(IHistorialCommands service) : IRequestHandler<RegistrarEventoCommand, HistorialEventoInternoDto>
{
    public Task<HistorialEventoInternoDto> Handle(RegistrarEventoCommand request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return service.RegistrarEvento(request.Dto);
    }
}
