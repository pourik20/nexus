using MongoDB.Bson.Serialization.Attributes;

namespace Nexus.Api.Domain;

public class Dataset
{
    [BsonId]
    public string Id { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public string Owner { get; set; } = default!;
    public int SchemaVersion { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
