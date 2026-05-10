using MongoDB.Driver;
using Nexus.Api.Domain;

namespace Nexus.Api.Features.Runs.CreateRun;

public static class CreateRunEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/pipelines/{id}/run", async (
            string id,
            IMongoCollection<Pipeline> pipelines,
            IMongoCollection<JobRun> runs,
            IMongoCollection<JobRunStep> steps,
            IRunSimulator simulator,
            CancellationToken ct) =>
        {
            var pipeline = await pipelines.Find(p => p.Id == id).FirstOrDefaultAsync(ct);
            if (pipeline is null)
                return Results.Problem(title: "Pipeline not found", statusCode: 404);
            if (!pipeline.Active)
                throw new DomainException("Pipeline is not active.");

            var activeVersion = pipeline.Versions.FirstOrDefault(v => v.IsCurrent)
                ?? pipeline.Versions.OrderByDescending(v => v.Version).FirstOrDefault();
            if (activeVersion is null)
                throw new DomainException("Pipeline has no versions.");

            var now = DateTime.UtcNow;
            var run = new JobRun
            {
                Id = Guid.NewGuid().ToString("N"),
                PipelineId = pipeline.Id,
                PipelineVersion = activeVersion.Version,
                PipelineVersionConfigSnapshot = activeVersion.Config.DeepClone().AsBsonDocument,
                Status = RunStatus.Pending,
                StartedAt = now,
                RecordsProcessed = 0,
                CreatedAt = now,
            };
            await runs.InsertOneAsync(run, cancellationToken: ct);

            var stepDocs = StepNames.Ordered.Select((name, idx) => new JobRunStep
            {
                Id = Guid.NewGuid().ToString("N"),
                RunId = run.Id,
                Name = name,
                Order = idx,
                Status = RunStatus.Pending,
            }).ToList();
            await steps.InsertManyAsync(stepDocs, cancellationToken: ct);

            simulator.Start(run);

            return Results.Created($"/runs/{run.Id}", run.ToDto(stepDocs));
        })
        .WithName("CreatePipelineRun");
    }
}
