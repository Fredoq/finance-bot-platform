using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using TelegramGateway.Api;
using TelegramGateway.Application;
using TelegramGateway.Application.Messaging;
using TelegramGateway.Application.Telegram.Contracts;
using TelegramGateway.Application.Telegram.Flow;
using TelegramGateway.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole();
builder.Services.AddProblemDetails();
builder.Services.AddOptionsWithValidateOnStart<TelegramWebhookOptions>().BindConfiguration(TelegramWebhookOptions.Section).ValidateDataAnnotations();
builder.Services.AddTelegramGatewayApplication();
builder.Services.AddTelegramGatewayInfrastructure(builder.Configuration);
var open = builder.Services.AddOpenTelemetry();
open.ConfigureResource(item => item.AddService("telegram-gateway"));
open.WithTracing(item => item.AddAspNetCoreInstrumentation());
open.WithMetrics(item => item.AddAspNetCoreInstrumentation().AddRuntimeInstrumentation());
var app = builder.Build();
app.UseExceptionHandler();
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = item => item.Tags.Contains("ready") });
var group = app.MapGroup("/telegram");
group.AddEndpointFilter<SecretGate>();
group.MapPost("/webhook", async Task<IResult> (TelegramUpdate update, ITelegramFlow flow, HttpContext item, ILogger<Program> log, CancellationToken token) =>
{
    try
    {
        var trace = Activity.Current?.TraceId.ToString() ?? item.TraceIdentifier;
        await flow.Run(update, trace, token);
        return TypedResults.Ok();
    }
    catch (BusException error)
    {
        log.LogError(error, "Webhook publish failed");
        return TypedResults.Problem(statusCode: StatusCodes.Status503ServiceUnavailable, title: "Service unavailable", detail: "Message publish failed");
    }
});
await app.RunAsync();

/// <summary>
/// Exposes the entry assembly type required by the ASP.NET Core test host.
/// Example:
/// <code>
/// using var host = new WebApplicationFactory&lt;Program&gt;();
/// </code>
/// </summary>
public partial class Program
{
    private Program()
    {
    }
}
