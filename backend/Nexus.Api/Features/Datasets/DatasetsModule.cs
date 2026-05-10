using Nexus.Api.Features.Datasets.Create;
using Nexus.Api.Features.Datasets.Delete;
using Nexus.Api.Features.Datasets.Get;
using Nexus.Api.Features.Datasets.List;

namespace Nexus.Api.Features.Datasets;

public static class DatasetsModule
{
    public static void AddDatasets(this IServiceCollection services)
    {
        services.AddScoped<CreateDatasetService>();
    }

    public static void MapDatasets(this IEndpointRouteBuilder app)
    {
        CreateDatasetEndpoint.Map(app);
        ListDatasetsEndpoint.Map(app);
        GetDatasetEndpoint.Map(app);
        DeleteDatasetEndpoint.Map(app);
    }
}
