using System.Text.Json;

namespace Nexus.Api.Features.Runs;

public record JobRunStepDto(
    string Id,
    string RunId,
    string Name,
    int Order,
    string Status,
    DateTime? StartedAt,
    DateTime? FinishedAt);

public record JobRunDto(
    string Id,
    string PipelineId,
    int PipelineVersion,
    JsonElement PipelineVersionConfigSnapshot,
    string Status,
    DateTime StartedAt,
    DateTime? FinishedAt,
    int RecordsProcessed,
    string? ErrorMessage,
    DateTime CreatedAt,
    IReadOnlyList<JobRunStepDto>? Steps);

public record PatchRunRequest(string Status, string? ErrorMessage);
