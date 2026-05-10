using Nexus.Api.Infrastructure.Validation;

namespace Nexus.Api.Features.Datasets.Create;

public static class CreateDatasetEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/datasets", async (CreateDatasetRequest req, CreateDatasetService svc, CancellationToken ct) =>
        {
            var d = await svc.Handle(req, ct);
            return Results.Created($"/datasets/{d.Id}", d.ToDto());
        })
        .AddEndpointFilter<ValidationFilter<CreateDatasetRequest>>()
        .WithName("CreateDataset");
    }
}
