using MongoDB.Driver;
using Nexus.Api.Domain;
using Nexus.Api.Infrastructure.Validation;

namespace Nexus.Api.Features.Pipelines.Update;

public static class UpdatePipelineEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPatch("/pipelines/{id}", async (string id, UpdatePipelineRequest req, IMongoCollection<Pipeline> col, CancellationToken ct) =>
        {
            var updates = new List<UpdateDefinition<Pipeline>>();
            if (req.Name is not null) updates.Add(Builders<Pipeline>.Update.Set(p => p.Name, req.Name.Trim()));
            if (req.Description is not null) updates.Add(Builders<Pipeline>.Update.Set(p => p.Description, req.Description));
            if (req.Schedule is not null) updates.Add(Builders<Pipeline>.Update.Set(p => p.Schedule, req.Schedule));
            if (req.Active is not null) updates.Add(Builders<Pipeline>.Update.Set(p => p.Active, req.Active.Value));

            if (updates.Count == 0)
            {
                var existing = await col.Find(p => p.Id == id).FirstOrDefaultAsync(ct);
                return existing is null
                    ? Results.Problem(title: "Pipeline not found", statusCode: 404)
                    : Results.Ok(existing.ToDto());
            }

            updates.Add(Builders<Pipeline>.Update.Set(p => p.UpdatedAt, DateTime.UtcNow));

            var updated = await col.FindOneAndUpdateAsync<Pipeline>(
                Builders<Pipeline>.Filter.Eq(p => p.Id, id),
                Builders<Pipeline>.Update.Combine(updates),
                new FindOneAndUpdateOptions<Pipeline> { ReturnDocument = ReturnDocument.After },
                ct);

            return updated is null
                ? Results.Problem(title: "Pipeline not found", statusCode: 404)
                : Results.Ok(updated.ToDto());
        })
        .AddEndpointFilter<ValidationFilter<UpdatePipelineRequest>>()
        .WithName("UpdatePipeline");
    }
}
