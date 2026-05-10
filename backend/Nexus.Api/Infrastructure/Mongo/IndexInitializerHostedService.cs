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

        await datasets.Indexes.CreateOneAsync(
            new CreateIndexModel<Dataset>(
                Builders<Dataset>.IndexKeys.Ascending(d => d.Name),
                new CreateIndexOptions { Unique = true, Name = "ux_datasets_name" }),
            cancellationToken: ct);

        _log.LogInformation("Mongo indexes ensured.");
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}
