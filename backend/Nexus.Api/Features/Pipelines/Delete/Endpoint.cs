using MongoDB.Driver;
using Nexus.Api.Domain;

namespace Nexus.Api.Features.Pipelines.Delete;

public static class DeletePipelineEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapDelete("/pipelines/{id}", async (string id, IMongoCollection<Pipeline> col, CancellationToken ct) =>
        {
            var result = await col.DeleteOneAsync(p => p.Id == id, ct);
            return result.DeletedCount == 0
                ? Results.Problem(title: "Pipeline not found", statusCode: 404)
                : Results.NoContent();
        })
        .WithName("DeletePipeline");
    }
}
