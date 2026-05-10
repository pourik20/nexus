namespace Nexus.Api.Features.Alerts;

public record CreateAlertRuleRequest(
    string PipelineId,
    string Name,
    string Type,
    string? RuntimeThreshold,
    string Expression,
    bool Enabled);

public record UpdateAlertRuleRequest(
    string? Name,
    string? Type,
    string? RuntimeThreshold,
    string? Expression,
    bool? Enabled);

public record AlertRuleDto(
    string Id,
    string PipelineId,
    string Name,
    string Type,
    string? RuntimeThreshold,
    string Expression,
    bool Enabled,
    DateTime CreatedAt);

public record AlertEventDto(
    string Id,
    string RuleId,
    string RunId,
    string PipelineId,
    string Message,
    string Severity,
    DateTime CreatedAt);

public record AlertEventDetailDto(
    string Id,
    string RuleId,
    string RunId,
    string PipelineId,
    string Message,
    string Severity,
    DateTime CreatedAt,
    AlertRuleDto Rule);
