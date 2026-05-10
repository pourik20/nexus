using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using Nexus.Api.Domain;
using Nexus.Api.Infrastructure.Validation;

namespace Nexus.Api.Features.Alerts;

public static class AlertsModule
{
    public static void AddAlerts(this IServiceCollection services)
    {
        services.AddScoped<IAlertEvaluator, AlertEvaluator>();
    }

    public static void MapAlerts(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("").WithTags("Alerts");

        group.MapPost("/alert-rules", async (CreateAlertRuleRequest req, IMongoCollection<AlertRule> rules, CancellationToken ct) =>
        {
            var rule = new AlertRule
            {
                Id = Guid.NewGuid().ToString("N"),
                PipelineId = req.PipelineId,
                Name = req.Name,
                Type = req.Type,
                RuntimeThreshold = req.RuntimeThreshold,
                Expression = req.Expression,
                Enabled = req.Enabled,
                CreatedAt = DateTime.UtcNow
            };
            await rules.InsertOneAsync(rule, cancellationToken: ct);
            return Results.Created($"/alert-rules/{rule.Id}", ToDto(rule));
        })
        .AddEndpointFilter<ValidationFilter<CreateAlertRuleRequest>>()
        .WithName("CreateAlertRule");

        group.MapGet("/alert-rules", async ([FromQuery] string? pipelineId, IMongoCollection<AlertRule> rules, CancellationToken ct) =>
        {
            var filter = pipelineId == null 
                ? Builders<AlertRule>.Filter.Empty 
                : Builders<AlertRule>.Filter.Eq(r => r.PipelineId, pipelineId);
                
            var result = await rules.Find(filter).ToListAsync(ct);
            return Results.Ok(result.Select(ToDto));
        })
        .WithName("ListAlertRules");

        group.MapGet("/alert-rules/{id}", async (string id, IMongoCollection<AlertRule> rules, CancellationToken ct) =>
        {
            var rule = await rules.Find(r => r.Id == id).FirstOrDefaultAsync(ct);
            return rule == null ? Results.NotFound() : Results.Ok(ToDto(rule));
        })
        .WithName("GetAlertRule");

        group.MapPatch("/alert-rules/{id}", async (string id, UpdateAlertRuleRequest req, IMongoCollection<AlertRule> rules, CancellationToken ct) =>
        {
            var rule = await rules.Find(r => r.Id == id).FirstOrDefaultAsync(ct);
            if (rule == null) return Results.NotFound();

            var newType = req.Type ?? rule.Type;
            // Treat empty string as a request to nullify the threshold.
            var newThreshold = req.RuntimeThreshold != null 
                ? (req.RuntimeThreshold == "" ? null : req.RuntimeThreshold) 
                : rule.RuntimeThreshold;
            
            if (newType == AlertRuleType.RuntimeExceeds && string.IsNullOrEmpty(newThreshold))
            {
                return Results.Problem(
                    detail: "RuntimeThreshold is required when Type is RuntimeExceeds.", 
                    statusCode: 400,
                    extensions: new Dictionary<string, object?> { { "errors", new { runtimeThreshold = new[] { "RuntimeThreshold is required when Type is RuntimeExceeds." } } } });
            }
            if (newType == AlertRuleType.RunFailed && !string.IsNullOrEmpty(newThreshold))
            {
                return Results.Problem(
                    detail: "RuntimeThreshold must be null when Type is RunFailed.", 
                    statusCode: 400,
                    extensions: new Dictionary<string, object?> { { "errors", new { runtimeThreshold = new[] { "RuntimeThreshold must be null when Type is RunFailed." } } } });
            }

            var update = Builders<AlertRule>.Update.Combine();
            bool hasUpdate = false;

            if (req.Name != null) { update = update.Set(r => r.Name, req.Name); rule.Name = req.Name; hasUpdate = true; }
            if (req.Type != null) { update = update.Set(r => r.Type, req.Type); rule.Type = req.Type; hasUpdate = true; }
            if (req.RuntimeThreshold != null) 
            { 
                var val = req.RuntimeThreshold == "" ? null : req.RuntimeThreshold;
                update = update.Set(r => r.RuntimeThreshold, val); 
                rule.RuntimeThreshold = val; 
                hasUpdate = true; 
            }
            if (req.Expression != null) { update = update.Set(r => r.Expression, req.Expression); rule.Expression = req.Expression; hasUpdate = true; }
            if (req.Enabled.HasValue) { update = update.Set(r => r.Enabled, req.Enabled.Value); rule.Enabled = req.Enabled.Value; hasUpdate = true; }

            if (hasUpdate)
            {
                await rules.UpdateOneAsync(r => r.Id == id, update, cancellationToken: ct);
            }

            return Results.Ok(ToDto(rule));
        })
        .AddEndpointFilter<ValidationFilter<UpdateAlertRuleRequest>>()
        .WithName("UpdateAlertRule");

        group.MapDelete("/alert-rules/{id}", async (string id, IMongoCollection<AlertRule> rules, CancellationToken ct) =>
        {
            var result = await rules.DeleteOneAsync(r => r.Id == id, ct);
            return result.DeletedCount == 0 ? Results.NotFound() : Results.NoContent();
        })
        .WithName("DeleteAlertRule");

        group.MapGet("/alerts", async (
            [FromQuery] string? pipelineId, 
            [FromQuery] string? severity, 
            [FromQuery] DateTime? createdAfter, 
            [FromQuery] DateTime? createdBefore, 
            IMongoCollection<AlertEvent> events, 
            CancellationToken ct) =>
        {
            var builder = Builders<AlertEvent>.Filter;
            var filter = builder.Empty;

            if (pipelineId != null) filter &= builder.Eq(e => e.PipelineId, pipelineId);
            if (severity != null) filter &= builder.Eq(e => e.Severity, severity);
            if (createdAfter != null) filter &= builder.Gte(e => e.CreatedAt, createdAfter);
            if (createdBefore != null) filter &= builder.Lte(e => e.CreatedAt, createdBefore);

            var result = await events.Find(filter).SortByDescending(e => e.CreatedAt).ToListAsync(ct);
            return Results.Ok(result.Select(ToEventDto));
        })
        .WithName("ListAlerts");

        group.MapGet("/alerts/{id}", async (string id, IMongoCollection<AlertEvent> events, IMongoCollection<AlertRule> rules, CancellationToken ct) =>
        {
            var evt = await events.Find(e => e.Id == id).FirstOrDefaultAsync(ct);
            if (evt == null) return Results.NotFound();

            var rule = await rules.Find(r => r.Id == evt.RuleId).FirstOrDefaultAsync(ct);
            
            return Results.Ok(new AlertEventDetailDto(
                evt.Id, evt.RuleId, evt.RunId, evt.PipelineId, evt.Message, evt.Severity, evt.CreatedAt,
                rule == null ? null! : ToDto(rule)
            ));
        })
        .WithName("GetAlertDetail");
    }

    private static AlertRuleDto ToDto(AlertRule r) => new(r.Id, r.PipelineId, r.Name, r.Type, r.RuntimeThreshold, r.Expression, r.Enabled, r.CreatedAt);
    private static AlertEventDto ToEventDto(AlertEvent e) => new(e.Id, e.RuleId, e.RunId, e.PipelineId, e.Message, e.Severity, e.CreatedAt);
}
