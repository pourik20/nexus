namespace Nexus.Api.Features.Datasets;

public record DatasetDto(
    string Id,
    string Name,
    string? Description,
    string Owner,
    int SchemaVersion,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record CreateDatasetRequest(
    string Name,
    string? Description,
    string Owner,
    int SchemaVersion);
