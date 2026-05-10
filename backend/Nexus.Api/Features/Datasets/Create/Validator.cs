using FluentValidation;

namespace Nexus.Api.Features.Datasets.Create;

public class CreateDatasetValidator : AbstractValidator<CreateDatasetRequest>
{
    public CreateDatasetValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Owner).NotEmpty().MaximumLength(200);
        RuleFor(x => x.SchemaVersion).GreaterThanOrEqualTo(1);
    }
}
