using System.Text.Json.Nodes;
using Jsonata.Net.Native;
using MongoDB.Driver;
using Nexus.Api.Domain;
using Nexus.Api.Infrastructure.SignalR;
using Nexus.Api.Infrastructure.SignalR.Events;

namespace Nexus.Api.Features.Alerts;

public interface IAlertEvaluator
{
    Task EvaluateForRun(string runId, CancellationToken ct = default);
}

public class AlertEvaluator : IAlertEvaluator
{
    private readonly IMongoCollection<JobRun> _runs;
    private readonly IMongoCollection<Pipeline> _pipelines;
    private readonly IMongoCollection<AlertRule> _rules;
    private readonly IMongoCollection<AlertEvent> _events;
    private readonly INotificationService _notifications;
    private readonly ILogger<AlertEvaluator> _log;

    public AlertEvaluator(
        IMongoCollection<JobRun> runs,
        IMongoCollection<Pipeline> pipelines,
        IMongoCollection<AlertRule> rules,
        IMongoCollection<AlertEvent> events,
        INotificationService notifications,
        ILogger<AlertEvaluator> log)
    {
        _runs = runs;
        _pipelines = pipelines;
        _rules = rules;
        _events = events;
        _notifications = notifications;
        _log = log;
    }

    public async Task EvaluateForRun(string runId, CancellationToken ct = default)
    {
        var run = await _runs.Find(r => r.Id == runId).FirstOrDefaultAsync(ct);
        if (run is null) return;
        if (!RunStatus.IsTerminal(run.Status)) return;

        var pipeline = await _pipelines.Find(p => p.Id == run.PipelineId).FirstOrDefaultAsync(ct);
        if (pipeline is null) return;

        var rules = await _rules.Find(r => r.PipelineId == run.PipelineId && r.Enabled).ToListAsync(ct);
        if (rules.Count == 0) return;

        var contextJson = EvaluationContextBuilder.Build(run, pipeline).ToJsonString();

        foreach (var rule in rules)
        {
            try
            {
                var query = new JsonataQuery(rule.Expression);
                var result = query.Eval(contextJson);
                var isMatch = result.Trim().Equals("true", StringComparison.OrdinalIgnoreCase);

                if (isMatch)
                {
                    var evt = new AlertEvent
                    {
                        Id = Guid.NewGuid().ToString("N"),
                        RuleId = rule.Id,
                        RunId = run.Id,
                        PipelineId = run.PipelineId,
                        Message = $"Rule '{rule.Name}' matched on run {run.Id}.",
                        Severity = rule.Severity,
                        CreatedAt = DateTime.UtcNow
                    };

                    await _events.InsertOneAsync(evt, cancellationToken: ct);

                    await _notifications.AlertRaised(new AlertRaised(
                        evt.Id,
                        evt.RuleId,
                        evt.RunId,
                        evt.PipelineId,
                        evt.Message,
                        evt.Severity,
                        evt.CreatedAt
                    ), ct);
                }
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "Failed to evaluate alert rule {RuleId} on run {RunId}", rule.Id, run.Id);
            }
        }
    }
}
