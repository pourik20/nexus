namespace Nexus.Api.Features.Alerts;

public record CreateAlertRuleRequest(
    string PipelineId,
    string Name,
    string Expression,
    string? Severity,
    bool Enabled);

public record UpdateAlertRuleRequest(
    string? Name,
    string? Expression,
    string? Severity,
    bool? Enabled);

public record AlertRuleDto(
    string Id,
    string PipelineId,
    string Name,
    string Expression,
    string Severity,
    bool Enabled,
    DateTime CreatedAt);

public record AlertEventDto(
    string Id,
    string RuleId,
    string RunId,
    string PipelineId,
    string Message,
    string Severity,
    DateTime CreatedAt,
    DateTime? AcknowledgedAt);

public record AlertEventDetailDto(
    string Id,
    string RuleId,
    string RunId,
    string PipelineId,
    string Message,
    string Severity,
    DateTime CreatedAt,
    DateTime? AcknowledgedAt,
    AlertRuleDto Rule);
