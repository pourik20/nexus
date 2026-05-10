using MongoDB.Driver;
using Nexus.Api.Domain;

namespace Nexus.Api.Features.Datasets.List;

public static class ListDatasetsEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/datasets", async (IMongoCollection<Dataset> col, CancellationToken ct) =>
        {
            var items = await col.Find(FilterDefinition<Dataset>.Empty)
                .SortByDescending(d => d.CreatedAt)
                .ToListAsync(ct);
            return Results.Ok(items.Select(d => d.ToDto()));
        })
        .WithName("ListDatasets");
    }
}
