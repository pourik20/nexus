using FluentValidation;
using Jsonata.Net.Native;
using Nexus.Api.Domain;

namespace Nexus.Api.Features.Alerts;

public class CreateAlertRuleRequestValidator : AbstractValidator<CreateAlertRuleRequest>
{
    public CreateAlertRuleRequestValidator()
    {
        RuleFor(x => x.PipelineId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.Type).Must(t => t == AlertRuleType.RuntimeExceeds || t == AlertRuleType.RunFailed)
            .WithMessage($"Type must be '{AlertRuleType.RuntimeExceeds}' or '{AlertRuleType.RunFailed}'.");
            
        RuleFor(x => x.RuntimeThreshold)
            .NotEmpty().When(x => x.Type == AlertRuleType.RuntimeExceeds)
            .WithMessage($"RuntimeThreshold is required when Type is {AlertRuleType.RuntimeExceeds}.");
            
        RuleFor(x => x.RuntimeThreshold)
            .Must(t => TimeSpan.TryParse(t, out _))
            .When(x => !string.IsNullOrEmpty(x.RuntimeThreshold))
            .WithMessage("RuntimeThreshold must be a parseable TimeSpan.");
            
        RuleFor(x => x.RuntimeThreshold)
            .Empty().When(x => x.Type == AlertRuleType.RunFailed)
            .WithMessage($"RuntimeThreshold must be null when Type is {AlertRuleType.RunFailed}.");
            
        RuleFor(x => x.Expression).NotEmpty()
            .Must(BeValidJsonata).WithMessage("Expression is not valid JSONata.");
    }
    
    private bool BeValidJsonata(string expression)
    {
        try
        {
            new JsonataQuery(expression);
            return true;
        }
        catch
        {
            return false;
        }
    }
}

public class UpdateAlertRuleRequestValidator : AbstractValidator<UpdateAlertRuleRequest>
{
    public UpdateAlertRuleRequestValidator()
    {
        RuleFor(x => x.Type).Must(t => t == AlertRuleType.RuntimeExceeds || t == AlertRuleType.RunFailed)
            .When(x => x.Type != null)
            .WithMessage($"Type must be '{AlertRuleType.RuntimeExceeds}' or '{AlertRuleType.RunFailed}'.");
            
        RuleFor(x => x.RuntimeThreshold)
            .Must(t => TimeSpan.TryParse(t, out _))
            .When(x => !string.IsNullOrEmpty(x.RuntimeThreshold))
            .WithMessage("RuntimeThreshold must be a parseable TimeSpan.");
            
        RuleFor(x => x.Expression)
            .Must(BeValidJsonata)
            .When(x => !string.IsNullOrEmpty(x.Expression))
            .WithMessage("Expression is not valid JSONata.");
    }
    
    private bool BeValidJsonata(string? expression)
    {
        if (string.IsNullOrEmpty(expression)) return true;
        try
        {
            new JsonataQuery(expression);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
