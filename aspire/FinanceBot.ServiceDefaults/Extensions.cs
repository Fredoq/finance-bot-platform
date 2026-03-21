using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace FinanceBot.ServiceDefaults;

/// <summary>
/// Provides shared Aspire defaults for local service orchestration.
/// </summary>
public static class Extensions
{
    private const string HealthEndpointPath = "/health";
    private const string AlivenessEndpointPath = "/alive";
    /// <summary>
    /// Adds shared health, telemetry, and service discovery defaults.
    /// </summary>
    /// <typeparam name="TBuilder">The application builder type.</typeparam>
    /// <param name="builder">The application builder.</param>
    /// <returns>The configured builder.</returns>
    public static TBuilder AddServiceDefaults<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.ConfigureOpenTelemetry();
        builder.AddDefaultHealthChecks();
        builder.Services.AddServiceDiscovery();
        builder.Services.ConfigureHttpClientDefaults(item =>
        {
            item.AddStandardResilienceHandler();
            item.AddServiceDiscovery();
        });
        return builder;
    }
    /// <summary>
    /// Configures OpenTelemetry defaults for Aspire local development.
    /// </summary>
    /// <typeparam name="TBuilder">The application builder type.</typeparam>
    /// <param name="builder">The application builder.</param>
    /// <returns>The configured builder.</returns>
    public static TBuilder ConfigureOpenTelemetry<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.Logging.AddOpenTelemetry(item =>
        {
            item.IncludeFormattedMessage = true;
            item.IncludeScopes = true;
        });
        builder.Services.AddOpenTelemetry()
            .WithMetrics(item => item.AddAspNetCoreInstrumentation().AddHttpClientInstrumentation().AddRuntimeInstrumentation())
            .WithTracing(item => item.AddSource(builder.Environment.ApplicationName).AddAspNetCoreInstrumentation(note => note.Filter = context => !context.Request.Path.StartsWithSegments(HealthEndpointPath) && !context.Request.Path.StartsWithSegments(AlivenessEndpointPath)).AddHttpClientInstrumentation());
        builder.AddOpenTelemetryExporters();
        return builder;
    }
    /// <summary>
    /// Adds the default liveness health check.
    /// </summary>
    /// <typeparam name="TBuilder">The application builder type.</typeparam>
    /// <param name="builder">The application builder.</param>
    /// <returns>The configured builder.</returns>
    public static TBuilder AddDefaultHealthChecks<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.Services.AddHealthChecks().AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);
        return builder;
    }
    /// <summary>
    /// Maps the standard Aspire health endpoints.
    /// </summary>
    /// <param name="app">The web application.</param>
    /// <returns>The configured application.</returns>
    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.MapHealthChecks(HealthEndpointPath);
            app.MapHealthChecks(AlivenessEndpointPath, new HealthCheckOptions { Predicate = item => item.Tags.Contains("live") });
        }
        return app;
    }
    private static TBuilder AddOpenTelemetryExporters<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        if (!string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]))
        {
            builder.Services.AddOpenTelemetry().UseOtlpExporter();
        }
        return builder;
    }
}
