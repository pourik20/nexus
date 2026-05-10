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
                Expression = req.Expression,
                Severity = req.Severity ?? "warning",
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

            var update = Builders<AlertRule>.Update.Combine();
            bool hasUpdate = false;

            if (req.Name != null) { update = update.Set(r => r.Name, req.Name); rule.Name = req.Name; hasUpdate = true; }
            if (req.Expression != null) { update = update.Set(r => r.Expression, req.Expression); rule.Expression = req.Expression; hasUpdate = true; }
            if (req.Severity != null) { update = update.Set(r => r.Severity, req.Severity); rule.Severity = req.Severity; hasUpdate = true; }
            if (req.Enabled.HasValue) { update = update.Set(r => r.Enabled, req.Enabled.Value); rule.Enabled = req.Enabled.Value; hasUpdate = true; }

            if (hasUpdate)
                await rules.UpdateOneAsync(r => r.Id == id, update, cancellationToken: ct);

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
            [FromQuery] bool? acknowledged,
            [FromQuery] DateTime? createdAfter,
            [FromQuery] DateTime? createdBefore,
            IMongoCollection<AlertEvent> events,
            CancellationToken ct) =>
        {
            var builder = Builders<AlertEvent>.Filter;
            var filter = builder.Empty;

            if (pipelineId != null) filter &= builder.Eq(e => e.PipelineId, pipelineId);
            if (severity != null) filter &= builder.Eq(e => e.Severity, severity);
            if (acknowledged == false) filter &= builder.Eq(e => e.AcknowledgedAt, null);
            if (acknowledged == true) filter &= builder.Ne(e => e.AcknowledgedAt, null);
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
                evt.Id, evt.RuleId, evt.RunId, evt.PipelineId, evt.Message, evt.Severity, evt.CreatedAt, evt.AcknowledgedAt,
                rule == null ? null! : ToDto(rule)
            ));
        })
        .WithName("GetAlertDetail");

        group.MapPost("/alerts/{id}/acknowledge", async (string id, IMongoCollection<AlertEvent> events, CancellationToken ct) =>
        {
            var updated = await events.FindOneAndUpdateAsync(
                Builders<AlertEvent>.Filter.And(
                    Builders<AlertEvent>.Filter.Eq(e => e.Id, id),
                    Builders<AlertEvent>.Filter.Eq(e => e.AcknowledgedAt, null)),
                Builders<AlertEvent>.Update.Set(e => e.AcknowledgedAt, DateTime.UtcNow),
                new FindOneAndUpdateOptions<AlertEvent> { ReturnDocument = ReturnDocument.After },
                ct);

            if (updated == null)
            {
                var exists = await events.Find(e => e.Id == id).AnyAsync(ct);
                return exists ? Results.Ok() : Results.NotFound();
            }

            return Results.Ok(ToEventDto(updated));
        })
        .WithName("AcknowledgeAlert");
    }

    private static AlertRuleDto ToDto(AlertRule r) =>
        new(r.Id, r.PipelineId, r.Name, r.Expression, r.Severity, r.Enabled, r.CreatedAt);

    private static AlertEventDto ToEventDto(AlertEvent e) =>
        new(e.Id, e.RuleId, e.RunId, e.PipelineId, e.Message, e.Severity, e.CreatedAt, e.AcknowledgedAt);
}
