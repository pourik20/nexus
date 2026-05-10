using MongoDB.Bson.Serialization.Attributes;

namespace Nexus.Api.Domain;

public static class AlertRuleType
{
    public const string RuntimeExceeds = "RuntimeExceeds";
    public const string RunFailed = "RunFailed";
}

public class AlertRule
{
    [BsonId]
    public string Id { get; set; } = default!;
    public string PipelineId { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Type { get; set; } = default!;
    
    // Stored as ISO 8601 duration string, parsed to TimeSpan where needed, or just kept as string.
    // "runtimeThreshold?: ISO 8601 duration string"
    public string? RuntimeThreshold { get; set; } 
    
    public string Expression { get; set; } = default!;
    public bool Enabled { get; set; }
    public DateTime CreatedAt { get; set; }
}
