using FluentValidation;

namespace Nexus.Api.Features.Pipelines.Update;

public class UpdatePipelineValidator : AbstractValidator<UpdatePipelineRequest>
{
    public UpdatePipelineValidator()
    {
        When(x => x.Name is not null, () =>
        {
            RuleFor(x => x.Name!).NotEmpty().MaximumLength(200);
        });
        RuleFor(x => x.Schedule).MaximumLength(200);
    }
}
