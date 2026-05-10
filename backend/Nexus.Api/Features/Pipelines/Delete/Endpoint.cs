using MongoDB.Driver;
using Nexus.Api.Domain;
using Nexus.Api.Infrastructure.SignalR;

namespace Nexus.Api.Features.Pipelines.Delete;

public static class DeletePipelineEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapDelete("/pipelines/{id}", async (
            string id,
            IMongoCollection<Pipeline> col,
            INotificationService notifications,
            CancellationToken ct) =>
        {
            var result = await col.DeleteOneAsync(p => p.Id == id, ct);
            if (result.DeletedCount == 0)
                return Results.Problem(title: "Pipeline not found", statusCode: 404);

            await notifications.PipelineUpdated(id, "deleted", ct);
            return Results.NoContent();
        })
        .WithName("DeletePipeline");
    }
}
