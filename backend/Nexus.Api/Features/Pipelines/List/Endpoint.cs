using MongoDB.Driver;
using Nexus.Api.Domain;

namespace Nexus.Api.Features.Pipelines.List;

public static class ListPipelinesEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/pipelines", async (
            string? datasetId,
            IMongoCollection<Pipeline> col,
            IMongoCollection<JobRun> runs,
            CancellationToken ct) =>
        {
            var filter = string.IsNullOrEmpty(datasetId)
                ? FilterDefinition<Pipeline>.Empty
                : Builders<Pipeline>.Filter.Eq(p => p.DatasetId, datasetId);

            var items = await col.Find(filter)
                .SortByDescending(p => p.CreatedAt)
                .ToListAsync(ct);

            if (items.Count == 0) return Results.Ok(Array.Empty<PipelineDto>());

            var ids = items.Select(p => p.Id).ToList();
            var pipelineRuns = await runs.Find(Builders<JobRun>.Filter.In(r => r.PipelineId, ids))
                .SortByDescending(r => r.StartedAt)
                .ToListAsync(ct);

            var lastByPipeline = pipelineRuns
                .GroupBy(r => r.PipelineId)
                .ToDictionary(g => g.Key, g => g.First());

            return Results.Ok(items.Select(p =>
            {
                lastByPipeline.TryGetValue(p.Id, out var last);
                return p.ToDto(last?.StartedAt, last?.Status);
            }));
        })
        .WithName("ListPipelines");
    }
}
