using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using Nexus.Api.Domain;

namespace Nexus.Api.Features.Dashboard;

public record DashboardRunSummary(
    string Id,
    string PipelineId,
    string PipelineName,
    string Status,
    DateTime StartedAt,
    DateTime? FinishedAt);

public record DashboardAlertSummary(
    string Id,
    string RuleName,
    string PipelineId,
    string PipelineName,
    string Severity,
    string Message,
    DateTime CreatedAt);

public record DashboardSummaryResponse(
    int TotalDatasets,
    int TotalPipelines,
    int ActivePipelines,
    int Runs24h,
    int FailedRuns24h,
    int CurrentlyRunning,
    int OpenAlerts,
    IReadOnlyList<DashboardRunSummary> LatestRuns,
    IReadOnlyList<DashboardAlertSummary> LatestAlerts);

[BsonIgnoreExtraElements]
file sealed class RunWithPipeline : JobRun
{
    [BsonElement("pl")] public List<Pipeline> Pl { get; set; } = new();
}

[BsonIgnoreExtraElements]
file sealed class AlertWithLookups : AlertEvent
{
    [BsonElement("rule")] public List<AlertRule> Rule { get; set; } = new();
    [BsonElement("pl")] public List<Pipeline> Pl { get; set; } = new();
}

public static class DashboardModule
{
    public static void MapDashboard(this IEndpointRouteBuilder app)
    {
        app.MapGet("/dashboard/summary", async (
            IMongoCollection<Dataset> datasets,
            IMongoCollection<Pipeline> pipelines,
            IMongoCollection<JobRun> runs,
            IMongoCollection<AlertEvent> alertEvents,
            IMongoCollection<AlertRule> alertRules,
            CancellationToken ct) =>
        {
            var since24h = DateTime.UtcNow.AddHours(-24);

            var totalDatasetsTask    = datasets.CountDocumentsAsync(Builders<Dataset>.Filter.Empty, cancellationToken: ct);
            var totalPipelinesTask   = pipelines.CountDocumentsAsync(Builders<Pipeline>.Filter.Empty, cancellationToken: ct);
            var activePipelinesTask  = pipelines.CountDocumentsAsync(Builders<Pipeline>.Filter.Eq(p => p.Active, true), cancellationToken: ct);
            var runs24hTask          = runs.CountDocumentsAsync(Builders<JobRun>.Filter.Gte(r => r.StartedAt, since24h), cancellationToken: ct);
            var failedRuns24hTask    = runs.CountDocumentsAsync(
                Builders<JobRun>.Filter.And(
                    Builders<JobRun>.Filter.Gte(r => r.StartedAt, since24h),
                    Builders<JobRun>.Filter.Eq(r => r.Status, RunStatus.Failed)),
                cancellationToken: ct);
            var currentlyRunningTask = runs.CountDocumentsAsync(Builders<JobRun>.Filter.Eq(r => r.Status, RunStatus.Running), cancellationToken: ct);
            var openAlertsTask       = alertEvents.CountDocumentsAsync(Builders<AlertEvent>.Filter.Eq(e => e.AcknowledgedAt, null), cancellationToken: ct);

            var latestRunsTask = runs.Aggregate()
                .Sort(Builders<JobRun>.Sort.Descending(r => r.StartedAt))
                .Limit(5)
                .Lookup<JobRun, Pipeline, RunWithPipeline>(pipelines, r => r.PipelineId, p => p.Id, r => r.Pl)
                .ToListAsync(ct);

            var latestAlertsTask = alertEvents.Aggregate()
                .Sort(Builders<AlertEvent>.Sort.Descending(e => e.CreatedAt))
                .Limit(5)
                .Lookup<AlertEvent, AlertRule, AlertWithLookups>(alertRules, e => e.RuleId, r => r.Id, e => e.Rule)
                .Lookup<AlertWithLookups, Pipeline, AlertWithLookups>(pipelines, e => e.PipelineId, p => p.Id, e => e.Pl)
                .ToListAsync(ct);

            await Task.WhenAll(
                totalDatasetsTask, totalPipelinesTask, activePipelinesTask,
                runs24hTask, failedRuns24hTask, currentlyRunningTask, openAlertsTask,
                latestRunsTask, latestAlertsTask);

            var latestRunDtos = latestRunsTask.Result.Select(r => new DashboardRunSummary(
                r.Id, r.PipelineId,
                r.Pl.FirstOrDefault()?.Name ?? r.PipelineId,
                r.Status, r.StartedAt, r.FinishedAt))
                .ToList();

            var latestAlertDtos = latestAlertsTask.Result.Select(e => new DashboardAlertSummary(
                e.Id,
                e.Rule.FirstOrDefault()?.Name ?? e.RuleId,
                e.PipelineId,
                e.Pl.FirstOrDefault()?.Name ?? e.PipelineId,
                e.Severity, e.Message, e.CreatedAt))
                .ToList();

            return Results.Ok(new DashboardSummaryResponse(
                (int)totalDatasetsTask.Result,
                (int)totalPipelinesTask.Result,
                (int)activePipelinesTask.Result,
                (int)runs24hTask.Result,
                (int)failedRuns24hTask.Result,
                (int)currentlyRunningTask.Result,
                (int)openAlertsTask.Result,
                latestRunDtos,
                latestAlertDtos));
        })
        .WithName("GetDashboardSummary")
        .WithTags("Dashboard");
    }
}
