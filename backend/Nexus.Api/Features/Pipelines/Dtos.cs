using System.Text.Json;

namespace Nexus.Api.Features.Pipelines;

public record PipelineVersionDto(
    string Id,
    int Version,
    JsonElement Config,
    bool IsCurrent,
    DateTime CreatedAt);

public record PipelineDto(
    string Id,
    string DatasetId,
    string Name,
    string? Description,
    string? Schedule,
    bool Active,
    IReadOnlyList<PipelineVersionDto> Versions,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? LastRunAt,
    string? LastRunStatus);

public record CreatePipelineRequest(
    string DatasetId,
    string Name,
    string? Description,
    string? Schedule,
    bool? Active);

public record UpdatePipelineRequest(
    string? Name,
    string? Description,
    string? Schedule,
    bool? Active);

public record AddVersionRequest(
    JsonElement? Config);
