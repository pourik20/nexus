using System.Text.Json;
using MongoDB.Bson;
using Nexus.Api.Domain;

namespace Nexus.Api.Features.Pipelines;

public static class PipelineMapping
{
    public static PipelineDto ToDto(this Pipeline p) =>
        new(
            p.Id,
            p.DatasetId,
            p.Name,
            p.Description,
            p.Schedule,
            p.Active,
            p.Versions
                .OrderBy(v => v.Version)
                .Select(v => v.ToDto())
                .ToList(),
            p.CreatedAt,
            p.UpdatedAt,
            LastRunAt: null,
            LastRunStatus: null);

    public static PipelineVersionDto ToDto(this PipelineVersion v) =>
        new(v.Id, v.Version, BsonToJson(v.Config), v.Active, v.CreatedAt);

    public static BsonDocument JsonToBson(JsonElement el) =>
        BsonDocument.Parse(el.GetRawText());

    private static JsonElement BsonToJson(BsonDocument doc)
    {
        var json = doc.ToJson(new MongoDB.Bson.IO.JsonWriterSettings { OutputMode = MongoDB.Bson.IO.JsonOutputMode.RelaxedExtendedJson });
        using var d = JsonDocument.Parse(json);
        return d.RootElement.Clone();
    }
}
