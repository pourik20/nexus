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

        _log.LogInformation("Mongo indexes ensured.");
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}
