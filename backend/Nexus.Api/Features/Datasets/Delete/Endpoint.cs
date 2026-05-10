using MongoDB.Driver;
using Nexus.Api.Domain;

namespace Nexus.Api.Features.Datasets.Delete;

public static class DeleteDatasetEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapDelete("/datasets/{id}", async (string id, IMongoCollection<Dataset> col, IServiceProvider sp, CancellationToken ct) =>
        {
            // Pipeline-reference guard is added in slice #3 via IDatasetReferenceGuard.
            var guard = sp.GetService<IDatasetReferenceGuard>();
            if (guard is not null) await guard.EnsureNotReferenced(id, ct);

            var result = await col.DeleteOneAsync(x => x.Id == id, ct);
            return result.DeletedCount == 0
                ? Results.Problem(title: "Dataset not found", statusCode: 404)
                : Results.NoContent();
        })
        .WithName("DeleteDataset");
    }
}

public interface IDatasetReferenceGuard
{
    Task EnsureNotReferenced(string datasetId, CancellationToken ct);
}
