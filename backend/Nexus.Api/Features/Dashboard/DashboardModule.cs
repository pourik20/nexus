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

            // Run all counts in parallel
            var totalDatasetsTask     = datasets.CountDocumentsAsync(Builders<Dataset>.Filter.Empty, cancellationToken: ct);
            var totalPipelinesTask    = pipelines.CountDocumentsAsync(Builders<Pipeline>.Filter.Empty, cancellationToken: ct);
            var activePipelinesTask   = pipelines.CountDocumentsAsync(Builders<Pipeline>.Filter.Eq(p => p.Active, true), cancellationToken: ct);
            var runs24hTask           = runs.CountDocumentsAsync(Builders<JobRun>.Filter.Gte(r => r.StartedAt, since24h), cancellationToken: ct);
            var failedRuns24hTask     = runs.CountDocumentsAsync(
                Builders<JobRun>.Filter.And(
                    Builders<JobRun>.Filter.Gte(r => r.StartedAt, since24h),
                    Builders<JobRun>.Filter.Eq(r => r.Status, RunStatus.Failed)),
                cancellationToken: ct);
            var currentlyRunningTask  = runs.CountDocumentsAsync(Builders<JobRun>.Filter.Eq(r => r.Status, RunStatus.Running), cancellationToken: ct);
            var openAlertsTask        = alertEvents.CountDocumentsAsync(Builders<AlertEvent>.Filter.Gte(e => e.CreatedAt, since24h), cancellationToken: ct);
            var latestRunsTask        = runs.Find(Builders<JobRun>.Filter.Empty)
                                            .SortByDescending(r => r.StartedAt)
                                            .Limit(5)
                                            .ToListAsync(ct);
            var latestAlertsTask      = alertEvents.Find(Builders<AlertEvent>.Filter.Empty)
                                            .SortByDescending(e => e.CreatedAt)
                                            .Limit(5)
                                            .ToListAsync(ct);

            await Task.WhenAll(
                totalDatasetsTask, totalPipelinesTask, activePipelinesTask,
                runs24hTask, failedRuns24hTask, currentlyRunningTask, openAlertsTask,
                latestRunsTask, latestAlertsTask);

            // Build pipeline name lookup
            var pipelineIds = latestRunsTask.Result.Select(r => r.PipelineId)
                .Concat(latestAlertsTask.Result.Select(e => e.PipelineId))
                .Distinct()
                .ToList();

            var pipelineMap = new Dictionary<string, string>();
            if (pipelineIds.Count > 0)
            {
                var pipelineList = await pipelines
                    .Find(Builders<Pipeline>.Filter.In(p => p.Id, pipelineIds))
                    .ToListAsync(ct);
                foreach (var p in pipelineList)
                    pipelineMap[p.Id] = p.Name;
            }

            // Build alert rule name lookup
            var ruleIds = latestAlertsTask.Result.Select(e => e.RuleId).Distinct().ToList();
            var ruleMap = new Dictionary<string, string>();
            if (ruleIds.Count > 0)
            {
                var ruleList = await alertRules
                    .Find(Builders<AlertRule>.Filter.In(r => r.Id, ruleIds))
                    .ToListAsync(ct);
                foreach (var r in ruleList)
                    ruleMap[r.Id] = r.Name;
            }

            var latestRunDtos = latestRunsTask.Result.Select(r => new DashboardRunSummary(
                r.Id,
                r.PipelineId,
                pipelineMap.GetValueOrDefault(r.PipelineId, r.PipelineId),
                r.Status,
                r.StartedAt,
                r.FinishedAt)).ToList();

            var latestAlertDtos = latestAlertsTask.Result.Select(e => new DashboardAlertSummary(
                e.Id,
                ruleMap.GetValueOrDefault(e.RuleId, e.RuleId),
                e.PipelineId,
                pipelineMap.GetValueOrDefault(e.PipelineId, e.PipelineId),
                e.Severity,
                e.Message,
                e.CreatedAt)).ToList();

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
