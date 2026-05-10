using MongoDB.Driver;
using Moq;
using Nexus.Api.Domain;
using Nexus.Api.Features.Runs;
using Nexus.Api.Infrastructure.SignalR;
using Nexus.Api.Infrastructure.SignalR.Events;

namespace Nexus.Api.Tests;

public class RunStateServiceTests
{
    private static (RunStateService svc, Mock<IMongoCollection<JobRun>> runs, Mock<IMongoCollection<JobRunStep>> steps, Mock<INotificationService> notifications) Build()
    {
        var runs = new Mock<IMongoCollection<JobRun>>();
        var steps = new Mock<IMongoCollection<JobRunStep>>();
        var notifications = new Mock<INotificationService>();
        var svc = new RunStateService(runs.Object, steps.Object, notifications.Object);
        return (svc, runs, steps, notifications);
    }

    private static JobRun PendingRun(string id = "run1") => new()
    {
        Id = id, PipelineId = "p1", Status = RunStatus.Pending, CreatedAt = DateTime.UtcNow,
    };

    private static JobRun RunningRun(string id = "run1") => new()
    {
        Id = id, PipelineId = "p1", Status = RunStatus.Running, StartedAt = DateTime.UtcNow,
    };

    private static JobRun TerminalRun(string status, string id = "run1") => new()
    {
        Id = id, PipelineId = "p1", Status = status, StartedAt = DateTime.UtcNow, FinishedAt = DateTime.UtcNow,
    };

