using MongoDB.Bson;
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
            var filter = Builders<Pipeline>.Filter.And(
                Builders<Pipeline>.Filter.Eq(p => p.Id, id),
                Builders<Pipeline>.Filter.ElemMatch(p => p.Versions, v => v.Id == versionId));

            var update = Builders<Pipeline>.Update
                .Set("Versions.$[target].Active", true)
                .Set("Versions.$[other].Active", false)
                .Set(p => p.UpdatedAt, DateTime.UtcNow);

            var arrayFilters = new List<ArrayFilterDefinition>
            {
                new BsonDocumentArrayFilterDefinition<BsonDocument>(
                    new BsonDocument("target.Id", versionId)),
                new BsonDocumentArrayFilterDefinition<BsonDocument>(
                    new BsonDocument("other.Id", new BsonDocument("$ne", versionId))),
            };

            var updated = await col.FindOneAndUpdateAsync<Pipeline>(
                filter,
                update,
                new FindOneAndUpdateOptions<Pipeline>
                {
                    ReturnDocument = ReturnDocument.After,
                    ArrayFilters = arrayFilters,
                },
                ct);

            if (updated is null)
            {
                var pipelineExists = await col.Find(p => p.Id == id).AnyAsync(ct);
                return pipelineExists
                    ? Results.Problem(title: "Version not found", statusCode: 404)
                    : Results.Problem(title: "Pipeline not found", statusCode: 404);
            }

            await notifications.PipelineUpdated(updated.Id, "updated", ct);
            return Results.Ok(updated.ToDto());
        })
        .WithName("ActivatePipelineVersion");
    }
}
