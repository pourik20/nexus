using MongoDB.Driver;
using Nexus.Api.Domain;

namespace Nexus.Api.Features.Runs.Get;

public static class GetRunEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/runs/{id}", async (
            string id,
            IMongoCollection<JobRun> runs,
            IMongoCollection<JobRunStep> steps,
            CancellationToken ct) =>
        {
            var run = await runs.Find(r => r.Id == id).FirstOrDefaultAsync(ct);
            if (run is null)
                return Results.Problem(title: "Run not found", statusCode: 404);

            var runSteps = await steps.Find(s => s.RunId == id).SortBy(s => s.Order).ToListAsync(ct);
            return Results.Ok(run.ToDto(runSteps));
        })
        .WithName("GetRun");
    }
}
