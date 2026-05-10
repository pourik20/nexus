using MongoDB.Driver;
using Nexus.Api.Domain;

namespace Nexus.Api.Features.Runs;

public interface IRunSimulator
{
    void Start(JobRun run);
}

public class RunSimulator : IRunSimulator
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RunSimulator> _log;

    public RunSimulator(IServiceScopeFactory scopeFactory, ILogger<RunSimulator> log)
    {
        _scopeFactory = scopeFactory;
        _log = log;
    }

    public void Start(JobRun run)
    {
        _ = Task.Run(() => RunAsync(run.Id));
    }

    private async Task RunAsync(string runId)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var state = scope.ServiceProvider.GetRequiredService<RunStateService>();
            var random = scope.ServiceProvider.GetRequiredService<IRandomProvider>();
            var runs = scope.ServiceProvider.GetRequiredService<IMongoCollection<JobRun>>();

            var run = await runs.Find(r => r.Id == runId).FirstOrDefaultAsync();
            if (run is null) return;

            await state.Start(run);

            foreach (var stepName in StepNames.Ordered)
            {
                await state.BeginStep(runId, stepName);

                var durationMs = random.Next(2000, 8001);
                await Task.Delay(durationMs);

                var failed = random.NextDouble() < 0.05;
                if (failed)
                {
                    await state.CompleteStep(runId, stepName, success: false);
                    await state.Complete(runId, success: false, errorMessage: $"Step '{stepName}' failed.");
                    return;
                }

                await state.CompleteStep(runId, stepName, success: true);
            }

            await state.Complete(runId, success: true);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Simulator failed for run {RunId}", runId);
        }
    }
}
