using MongoDB.Driver;
using Nexus.Api.Domain;

namespace Nexus.Api.Features.Runs.List;

public static class ListRunsEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/runs", async (
            string? pipelineId,
            string? status,
            DateTime? startedAfter,
            DateTime? startedBefore,
            IMongoCollection<JobRun> runs,
            CancellationToken ct) =>
        {
            var fb = Builders<JobRun>.Filter;
            var filters = new List<FilterDefinition<JobRun>>();
            if (!string.IsNullOrEmpty(pipelineId)) filters.Add(fb.Eq(r => r.PipelineId, pipelineId));
            if (!string.IsNullOrEmpty(status)) filters.Add(fb.Eq(r => r.Status, status));
            if (startedAfter.HasValue) filters.Add(fb.Gte(r => r.StartedAt, startedAfter.Value));
            if (startedBefore.HasValue) filters.Add(fb.Lte(r => r.StartedAt, startedBefore.Value));

            var filter = filters.Count == 0 ? FilterDefinition<JobRun>.Empty : fb.And(filters);

            var items = await runs.Find(filter)
                .SortByDescending(r => r.StartedAt)
                .Limit(500)
                .ToListAsync(ct);

            return Results.Ok(items.Select(r => r.ToDto()));
        })
        .WithName("ListRuns");
    }
}
