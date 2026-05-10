using MongoDB.Driver;
using Nexus.Api.Domain;
using Nexus.Api.Features.Datasets.Delete;

namespace Nexus.Api.Features.Pipelines;

public class PipelineDatasetReferenceGuard : IDatasetReferenceGuard
{
    private readonly IMongoCollection<Pipeline> _pipelines;

    public PipelineDatasetReferenceGuard(IMongoCollection<Pipeline> pipelines)
    {
        _pipelines = pipelines;
    }

    public async Task EnsureNotReferenced(string datasetId, CancellationToken ct)
    {
        var referenced = await _pipelines.Find(p => p.DatasetId == datasetId).AnyAsync(ct);
        if (referenced)
            throw new DomainException(
                "Cannot delete dataset: at least one pipeline references it.",
                "datasetId");
    }
}
