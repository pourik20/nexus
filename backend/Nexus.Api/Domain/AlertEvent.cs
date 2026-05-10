using MongoDB.Bson.Serialization.Attributes;

namespace Nexus.Api.Domain;

public class AlertEvent
{
    [BsonId]
    public string Id { get; set; } = default!;
    public string RuleId { get; set; } = default!;
    public string RunId { get; set; } = default!;
    public string PipelineId { get; set; } = default!;
    public string Message { get; set; } = default!;
    public string Severity { get; set; } = default!; // "info" | "warning" | "error"
    public DateTime CreatedAt { get; set; }
}
