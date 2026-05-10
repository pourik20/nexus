using MongoDB.Driver;
using Nexus.Api.Domain;

namespace Nexus.Api.Infrastructure.Mongo;

public class IndexInitializerHostedService : IHostedService
{
    private readonly IServiceProvider _sp;
    private readonly ILogger<IndexInitializerHostedService> _log;

    public IndexInitializerHostedService(IServiceProvider sp, ILogger<IndexInitializerHostedService> log)
    {
        _sp = sp;
        _log = log;
    }

    public async Task StartAsync(CancellationToken ct)
    {
        using var scope = _sp.CreateScope();
        var datasets = scope.ServiceProvider.GetRequiredService<IMongoCollection<Dataset>>();
        var pipelines = scope.ServiceProvider.GetRequiredService<IMongoCollection<Pipeline>>();
        var jobRuns = scope.ServiceProvider.GetRequiredService<IMongoCollection<JobRun>>();
        var jobRunSteps = scope.ServiceProvider.GetRequiredService<IMongoCollection<JobRunStep>>();

        await datasets.Indexes.CreateOneAsync(
            new CreateIndexModel<Dataset>(
                Builders<Dataset>.IndexKeys.Ascending(d => d.Name),
                new CreateIndexOptions { Unique = true, Name = "ux_datasets_name" }),
            cancellationToken: ct);

        await pipelines.Indexes.CreateOneAsync(
            new CreateIndexModel<Pipeline>(
                Builders<Pipeline>.IndexKeys.Ascending(p => p.DatasetId),
                new CreateIndexOptions { Name = "ix_pipelines_datasetId" }),
            cancellationToken: ct);

        await jobRuns.Indexes.CreateOneAsync(
            new CreateIndexModel<JobRun>(
                Builders<JobRun>.IndexKeys
                    .Ascending(r => r.PipelineId)
                    .Descending(r => r.StartedAt),
                new CreateIndexOptions { Name = "ix_jobRuns_pipelineId_startedAt" }),
            cancellationToken: ct);

        await jobRunSteps.Indexes.CreateOneAsync(
            new CreateIndexModel<JobRunStep>(
                Builders<JobRunStep>.IndexKeys.Ascending(s => s.RunId),
                new CreateIndexOptions { Name = "ix_jobRunSteps_runId" }),
            cancellationToken: ct);

        _log.LogInformation("Mongo indexes ensured.");
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}
