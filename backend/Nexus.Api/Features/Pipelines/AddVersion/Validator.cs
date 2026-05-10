using FluentValidation;

namespace Nexus.Api.Features.Pipelines.AddVersion;

public class AddVersionValidator : AbstractValidator<AddVersionRequest>
{
    public AddVersionValidator()
    {
    }
}
