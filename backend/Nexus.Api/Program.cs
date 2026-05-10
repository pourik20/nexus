using FluentValidation;
using Nexus.Api.Features.Datasets;
using Nexus.Api.Features.Pipelines;
using Nexus.Api.Features.Runs;
using Nexus.Api.Infrastructure.Auth;
using Nexus.Api.Infrastructure.Errors;
using Nexus.Api.Infrastructure.Mongo;
using Nexus.Api.Infrastructure.SignalR;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, lc) => lc
    .ReadFrom.Configuration(ctx.Configuration)
    .WriteTo.Console());

builder.WebHost.ConfigureKestrel(o => o.ListenAnyIP(5000));

builder.Services.AddMongo(builder.Configuration);
builder.Services.AddHostedService<IndexInitializerHostedService>();

builder.Services.AddSingleton<ICurrentUser, HardcodedAdminUser>();

builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddScoped(typeof(Nexus.Api.Infrastructure.Validation.ValidationFilter<>));

builder.Services.AddDatasets();
builder.Services.AddPipelines();
builder.Services.AddRuns();

builder.Services.AddSignalR();
builder.Services.AddSingleton<INotificationService, NotificationService>();

builder.Services.AddOpenApi();

builder.Services.AddCors(o => o.AddDefaultPolicy(p => p
    .WithOrigins("http://localhost:3000")
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials()));

var app = builder.Build();

app.UseDomainExceptionHandler();
app.UseCors();
app.MapOpenApi();

app.MapDatasets();
app.MapPipelines();
app.MapRuns();
app.MapHub<ControlHub>("/hubs/control");

app.MapGet("/", () => Results.Ok(new { name = "Nexus API", status = "ok" }));

app.Run();
