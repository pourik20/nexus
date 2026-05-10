using MongoDB.Driver;
using Nexus.Api.Domain;

namespace Nexus.Api.Features.Pipelines.List;

public static class ListPipelinesEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/pipelines", async (string? datasetId, IMongoCollection<Pipeline> col, CancellationToken ct) =>
        {
            var filter = string.IsNullOrEmpty(datasetId)
                ? FilterDefinition<Pipeline>.Empty
                : Builders<Pipeline>.Filter.Eq(p => p.DatasetId, datasetId);

            var items = await col.Find(filter)
                .SortByDescending(p => p.CreatedAt)
                .ToListAsync(ct);

            return Results.Ok(items.Select(p => p.ToDto()));
        })
        .WithName("ListPipelines");
    }
}
