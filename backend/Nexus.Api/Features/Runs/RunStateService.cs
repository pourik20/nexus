using MongoDB.Driver;
using Nexus.Api.Domain;
using Nexus.Api.Infrastructure.SignalR;
using Nexus.Api.Infrastructure.SignalR.Events;
using Nexus.Api.Features.Alerts;

namespace Nexus.Api.Features.Runs;

public class RunStateService
{
    private readonly IMongoCollection<JobRun> _runs;
    private readonly IMongoCollection<JobRunStep> _steps;
    private readonly INotificationService _notifications;
    private readonly IAlertEvaluator _alertEvaluator;

    public RunStateService(
        IMongoCollection<JobRun> runs,
        IMongoCollection<JobRunStep> steps,
        INotificationService notifications,
        IAlertEvaluator alertEvaluator)
    {
        _runs = runs;
        _steps = steps;
        _notifications = notifications;
        _alertEvaluator = alertEvaluator;
    }

    public async Task Start(JobRun run, CancellationToken ct = default)
    {
        var current = await _runs.Find(r => r.Id == run.Id).FirstOrDefaultAsync(ct);
        if (current is null)
            throw new DomainException($"Run '{run.Id}' does not exist.");
        if (current.Status != RunStatus.Pending)
            throw new DomainException($"Cannot start run in status '{current.Status}'.");

        var now = DateTime.UtcNow;
        var update = Builders<JobRun>.Update
            .Set(r => r.Status, RunStatus.Running)
            .Set(r => r.StartedAt, now);

        var updated = await _runs.FindOneAndUpdateAsync<JobRun>(
            Builders<JobRun>.Filter.And(
                Builders<JobRun>.Filter.Eq(r => r.Id, run.Id),
                Builders<JobRun>.Filter.Eq(r => r.Status, RunStatus.Pending)),
            update,
            new FindOneAndUpdateOptions<JobRun> { ReturnDocument = ReturnDocument.After },
            ct);

        if (updated is null)
            throw new DomainException("Run state changed concurrently.");

        await _notifications.RunStateChanged(
            new RunStateChanged(updated.Id, updated.PipelineId, updated.Status, updated.StartedAt, updated.FinishedAt, updated.ErrorMessage),
            ct);
    }

    public async Task BeginStep(string runId, string stepName, CancellationToken ct = default)
    {
        var run = await _runs.Find(r => r.Id == runId).FirstOrDefaultAsync(ct);
        if (run is null)
            throw new DomainException($"Run '{runId}' does not exist.");
        if (run.Status != RunStatus.Running)
            throw new DomainException($"Cannot begin step on run in status '{run.Status}'.");

        var step = await _steps.Find(s => s.RunId == runId && s.Name == stepName).FirstOrDefaultAsync(ct);
        if (step is null)
            throw new DomainException($"Step '{stepName}' does not exist on run '{runId}'.");
        if (step.Status != RunStatus.Pending)
            throw new DomainException($"Cannot begin step in status '{step.Status}'.");

        var now = DateTime.UtcNow;
        var updated = await _steps.FindOneAndUpdateAsync<JobRunStep>(
            Builders<JobRunStep>.Filter.And(
                Builders<JobRunStep>.Filter.Eq(s => s.Id, step.Id),
                Builders<JobRunStep>.Filter.Eq(s => s.Status, RunStatus.Pending)),
            Builders<JobRunStep>.Update
                .Set(s => s.Status, RunStatus.Running)
                .Set(s => s.StartedAt, now),
            new FindOneAndUpdateOptions<JobRunStep> { ReturnDocument = ReturnDocument.After },
            ct);

        if (updated is null)
            throw new DomainException("Step state changed concurrently.");

        await _notifications.StepStateChanged(
            new StepStateChanged(run.Id, run.PipelineId, updated.Name, updated.Status, updated.StartedAt, updated.FinishedAt),
            ct);
    }

    public async Task CompleteStep(string runId, string stepName, bool success, CancellationToken ct = default)
    {
        var run = await _runs.Find(r => r.Id == runId).FirstOrDefaultAsync(ct);
        if (run is null)
            throw new DomainException($"Run '{runId}' does not exist.");

        var step = await _steps.Find(s => s.RunId == runId && s.Name == stepName).FirstOrDefaultAsync(ct);
        if (step is null)
            throw new DomainException($"Step '{stepName}' does not exist on run '{runId}'.");
        if (step.Status != RunStatus.Running)
            throw new DomainException($"Cannot complete step in status '{step.Status}'.");

        var now = DateTime.UtcNow;
        var target = success ? RunStatus.Success : RunStatus.Failed;
        var updated = await _steps.FindOneAndUpdateAsync<JobRunStep>(
            Builders<JobRunStep>.Filter.And(
                Builders<JobRunStep>.Filter.Eq(s => s.Id, step.Id),
                Builders<JobRunStep>.Filter.Eq(s => s.Status, RunStatus.Running)),
            Builders<JobRunStep>.Update
                .Set(s => s.Status, target)
                .Set(s => s.FinishedAt, now),
            new FindOneAndUpdateOptions<JobRunStep> { ReturnDocument = ReturnDocument.After },
            ct);

        if (updated is null)
            throw new DomainException("Step state changed concurrently.");

        await _notifications.StepStateChanged(
            new StepStateChanged(run.Id, run.PipelineId, updated.Name, updated.Status, updated.StartedAt, updated.FinishedAt),
            ct);
    }

    public async Task Complete(string runId, bool success, string? errorMessage = null, CancellationToken ct = default)
    {
        var run = await _runs.Find(r => r.Id == runId).FirstOrDefaultAsync(ct);
        if (run is null)
            throw new DomainException($"Run '{runId}' does not exist.");
        if (run.Status == RunStatus.Pending)
            throw new DomainException("Cannot complete a run that has not started.");
        if (RunStatus.IsTerminal(run.Status))
            throw new DomainException($"Run is already in terminal status '{run.Status}'.");

        var now = DateTime.UtcNow;
        var target = success ? RunStatus.Success : RunStatus.Failed;
        var update = Builders<JobRun>.Update
            .Set(r => r.Status, target)
            .Set(r => r.FinishedAt, now);
        if (!string.IsNullOrWhiteSpace(errorMessage))
            update = update.Set(r => r.ErrorMessage, errorMessage);
        if (success)
            update = update.Set(r => r.RecordsProcessed, run.RecordsProcessed > 0 ? run.RecordsProcessed : 1000);

        var updated = await _runs.FindOneAndUpdateAsync<JobRun>(
            Builders<JobRun>.Filter.And(
                Builders<JobRun>.Filter.Eq(r => r.Id, runId),
                Builders<JobRun>.Filter.Eq(r => r.Status, RunStatus.Running)),
            update,
            new FindOneAndUpdateOptions<JobRun> { ReturnDocument = ReturnDocument.After },
            ct);

        if (updated is null)
            throw new DomainException("Run state changed concurrently.");

        await _notifications.RunStateChanged(
            new RunStateChanged(updated.Id, updated.PipelineId, updated.Status, updated.StartedAt, updated.FinishedAt, updated.ErrorMessage),
            ct);
        await _notifications.PipelineUpdated(updated.PipelineId, "updated", ct);

        await _alertEvaluator.EvaluateForRun(updated.Id, ct);
    }
}
