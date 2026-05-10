using MongoDB.Bson;
using MongoDB.Driver;
using Nexus.Api.Domain;

namespace Nexus.Api.Infrastructure.Seed;

/// <summary>
/// Idempotent seeder for the taxi-fleet demo fixture.
/// Drops all collections and re-inserts a fixed dataset.
/// Run via: dotnet run -- --seed
/// </summary>
public class DatabaseSeeder
{
    private readonly IMongoDatabase _db;
    private readonly IMongoCollection<Dataset> _datasets;
    private readonly IMongoCollection<Pipeline> _pipelines;
    private readonly IMongoCollection<AlertRule> _alertRules;
    private readonly IMongoCollection<JobRun> _runs;
    private readonly IMongoCollection<JobRunStep> _steps;
    private readonly IMongoCollection<AlertEvent> _alertEvents;

    public DatabaseSeeder(
        IMongoDatabase db,
        IMongoCollection<Dataset> datasets,
        IMongoCollection<Pipeline> pipelines,
        IMongoCollection<AlertRule> alertRules,
        IMongoCollection<JobRun> runs,
        IMongoCollection<JobRunStep> steps,
        IMongoCollection<AlertEvent> alertEvents)
    {
        _db = db;
        _datasets = datasets;
        _pipelines = pipelines;
        _alertRules = alertRules;
        _runs = runs;
        _steps = steps;
        _alertEvents = alertEvents;
    }

