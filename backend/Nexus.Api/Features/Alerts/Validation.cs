using FluentValidation;
using Jsonata.Net.Native;

namespace Nexus.Api.Features.Alerts;

public class CreateAlertRuleRequestValidator : AbstractValidator<CreateAlertRuleRequest>
{
    private static readonly string[] ValidSeverities = ["info", "warning", "error"];

    public CreateAlertRuleRequestValidator()
    {
        RuleFor(x => x.PipelineId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.Expression).NotEmpty().Must(BeValidJsonata).WithMessage("Expression is not valid JSONata.");
        RuleFor(x => x.Severity)
            .Must(s => s == null || ValidSeverities.Contains(s))
            .WithMessage($"Severity must be one of: {string.Join(", ", ValidSeverities)}.");
    }

    private static bool BeValidJsonata(string expression)
    {
        try { new JsonataQuery(expression); return true; }
        catch { return false; }
    }
}

public class UpdateAlertRuleRequestValidator : AbstractValidator<UpdateAlertRuleRequest>
{
    private static readonly string[] ValidSeverities = ["info", "warning", "error"];

    public UpdateAlertRuleRequestValidator()
    {
        RuleFor(x => x.Expression)
            .Must(BeValidJsonata)
            .When(x => !string.IsNullOrEmpty(x.Expression))
            .WithMessage("Expression is not valid JSONata.");
        RuleFor(x => x.Severity)
            .Must(s => s == null || ValidSeverities.Contains(s))
            .WithMessage($"Severity must be one of: {string.Join(", ", ValidSeverities)}.");
    }

    private static bool BeValidJsonata(string? expression)
    {
        if (string.IsNullOrEmpty(expression)) return true;
        try { new JsonataQuery(expression); return true; }
        catch { return false; }
    }
}
