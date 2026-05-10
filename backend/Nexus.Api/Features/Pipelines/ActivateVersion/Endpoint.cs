using MongoDB.Driver;
using Nexus.Api.Domain;
using Nexus.Api.Infrastructure.SignalR;

namespace Nexus.Api.Features.Pipelines.ActivateVersion;

public static class ActivateVersionEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPatch("/pipelines/{id}/versions/{versionId}/activate", async (
            string id,
            string versionId,
            IMongoCollection<Pipeline> col,
            INotificationService notifications,
            CancellationToken ct) =>
        {
            var pipeline = await col.Find(p => p.Id == id).FirstOrDefaultAsync(ct);
            if (pipeline is null)
                return Results.Problem(title: "Pipeline not found", statusCode: 404);

            var target = pipeline.Versions.FirstOrDefault(v => v.Id == versionId);
            if (target is null)
                return Results.Problem(title: "Version not found", statusCode: 404);

            foreach (var version in pipeline.Versions)
                version.IsCurrent = version.Id == versionId;

            pipeline.UpdatedAt = DateTime.UtcNow;

            var updated = await col.FindOneAndUpdateAsync(
                Builders<Pipeline>.Filter.Eq(p => p.Id, id),
                Builders<Pipeline>.Update
                    .Set(p => p.Versions, pipeline.Versions)
                    .Set(p => p.UpdatedAt, pipeline.UpdatedAt),
                new FindOneAndUpdateOptions<Pipeline> { ReturnDocument = ReturnDocument.After },
                ct);

            if (updated is null)
                return Results.Problem(title: "Pipeline not found", statusCode: 404);

            await notifications.PipelineUpdated(updated.Id, "updated", ct);
            return Results.Ok(updated.ToDto());
        })
        .WithName("ActivatePipelineVersion");
    }
}
