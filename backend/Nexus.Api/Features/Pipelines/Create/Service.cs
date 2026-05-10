using MongoDB.Bson;
using MongoDB.Driver;
using Nexus.Api.Domain;
using Nexus.Api.Infrastructure.SignalR;

namespace Nexus.Api.Features.Pipelines.Create;

public class CreatePipelineService
{
    private readonly IMongoCollection<Pipeline> _pipelines;
    private readonly IMongoCollection<Dataset> _datasets;
    private readonly INotificationService _notifications;

    public CreatePipelineService(
        IMongoCollection<Pipeline> pipelines,
        IMongoCollection<Dataset> datasets,
        INotificationService notifications)
    {
        _pipelines = pipelines;
        _datasets = datasets;
        _notifications = notifications;
    }

    public async Task<Pipeline> Handle(CreatePipelineRequest req, CancellationToken ct)
    {
        var datasetExists = await _datasets.Find(d => d.Id == req.DatasetId).AnyAsync(ct);
        if (!datasetExists)
            throw new DomainException($"Dataset '{req.DatasetId}' does not exist.", nameof(req.DatasetId));

        var now = DateTime.UtcNow;
        var p = new Pipeline
        {
            Id = Guid.NewGuid().ToString("N"),
            DatasetId = req.DatasetId,
            Name = req.Name.Trim(),
            Description = req.Description,
            Schedule = req.Schedule,
            Active = req.Active ?? true,
            Versions = new List<PipelineVersion>
            {
                new()
                {
                    Id = Guid.NewGuid().ToString("N"),
                    Version = 1,
                    Config = new BsonDocument(),
                    IsCurrent = true,
                    CreatedAt = now,
                },
            },
            CreatedAt = now,
            UpdatedAt = now,
        };

        await _pipelines.InsertOneAsync(p, cancellationToken: ct);
        await _notifications.PipelineUpdated(p.Id, "created", ct);
        return p;
    }
}
