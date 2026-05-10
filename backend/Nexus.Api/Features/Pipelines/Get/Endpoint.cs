using MongoDB.Driver;
using Nexus.Api.Domain;

namespace Nexus.Api.Features.Pipelines.Get;

public static class GetPipelineEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/pipelines/{id}", async (string id, IMongoCollection<Pipeline> col, CancellationToken ct) =>
        {
            var p = await col.Find(x => x.Id == id).FirstOrDefaultAsync(ct);
            return p is null
                ? Results.Problem(title: "Pipeline not found", statusCode: 404)
                : Results.Ok(p.ToDto());
        })
        .WithName("GetPipeline");
    }
}
