using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Nexus.Api.Domain;

public class Pipeline
{
    [BsonId]
    public string Id { get; set; } = default!;
    public string DatasetId { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public string? Schedule { get; set; }
    public bool Active { get; set; }
    public List<PipelineVersion> Versions { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class PipelineVersion
{
    public string Id { get; set; } = default!;
    public int Version { get; set; }
    public BsonDocument Config { get; set; } = new();
    public bool IsCurrent { get; set; }
    public DateTime CreatedAt { get; set; }
}
