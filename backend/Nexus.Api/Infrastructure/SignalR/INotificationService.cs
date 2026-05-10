using Nexus.Api.Infrastructure.SignalR.Events;

namespace Nexus.Api.Infrastructure.SignalR;

public interface INotificationService
{
    Task PipelineUpdated(string pipelineId, string action, CancellationToken ct = default);
    Task RunStateChanged(RunStateChanged evt, CancellationToken ct = default);
    Task StepStateChanged(StepStateChanged evt, CancellationToken ct = default);
    Task AlertRaised(AlertRaised evt, CancellationToken ct = default);
}