    public async Task SeedAsync()
    {
        Console.WriteLine("=== Nexus Seed CLI ===");
        Console.WriteLine("Dropping collections…");

        await _db.DropCollectionAsync("datasets");
        await _db.DropCollectionAsync("pipelines");
        await _db.DropCollectionAsync("alertRules");
        await _db.DropCollectionAsync("jobRuns");
        await _db.DropCollectionAsync("jobRunSteps");
        await _db.DropCollectionAsync("alertEvents");

        // ── Datasets ──────────────────────────────────────────────────────────
        var ds1 = new Dataset { Id = "ds-telemetry",  Name = "vehicle-telemetry", Description = "Real-time sensor streams from the autonomous taxi fleet.", Owner = "fleet-ops@nexus.internal", SchemaVersion = 3, CreatedAt = Ago(30), UpdatedAt = Ago(5) };
        var ds2 = new Dataset { Id = "ds-charging",   Name = "charging-cycles",   Description = "Battery charge/discharge data per vehicle per session.", Owner = "energy-team@nexus.internal", SchemaVersion = 2, CreatedAt = Ago(25), UpdatedAt = Ago(3) };
        var ds3 = new Dataset { Id = "ds-trips",      Name = "trip-records",      Description = "Per-trip origin/destination, fare, and rating metadata.", Owner = "analytics@nexus.internal", SchemaVersion = 1, CreatedAt = Ago(20), UpdatedAt = Ago(1) };

        await _datasets.InsertManyAsync(new[] { ds1, ds2, ds3 });
        Console.WriteLine($"  datasets:    {3}");

        // ── Pipelines ─────────────────────────────────────────────────────────
        var now = DateTime.UtcNow;

        var p1 = new Pipeline
        {
            Id = "pl-telemetry-ingest", DatasetId = ds1.Id, Name = "Telemetry Ingest",
            Description = "Ingests raw vehicle telemetry, normalises units, loads into warehouse.",
            Schedule = "*/5 * * * *", Active = true,
            CreatedAt = Ago(20), UpdatedAt = Ago(1),
            Versions = new List<PipelineVersion>
            {
                new() { Id = "plv-ti-1", Version = 1, IsCurrent = false, CreatedAt = Ago(20),
                    Config = new BsonDocument { { "source", "kafka://telemetry" }, { "sink", "warehouse://raw_telemetry" } } },
                new() { Id = "plv-ti-2", Version = 2, IsCurrent = true, CreatedAt = Ago(10),
                    Config = new BsonDocument { { "source", "kafka://telemetry" }, { "sink", "warehouse://normalised_telemetry" }, { "normalise", true } } },
            }
        };

        var p2 = new Pipeline
        {
            Id = "pl-charging-etl", DatasetId = ds2.Id, Name = "Charging ETL",
            Description = "Extracts charging events, computes derived metrics, persists to analytics layer.",
            Schedule = "0 * * * *", Active = true,
            CreatedAt = Ago(18), UpdatedAt = Ago(2),
            Versions = new List<PipelineVersion>
            {
                new() { Id = "plv-ce-1", Version = 1, IsCurrent = true, CreatedAt = Ago(18),
                    Config = new BsonDocument { { "source", "postgres://charging_raw" }, { "sink", "warehouse://charging_kpis" } } },
            }
        };

        var p3 = new Pipeline
        {
            Id = "pl-trip-agg", DatasetId = ds3.Id, Name = "Trip Aggregator",
            Description = "Aggregates trip records into daily summaries per zone and vehicle class.",
            Schedule = "0 2 * * *", Active = true,
            CreatedAt = Ago(15), UpdatedAt = Ago(3),
            Versions = new List<PipelineVersion>
            {
                new() { Id = "plv-ta-1", Version = 1, IsCurrent = true, CreatedAt = Ago(15),
                    Config = new BsonDocument { { "source", "warehouse://trip_records" }, { "sink", "warehouse://trip_daily_agg" }, { "groupBy", "zone,vehicle_class" } } },
            }
        };

        var p4 = new Pipeline
        {
            Id = "pl-predictive-maint", DatasetId = ds1.Id, Name = "Predictive Maintenance",
            Description = "Scores component wear risk from telemetry and generates maintenance tickets.",
            Schedule = "0 6 * * *", Active = false,
            CreatedAt = Ago(12), UpdatedAt = Ago(4),
            Versions = new List<PipelineVersion>
            {
                new() { Id = "plv-pm-1", Version = 1, IsCurrent = true, CreatedAt = Ago(12),
                    Config = new BsonDocument { { "model", "wear-score-v3" }, { "threshold", 0.85 }, { "sink", "tickets://maintenance" } } },
            }
        };

        await _pipelines.InsertManyAsync(new[] { p1, p2, p3, p4 });
        Console.WriteLine($"  pipelines:   {4}");

        // ── Alert Rules ───────────────────────────────────────────────────────
        var rule1 = new AlertRule
        {
            Id = "rule-runtime", PipelineId = p1.Id, Name = "Runtime Exceeds 10s",
            Expression = "runtime > 10", Severity = "warning", Enabled = true, CreatedAt = Ago(10)
        };
        var rule2 = new AlertRule
        {
            Id = "rule-failed", PipelineId = p2.Id, Name = "Run Failed",
            Expression = "status = \"failed\"", Severity = "error", Enabled = true, CreatedAt = Ago(8)
        };

        await _alertRules.InsertManyAsync(new[] { rule1, rule2 });
        Console.WriteLine($"  alertRules:  {2}");

        // ── JobRuns & Steps ───────────────────────────────────────────────────
        // ~20 historical runs spread across pipelines and the last 7 days.
        var runDefs = new (string PipelineId, int Version, string Status, double HoursAgo, double DurationMin, string? Error)[]
        {
            (p1.Id, 2, RunStatus.Success, 168, 2,    null),
            (p1.Id, 2, RunStatus.Success, 144, 3,    null),
            (p1.Id, 2, RunStatus.Failed,  120, 1,    "Kafka broker timeout after 60s"),
            (p1.Id, 2, RunStatus.Success, 96,  2.5,  null),
            (p1.Id, 2, RunStatus.Success, 72,  2,    null),
            (p1.Id, 2, RunStatus.Failed,  48,  0.5,  "Schema validation error on field 'battery_v'"),
            (p1.Id, 2, RunStatus.Success, 24,  2,    null),
            (p1.Id, 2, RunStatus.Running, 0.25, 0,   null),  // currently running

            (p2.Id, 1, RunStatus.Success, 160, 5,    null),
            (p2.Id, 1, RunStatus.Success, 130, 4,    null),
            (p2.Id, 1, RunStatus.Failed,  100, 0.8,  "Postgres connection refused"),
            (p2.Id, 1, RunStatus.Success, 80,  6,    null),
            (p2.Id, 1, RunStatus.Success, 50,  5,    null),
            (p2.Id, 1, RunStatus.Failed,  20,  1.2,  "Null constraint violation on 'session_id'"),

            (p3.Id, 1, RunStatus.Success, 155, 12,   null),
            (p3.Id, 1, RunStatus.Success, 107, 14,   null),
            (p3.Id, 1, RunStatus.Success, 59,  11,   null),
            (p3.Id, 1, RunStatus.Failed,  11,  3,    "Aggregation timeout: zone partition too large"),

            (p4.Id, 1, RunStatus.Success, 150, 20,   null),
            (p4.Id, 1, RunStatus.Success, 102, 18,   null),
            (p4.Id, 1, RunStatus.Success, 54,  22,   null),
        };

        var allRuns  = new List<JobRun>();
        var allSteps = new List<JobRunStep>();

        for (int i = 0; i < runDefs.Length; i++)
        {
            var (pid, ver, status, hoursAgo, durMin, err) = runDefs[i];
            var runId   = $"run-{i + 1:D3}";
            var started = Ago(hoursAgo);
            var finished = (status == RunStatus.Running) ? (DateTime?)null : started.AddMinutes(durMin);

            var run = new JobRun
            {
                Id = runId, PipelineId = pid, PipelineVersion = ver,
                PipelineVersionConfigSnapshot = new BsonDocument(),
                Status = status, StartedAt = started, FinishedAt = finished,
                RecordsProcessed = status == RunStatus.Success ? (i + 1) * 4700 : 0,
                ErrorMessage = err, CreatedAt = started
            };
            allRuns.Add(run);

            // Create ETL steps for non-running runs
            if (status != RunStatus.Running)
            {
                foreach (var stepName in StepNames.Ordered)
                {
                    var order = StepNames.Order(stepName);
                    var stepStart = started.AddSeconds(order * 20);
                    var stepEnd   = status == RunStatus.Failed && order == 2
                        ? stepStart.AddSeconds(15)
                        : stepStart.AddSeconds(20);
                    var stepStatus = (status == RunStatus.Failed && order == 2)
                        ? RunStatus.Failed
                        : RunStatus.Success;

                    allSteps.Add(new JobRunStep
                    {
                        Id       = $"{runId}-step-{stepName}",
                        RunId    = runId,
                        Name     = stepName,
                        Order    = order,
                        Status   = stepStatus,
                        StartedAt  = stepStart,
                        FinishedAt = stepEnd
                    });
                }
            }
        }

        await _runs.InsertManyAsync(allRuns);
        await _steps.InsertManyAsync(allSteps);
        Console.WriteLine($"  jobRuns:     {allRuns.Count}");
        Console.WriteLine($"  jobRunSteps: {allSteps.Count}");

        // ── Alert Events ──────────────────────────────────────────────────────
        // Tie alert events to failed runs that match the rules.
        var failedRunsForRule1 = allRuns.Where(r => r.PipelineId == p1.Id && r.Status == RunStatus.Failed).ToList();
        var failedRunsForRule2 = allRuns.Where(r => r.PipelineId == p2.Id && r.Status == RunStatus.Failed).ToList();

        var alertEvents = new List<AlertEvent>();

        foreach (var r in failedRunsForRule1)
        {
            alertEvents.Add(new AlertEvent
            {
                Id = $"alert-{alertEvents.Count + 1:D3}", RuleId = rule1.Id,
                RunId = r.Id, PipelineId = r.PipelineId,
                Message = $"Pipeline '{p1.Name}' run exceeded runtime threshold (10s). Error: {r.ErrorMessage}",
                Severity = "error", CreatedAt = r.FinishedAt!.Value
            });
        }

        foreach (var r in failedRunsForRule2)
        {
            alertEvents.Add(new AlertEvent
            {
                Id = $"alert-{alertEvents.Count + 1:D3}", RuleId = rule2.Id,
                RunId = r.Id, PipelineId = r.PipelineId,
                Message = $"Pipeline '{p2.Name}' run failed. Error: {r.ErrorMessage}",
                Severity = "warning", CreatedAt = r.FinishedAt!.Value
            });
        }

        await _alertEvents.InsertManyAsync(alertEvents);
        Console.WriteLine($"  alertEvents: {alertEvents.Count}");

        Console.WriteLine();
        Console.WriteLine("✓ Seed completed successfully.");
    }

    private static DateTime Ago(double hours) =>
        DateTime.UtcNow.AddHours(-hours);
}
