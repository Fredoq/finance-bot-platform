using FinanceBot.ServiceDefaults;
using FinanceCore.Application.Composition;
using FinanceCore.Infrastructure.Composition;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.Services.AddProblemDetails();
builder.Services.AddFinanceCoreApplication();
builder.Services.AddFinanceCoreInfrastructure();
WebApplication app = builder.Build();
app.UseExceptionHandler();
app.MapDefaultEndpoints();
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
