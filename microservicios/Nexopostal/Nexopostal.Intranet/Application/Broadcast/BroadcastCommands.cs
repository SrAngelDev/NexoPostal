using MediatR;
using Nexopostal.Shared.Cqrs;
using Microsoft.AspNetCore.SignalR;
using Nexopostal.Intranet.Hubs;
using Nexopostal.Intranet.Services;

namespace Nexopostal.Intranet.Application.Broadcast;
public sealed record BroadcastCommand(BroadcastRequest Req) : ICommand<Unit>;
public sealed class BroadcastCommandHandler(IBroadcastCommands service) : IRequestHandler<BroadcastCommand, Unit>
{
    public async Task<Unit> Handle(BroadcastCommand request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await service.BroadcastAsync(request.Req);
        return Unit.Value;
    }
}