    [Fact]
    public async Task Start_TransitionsPendingToRunning_AndEmitsEvent()
    {
        var (svc, runs, _, notifications) = Build();
        var run = PendingRun();
        runs.SetupFindReturning(new[] { run });
        runs.SetupFindOneAndUpdateReturning(new JobRun { Id = run.Id, PipelineId = run.PipelineId, Status = RunStatus.Running, StartedAt = DateTime.UtcNow });

        await svc.Start(run);

        notifications.Verify(n => n.RunStateChanged(It.Is<RunStateChanged>(e => e.Status == RunStatus.Running && e.RunId == run.Id), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Start_OnNonExistentRun_Throws()
    {
        var (svc, runs, _, _) = Build();
        runs.SetupFindReturning(Array.Empty<JobRun>());
        await Assert.ThrowsAsync<DomainException>(() => svc.Start(PendingRun()));
    }

    [Fact]
    public async Task Start_OnAlreadyRunningRun_Throws()
    {
        var (svc, runs, _, _) = Build();
        runs.SetupFindReturning(new[] { RunningRun() });
        await Assert.ThrowsAsync<DomainException>(() => svc.Start(RunningRun()));
    }

    [Fact]
    public async Task BeginStep_OnRunningRun_TransitionsStepToRunning()
    {
        var (svc, runs, steps, notifications) = Build();
        runs.SetupFindReturning(new[] { RunningRun() });
        var step = new JobRunStep { Id = "s1", RunId = "run1", Name = StepNames.Extract, Order = 0, Status = RunStatus.Pending };
        steps.SetupFindReturning(new[] { step });
        steps.SetupFindOneAndUpdateReturning(new JobRunStep { Id = step.Id, RunId = step.RunId, Name = step.Name, Order = step.Order, Status = RunStatus.Running, StartedAt = DateTime.UtcNow });

        await svc.BeginStep("run1", StepNames.Extract);

        notifications.Verify(n => n.StepStateChanged(It.Is<StepStateChanged>(e => e.Status == RunStatus.Running && e.StepName == StepNames.Extract), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task BeginStep_OnNonRunningRun_Throws()
    {
        var (svc, runs, _, _) = Build();
        runs.SetupFindReturning(new[] { PendingRun() });
        await Assert.ThrowsAsync<DomainException>(() => svc.BeginStep("run1", StepNames.Extract));
    }

    [Fact]
    public async Task BeginStep_OnNonExistentStep_Throws()
    {
        var (svc, runs, steps, _) = Build();
        runs.SetupFindReturning(new[] { RunningRun() });
        steps.SetupFindReturning(Array.Empty<JobRunStep>());
        await Assert.ThrowsAsync<DomainException>(() => svc.BeginStep("run1", StepNames.Extract));
    }

    [Fact]
    public async Task CompleteStep_OnRunningStep_TransitionsToSuccess()
    {
        var (svc, runs, steps, notifications) = Build();
        runs.SetupFindReturning(new[] { RunningRun() });
        var step = new JobRunStep { Id = "s1", RunId = "run1", Name = StepNames.Extract, Order = 0, Status = RunStatus.Running, StartedAt = DateTime.UtcNow };
        steps.SetupFindReturning(new[] { step });
        steps.SetupFindOneAndUpdateReturning(new JobRunStep { Id = step.Id, RunId = step.RunId, Name = step.Name, Order = step.Order, Status = RunStatus.Success, StartedAt = step.StartedAt, FinishedAt = DateTime.UtcNow });

        await svc.CompleteStep("run1", StepNames.Extract, success: true);

        notifications.Verify(n => n.StepStateChanged(It.Is<StepStateChanged>(e => e.Status == RunStatus.Success), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CompleteStep_OnPendingStep_Throws()
    {
        var (svc, runs, steps, _) = Build();
        runs.SetupFindReturning(new[] { RunningRun() });
        steps.SetupFindReturning(new[] { new JobRunStep { Id = "s1", RunId = "run1", Name = StepNames.Extract, Order = 0, Status = RunStatus.Pending } });
        await Assert.ThrowsAsync<DomainException>(() => svc.CompleteStep("run1", StepNames.Extract, true));
    }

    [Fact]
    public async Task CompleteStep_OnNonExistentStep_Throws()
    {
        var (svc, runs, steps, _) = Build();
        runs.SetupFindReturning(new[] { RunningRun() });
        steps.SetupFindReturning(Array.Empty<JobRunStep>());
        await Assert.ThrowsAsync<DomainException>(() => svc.CompleteStep("run1", "missing", true));
    }

    [Fact]
    public async Task Complete_OnRunningRun_TransitionsToSuccess()
    {
        var (svc, runs, _, notifications) = Build();
        var running = RunningRun();
        runs.SetupFindReturning(new[] { running });
        runs.SetupFindOneAndUpdateReturning(new JobRun { Id = running.Id, PipelineId = running.PipelineId, Status = RunStatus.Success, StartedAt = running.StartedAt, FinishedAt = DateTime.UtcNow });

        await svc.Complete("run1", success: true);

        notifications.Verify(n => n.RunStateChanged(It.Is<RunStateChanged>(e => e.Status == RunStatus.Success), It.IsAny<CancellationToken>()), Times.Once);
        notifications.Verify(n => n.PipelineUpdated(running.PipelineId, "updated", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Complete_OnPendingRun_Throws()
    {
        var (svc, runs, _, _) = Build();
        runs.SetupFindReturning(new[] { PendingRun() });
        await Assert.ThrowsAsync<DomainException>(() => svc.Complete("run1", true));
    }

    [Fact]
    public async Task Complete_OnTerminalRun_Throws()
    {
        var (svc, runs, _, _) = Build();
        runs.SetupFindReturning(new[] { TerminalRun(RunStatus.Success) });
        await Assert.ThrowsAsync<DomainException>(() => svc.Complete("run1", true));
    }

    [Fact]
    public async Task Complete_OnNonExistentRun_Throws()
    {
        var (svc, runs, _, _) = Build();
        runs.SetupFindReturning(Array.Empty<JobRun>());
        await Assert.ThrowsAsync<DomainException>(() => svc.Complete("run1", true));
    }

    [Fact]
    public async Task Complete_WithFailure_PassesErrorMessage()
    {
        var (svc, runs, _, notifications) = Build();
        var running = RunningRun();
        runs.SetupFindReturning(new[] { running });
        runs.SetupFindOneAndUpdateReturning(new JobRun { Id = running.Id, PipelineId = running.PipelineId, Status = RunStatus.Failed, StartedAt = running.StartedAt, FinishedAt = DateTime.UtcNow, ErrorMessage = "boom" });

        await svc.Complete("run1", success: false, errorMessage: "boom");

        notifications.Verify(n => n.RunStateChanged(It.Is<RunStateChanged>(e => e.Status == RunStatus.Failed && e.ErrorMessage == "boom"), It.IsAny<CancellationToken>()), Times.Once);
    }
}
