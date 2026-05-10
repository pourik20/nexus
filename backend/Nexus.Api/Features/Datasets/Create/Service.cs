using MongoDB.Driver;
using Nexus.Api.Domain;

namespace Nexus.Api.Features.Datasets.Create;

public class CreateDatasetService
{
    private readonly IMongoCollection<Dataset> _col;

    public CreateDatasetService(IMongoCollection<Dataset> col) { _col = col; }

    public async Task<Dataset> Handle(CreateDatasetRequest req, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var d = new Dataset
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = req.Name.Trim(),
            Description = req.Description,
            Owner = req.Owner.Trim(),
            SchemaVersion = req.SchemaVersion,
            CreatedAt = now,
            UpdatedAt = now,
        };

        try
        {
            await _col.InsertOneAsync(d, cancellationToken: ct);
        }
        catch (MongoWriteException mwx) when (mwx.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            throw new DomainException($"A dataset named '{d.Name}' already exists.", nameof(req.Name));
        }

        return d;
    }
}
