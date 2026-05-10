namespace Nexus.Api.Infrastructure.SignalR.Events;

public record RunStateChanged(
    string RunId,
    string PipelineId,
    string Status,
    DateTime? StartedAt = null,
    DateTime? FinishedAt = null,
    string? ErrorMessage = null);
