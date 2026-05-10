using Nexus.Api.Infrastructure.Validation;

namespace Nexus.Api.Features.Pipelines.Create;

public static class CreatePipelineEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/pipelines", async (CreatePipelineRequest req, CreatePipelineService svc, CancellationToken ct) =>
        {
            var p = await svc.Handle(req, ct);
            return Results.Created($"/pipelines/{p.Id}", p.ToDto());
        })
        .AddEndpointFilter<ValidationFilter<CreatePipelineRequest>>()
        .WithName("CreatePipeline");
    }
}
