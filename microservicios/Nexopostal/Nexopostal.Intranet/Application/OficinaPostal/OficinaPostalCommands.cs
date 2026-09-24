using MediatR;
using Nexopostal.Shared.Cqrs;
using Nexopostal.Intranet.DTOs;
using Nexopostal.Intranet.Models;
using Nexopostal.Intranet.Repositories;
using Nexopostal.Intranet.Services;

namespace Nexopostal.Intranet.Application.OficinaPostal;
public sealed record ActualizarOficinaAdminCommand(string IdentityUserId, AdminActualizarOficinaDto Dto) : ICommand<(bool Ok, string? Error, MiOficinaInfoDto? Resultado)>;
public sealed class ActualizarOficinaAdminCommandHandler(IOficinaPostalCommands service) : IRequestHandler<ActualizarOficinaAdminCommand, (bool Ok, string? Error, MiOficinaInfoDto? Resultado)>
{
    public Task<(bool Ok, string? Error, MiOficinaInfoDto? Resultado)> Handle(ActualizarOficinaAdminCommand request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return service.ActualizarOficinaAdmin(request.IdentityUserId, request.Dto);
    }
}
