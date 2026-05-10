using MongoDB.Driver;
using Nexus.Api.Domain;

namespace Nexus.Api.Features.Runs.Patch;

public static class PatchRunEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPatch("/runs/{id}", async (
            string id,
            PatchRunRequest req,
            IMongoCollection<JobRun> runs,
            IMongoCollection<JobRunStep> steps,
            RunStateService state,
            CancellationToken ct) =>
        {
            var run = await runs.Find(r => r.Id == id).FirstOrDefaultAsync(ct);
            if (run is null)
                return Results.Problem(title: "Run not found", statusCode: 404);

            if (req.Status != RunStatus.Success && req.Status != RunStatus.Failed)
                throw new DomainException(
                    $"Status must be '{RunStatus.Success}' or '{RunStatus.Failed}'.",
                    nameof(req.Status));

            await state.Complete(id, success: req.Status == RunStatus.Success, errorMessage: req.ErrorMessage, ct: ct);

            var updated = await runs.Find(r => r.Id == id).FirstOrDefaultAsync(ct);
            var runSteps = await steps.Find(s => s.RunId == id).SortBy(s => s.Order).ToListAsync(ct);
            return Results.Ok(updated!.ToDto(runSteps));
        })
        .WithName("PatchRun");
    }
}
