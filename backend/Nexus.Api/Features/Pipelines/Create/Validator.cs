using FluentValidation;

namespace Nexus.Api.Features.Pipelines.Create;

public class CreatePipelineValidator : AbstractValidator<CreatePipelineRequest>
{
    public CreatePipelineValidator()
    {
        RuleFor(x => x.DatasetId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Schedule).MaximumLength(200);
    }
}
