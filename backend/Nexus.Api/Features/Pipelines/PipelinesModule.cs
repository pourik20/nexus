using Nexus.Api.Features.Datasets.Delete;
using Nexus.Api.Features.Pipelines.ActivateVersion;
using Nexus.Api.Features.Pipelines.AddVersion;
using Nexus.Api.Features.Pipelines.Create;
using Nexus.Api.Features.Pipelines.Delete;
using Nexus.Api.Features.Pipelines.Get;
using Nexus.Api.Features.Pipelines.List;
using Nexus.Api.Features.Pipelines.Update;

namespace Nexus.Api.Features.Pipelines;

public static class PipelinesModule
{
    public static void AddPipelines(this IServiceCollection services)
    {
        services.AddScoped<CreatePipelineService>();
        services.AddScoped<IDatasetReferenceGuard, PipelineDatasetReferenceGuard>();
    }

    public static void MapPipelines(this IEndpointRouteBuilder app)
    {
        CreatePipelineEndpoint.Map(app);
        ListPipelinesEndpoint.Map(app);
        GetPipelineEndpoint.Map(app);
        UpdatePipelineEndpoint.Map(app);
        DeletePipelineEndpoint.Map(app);
        AddVersionEndpoint.Map(app);
        ActivateVersionEndpoint.Map(app);
    }
}
