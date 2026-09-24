using MediatR;
using Nexopostal.Shared.Cqrs;
using Nexopostal.Intranet.DTOs;
using Nexopostal.Intranet.Models;
using Nexopostal.Intranet.Repositories;
using Nexopostal.Intranet.Services;

namespace Nexopostal.Intranet.Application.Admision;
public sealed record AdmitirPaqueteCommand(AdmisionPaqueteDto Dto) : ICommand<AdmisionPaqueteResponseDto>;
public sealed class AdmitirPaqueteCommandHandler(IAdmisionCommands service) : IRequestHandler<AdmitirPaqueteCommand, AdmisionPaqueteResponseDto>
{
    public Task<AdmisionPaqueteResponseDto> Handle(AdmitirPaqueteCommand request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return service.AdmitirPaquete(request.Dto);
    }
}
