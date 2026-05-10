using Microsoft.AspNetCore.Diagnostics;
using Nexus.Api.Domain;

namespace Nexus.Api.Infrastructure.Errors;

public static class ExceptionHandling
{
    public static void UseDomainExceptionHandler(this WebApplication app)
    {
        app.UseExceptionHandler(builder =>
        {
            builder.Run(async ctx =>
            {
                var feature = ctx.Features.Get<IExceptionHandlerFeature>();
                var ex = feature?.Error;

                if (ex is DomainException domainEx)
                {
                    var errors = string.IsNullOrEmpty(domainEx.Field)
                        ? new Dictionary<string, string[]> { [""] = new[] { domainEx.Message } }
                        : new Dictionary<string, string[]> { [domainEx.Field] = new[] { domainEx.Message } };

                    ctx.Response.StatusCode = StatusCodes.Status422UnprocessableEntity;
                    await Results.ValidationProblem(errors, statusCode: 422, title: "Domain rule violated")
                        .ExecuteAsync(ctx);
                    return;
                }

                ctx.Response.StatusCode = StatusCodes.Status500InternalServerError;
                await Results.Problem(
                    title: "Unexpected error",
                    detail: ex?.Message,
                    statusCode: 500
                ).ExecuteAsync(ctx);
            });
        });
    }
}
