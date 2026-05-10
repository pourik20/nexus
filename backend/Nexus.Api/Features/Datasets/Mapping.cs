using Nexus.Api.Domain;

namespace Nexus.Api.Features.Datasets;

public static class DatasetMapping
{
    public static DatasetDto ToDto(this Dataset d) =>
        new(d.Id, d.Name, d.Description, d.Owner, d.SchemaVersion, d.CreatedAt, d.UpdatedAt);
}
