using FinanceCore.Infrastructure.Observability.Checks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FinanceCore.Api.Tests;

/// <summary>
/// Covers shared health probe behavior.
/// </summary>
public sealed class HealthProbeTests
{
    /// <summary>
    /// Verifies that a false probe result maps to an unhealthy status.
    /// </summary>
    /// <returns>A task that completes when the assertions finish.</returns>
    [Fact(DisplayName = "Returns unhealthy when the boolean probe returns false")]
    public async Task Rejects_false_probe()
    {
        HealthCheckResult item = await HealthProbe.Run("broken", _ => ValueTask.FromResult(false), default);
        Assert.Equal(HealthStatus.Unhealthy, item.Status);
    }
    /// <summary>
    /// Verifies that a thrown exception is attached to the unhealthy result.
    /// </summary>
    /// <returns>A task that completes when the assertions finish.</returns>
    [Fact(DisplayName = "Returns unhealthy when the action probe throws")]
    public async Task Rejects_throwing_probe()
    {
        var error = new InvalidOperationException("broken");
        HealthCheckResult item = await HealthProbe.Run("broken", _ => ValueTask.FromException(error), default);
        Assert.Equal(HealthStatus.Unhealthy, item.Status);
        Assert.Same(error, item.Exception);
    }
}
