using System.Text.Json;
using MongoDB.Bson;
using Nexus.Api.Domain;

namespace Nexus.Api.Features.Runs;

public static class RunMapping
{
    public static JobRunDto ToDto(this JobRun r, IEnumerable<JobRunStep>? steps = null) =>
        new(
            r.Id,
            r.PipelineId,
            r.PipelineVersion,
            BsonToJson(r.PipelineVersionConfigSnapshot),
            r.Status,
            r.StartedAt,
            r.FinishedAt,
            r.RecordsProcessed,
            r.ErrorMessage,
            r.CreatedAt,
            steps?.OrderBy(s => s.Order).Select(s => s.ToDto()).ToList());

    public static JobRunStepDto ToDto(this JobRunStep s) =>
        new(s.Id, s.RunId, s.Name, s.Order, s.Status, s.StartedAt, s.FinishedAt);

    private static JsonElement BsonToJson(BsonDocument doc)
    {
        var json = doc.ToJson(new MongoDB.Bson.IO.JsonWriterSettings { OutputMode = MongoDB.Bson.IO.JsonOutputMode.RelaxedExtendedJson });
        using var d = JsonDocument.Parse(json);
        return d.RootElement.Clone();
    }
}
