using Microsoft.AspNetCore.SignalR;
using Nexopostal.Intranet.Hubs;
using Nexopostal.Intranet.Services;

namespace Nexopostal.Intranet.Application.Broadcast;
/// <summary>Contrato de operaciones que modifican estado para Broadcast.</summary>
public interface IBroadcastCommands
{
    Task BroadcastAsync(BroadcastRequest req);
}
