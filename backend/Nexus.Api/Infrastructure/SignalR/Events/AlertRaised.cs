namespace Nexus.Api.Infrastructure.SignalR.Events;

public record AlertRaised(
    string AlertId,
    string RuleId,
    string RunId,
    string PipelineId,
    string Message,
    string Severity,
    DateTime CreatedAt);
