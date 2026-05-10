using System.Text.Json.Nodes;
using Nexus.Api.Domain;

namespace Nexus.Api.Features.Alerts;

public static class EvaluationContextBuilder
{
    public static JsonNode Build(JobRun run, Pipeline pipeline)
    {
        var finishedAt = run.FinishedAt ?? DateTime.UtcNow;
        var runtimeSeconds = (int)(finishedAt - run.StartedAt).TotalSeconds;

        var context = new JsonObject
        {
            ["runtime"] = runtimeSeconds,
            ["status"] = run.Status,
            ["recordsProcessed"] = run.RecordsProcessed,
            ["finishedAt"] = finishedAt.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            ["pipeline"] = new JsonObject
            {
                ["name"] = pipeline.Name,
                ["schedule"] = pipeline.Schedule,
                ["version"] = run.PipelineVersion
            }
        };

        return context;
    }
}
