using MongoDB.Driver;
using Nexus.Api.Domain;

namespace Nexus.Api.Features.Datasets.Get;

public static class GetDatasetEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/datasets/{id}", async (string id, IMongoCollection<Dataset> col, CancellationToken ct) =>
        {
            var d = await col.Find(x => x.Id == id).FirstOrDefaultAsync(ct);
            return d is null
                ? Results.Problem(title: "Dataset not found", statusCode: 404)
                : Results.Ok(d.ToDto());
        })
        .WithName("GetDataset");
    }
}
