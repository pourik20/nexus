using Microsoft.AspNetCore.SignalR;
using Nexus.Api.Infrastructure.SignalR.Events;

namespace Nexus.Api.Infrastructure.SignalR;

public class NotificationService : INotificationService
{
    private readonly IHubContext<ControlHub> _hub;

    public NotificationService(IHubContext<ControlHub> hub)
    {
        _hub = hub;
    }

    public Task PipelineUpdated(string pipelineId, string action, CancellationToken ct = default)
        => _hub.Clients.All.SendAsync(nameof(Events.PipelineUpdated), new PipelineUpdated(pipelineId, action), ct);

    public Task RunStateChanged(RunStateChanged evt, CancellationToken ct = default)
        => _hub.Clients.All.SendAsync(nameof(Events.RunStateChanged), evt, ct);

    public Task StepStateChanged(StepStateChanged evt, CancellationToken ct = default)
        => _hub.Clients.All.SendAsync(nameof(Events.StepStateChanged), evt, ct);

    public Task AlertRaised(AlertRaised evt, CancellationToken ct = default)
        => _hub.Clients.All.SendAsync(nameof(Events.AlertRaised), evt, ct);
}
