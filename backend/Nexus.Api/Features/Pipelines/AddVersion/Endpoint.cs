using System.Text.Json;
using MongoDB.Bson;
using MongoDB.Driver;
using Nexus.Api.Domain;
using Nexus.Api.Infrastructure.SignalR;

namespace Nexus.Api.Features.Pipelines.AddVersion;

public static class AddVersionEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/pipelines/{id}/versions", async (
            string id,
            AddVersionRequest req,
            IMongoCollection<Pipeline> col,
            INotificationService notifications,
            CancellationToken ct) =>
        {
            var pipeline = await col.Find(p => p.Id == id).FirstOrDefaultAsync(ct);
            if (pipeline is null)
                return Results.Problem(title: "Pipeline not found", statusCode: 404);

            var nextVersion = pipeline.Versions.Count == 0 ? 1 : pipeline.Versions.Max(v => v.Version) + 1;
            var configBson = req.Config is JsonElement el && el.ValueKind != JsonValueKind.Undefined && el.ValueKind != JsonValueKind.Null
                ? PipelineMapping.JsonToBson(el)
                : new BsonDocument();

            var version = new PipelineVersion
            {
                Id = Guid.NewGuid().ToString("N"),
                Version = nextVersion,
                Config = configBson,
                IsCurrent = false,
                CreatedAt = DateTime.UtcNow,
            };

            var update = Builders<Pipeline>.Update
                .Push(p => p.Versions, version)
                .Set(p => p.UpdatedAt, DateTime.UtcNow);

            var updated = await col.FindOneAndUpdateAsync<Pipeline>(
                Builders<Pipeline>.Filter.Eq(p => p.Id, id),
                update,
                new FindOneAndUpdateOptions<Pipeline> { ReturnDocument = ReturnDocument.After },
                ct);

            if (updated is null)
                return Results.Problem(title: "Pipeline not found", statusCode: 404);

            await notifications.PipelineUpdated(updated.Id, "updated", ct);
            return Results.Created($"/pipelines/{id}/versions/{version.Id}", updated.ToDto());
        })
        .WithName("AddPipelineVersion");
    }
}
