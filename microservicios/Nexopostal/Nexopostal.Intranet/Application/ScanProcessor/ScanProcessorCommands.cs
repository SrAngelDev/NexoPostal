using MediatR;
using Nexopostal.Shared.Cqrs;
using Nexopostal.Intranet.DTOs;
using Nexopostal.Intranet.Models;
using Nexopostal.Intranet.Repositories;
using Nexopostal.Intranet.Services;

namespace Nexopostal.Intranet.Application.ScanProcessor;
public sealed record ProcesarEscaneoCommand(ScanRequestDto Request) : ICommand<ScanResultDto>;
public sealed class ProcesarEscaneoCommandHandler(IScanProcessorCommands service) : IRequestHandler<ProcesarEscaneoCommand, ScanResultDto>
{
    public Task<ScanResultDto> Handle(ProcesarEscaneoCommand request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return service.ProcesarEscaneo(request.Request);
    }
}

public sealed record ProcesarLoteCommand(ScanBatchRequestDto Request) : ICommand<ScanBatchResultDto>;
public sealed class ProcesarLoteCommandHandler(IScanProcessorCommands service) : IRequestHandler<ProcesarLoteCommand, ScanBatchResultDto>
{
    public Task<ScanBatchResultDto> Handle(ProcesarLoteCommand request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return service.ProcesarLote(request.Request);
    }
}
