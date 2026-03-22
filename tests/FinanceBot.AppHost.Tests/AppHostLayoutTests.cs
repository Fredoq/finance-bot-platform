using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace FinanceBot.AppHost.Tests;

/// <summary>
/// Covers the Aspire app host resource graph used for local development.
/// </summary>
public sealed class AppHostLayoutTests
{
    /// <summary>
    /// Verifies that the dev resource graph contains the expected services.
    /// </summary>
    [Fact(DisplayName = "Adds the expected dev resources to the app host")]
    public void Adds_resources()
    {
        string directory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "aspire", "FinanceBot.AppHost"));
        IDistributedApplicationBuilder item = DistributedApplication.CreateBuilder(new DistributedApplicationOptions
        {
            Args = [],
            AllowUnsecuredTransport = true,
            AssemblyName = "FinanceBot.AppHost",
            DisableDashboard = true,
            ProjectDirectory = directory
        });
        item.Configuration["Parameters:telegram-bot-token"] = "token";
        item.Configuration["Parameters:telegram-webhook-secret"] = "hook";
        item.Configuration["Parameters:telegram-key-secret"] = "key";
        var app = new AppHostLayout();
        app.Add(item);
        Assert.Contains(item.Resources, note => string.Equals(note.Name, "postgres", StringComparison.Ordinal));
        Assert.Contains(item.Resources, note => string.Equals(note.Name, "finance-db", StringComparison.Ordinal));
        Assert.Contains(item.Resources, note => string.Equals(note.Name, "rabbitmq", StringComparison.Ordinal));
        Assert.Contains(item.Resources, note => string.Equals(note.Name, "finance-core", StringComparison.Ordinal));
        Assert.Contains(item.Resources, note => string.Equals(note.Name, "telegram-gateway", StringComparison.Ordinal));
    }
    /// <summary>
    /// Verifies that the gateway waits for the finance core service.
    /// </summary>
    [Fact(DisplayName = "Makes telegram-gateway wait for finance-core")]
    public void Adds_gateway_wait()
    {
        string directory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "aspire", "FinanceBot.AppHost"));
        IDistributedApplicationBuilder item = DistributedApplication.CreateBuilder(new DistributedApplicationOptions
        {
            Args = [],
            AllowUnsecuredTransport = true,
            AssemblyName = "FinanceBot.AppHost",
            DisableDashboard = true,
            ProjectDirectory = directory
        });
        item.Configuration["Parameters:telegram-bot-token"] = "token";
        item.Configuration["Parameters:telegram-webhook-secret"] = "hook";
        item.Configuration["Parameters:telegram-key-secret"] = "key";
        var app = new AppHostLayout();
        app.Add(item);
        IResource gate = Assert.Single(item.Resources, note => string.Equals(note.Name, "telegram-gateway", StringComparison.Ordinal));
        WaitAnnotation note = Assert.Single(gate.Annotations.OfType<WaitAnnotation>(), data => string.Equals(data.Resource.Name, "finance-core", StringComparison.Ordinal));
        Assert.Equal(WaitType.WaitUntilHealthy, note.WaitType);
    }
}
