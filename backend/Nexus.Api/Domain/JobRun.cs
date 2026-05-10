using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Nexus.Api.Domain;

public static class RunStatus
{
    public const string Pending = "pending";
    public const string Running = "running";
    public const string Success = "success";
    public const string Failed = "failed";

    public static bool IsTerminal(string s) => s == Success || s == Failed;
    public static bool IsKnown(string s) => s == Pending || s == Running || s == Success || s == Failed;
}

public static class StepNames
{
    public const string Extract = "extract";
    public const string Transform = "transform";
    public const string Load = "load";

    public static readonly string[] Ordered = new[] { Extract, Transform, Load };

    public static int Order(string name) => Array.IndexOf(Ordered, name);
    public static bool IsKnown(string name) => Order(name) >= 0;
}

public class JobRun
{
    [BsonId]
    public string Id { get; set; } = default!;
    public string PipelineId { get; set; } = default!;
    public int PipelineVersion { get; set; }
    public BsonDocument PipelineVersionConfigSnapshot { get; set; } = new();
    public string Status { get; set; } = RunStatus.Pending;
    public DateTime StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
    public int RecordsProcessed { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class JobRunStep
{
    [BsonId]
    public string Id { get; set; } = default!;
    public string RunId { get; set; } = default!;
    public string Name { get; set; } = default!;
    public int Order { get; set; }
    public string Status { get; set; } = RunStatus.Pending;
    public DateTime? StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
}
