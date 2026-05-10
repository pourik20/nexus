using Nexus.Api.Features.Runs.CreateRun;
using Nexus.Api.Features.Runs.Get;
using Nexus.Api.Features.Runs.List;
using Nexus.Api.Features.Runs.Patch;

namespace Nexus.Api.Features.Runs;

public static class RunsModule
{
    public static void AddRuns(this IServiceCollection services)
    {
        services.AddSingleton<IRandomProvider, DefaultRandomProvider>();
        services.AddScoped<RunStateService>();
        services.AddSingleton<IRunSimulator, RunSimulator>();
    }

    public static void MapRuns(this IEndpointRouteBuilder app)
    {
        CreateRunEndpoint.Map(app);
        ListRunsEndpoint.Map(app);
        GetRunEndpoint.Map(app);
        PatchRunEndpoint.Map(app);
    }
}
