namespace Nexus.Api.Infrastructure.SignalR.Events;

public record StepStateChanged(
    string RunId,
    string PipelineId,
    string StepName,
    string Status,
    DateTime? StartedAt = null,
    DateTime? FinishedAt = null);
