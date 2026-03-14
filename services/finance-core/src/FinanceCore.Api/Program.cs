using FinanceCore.Application;
using FinanceCore.Infrastructure;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole();
builder.Services.AddProblemDetails();
builder.Services.AddFinanceCoreApplication();
builder.Services.AddFinanceCoreInfrastructure();
OpenTelemetryBuilder open = builder.Services.AddOpenTelemetry();
open.ConfigureResource(item => item.AddService("finance-core"));
open.WithTracing(item => item.AddAspNetCoreInstrumentation().AddOtlpExporter());
open.WithMetrics(item => item.AddAspNetCoreInstrumentation().AddRuntimeInstrumentation().AddOtlpExporter());
WebApplication app = builder.Build();
app.UseExceptionHandler();
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = item => item.Tags.Contains("ready") });
await app.RunAsync();

/// <summary>
/// Exposes the entry assembly type required by the ASP.NET Core test host.
/// </summary>
public partial class Program
{
    private Program()
    {
    }
}
