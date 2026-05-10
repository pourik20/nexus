using MongoDB.Bson.Serialization.Attributes;

namespace Nexus.Api.Domain;

public class AlertRule
{
    [BsonId]
    public string Id { get; set; } = default!;
    public string PipelineId { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Expression { get; set; } = default!;
    public string Severity { get; set; } = "warning";
    public bool Enabled { get; set; }
    public DateTime CreatedAt { get; set; }
}
