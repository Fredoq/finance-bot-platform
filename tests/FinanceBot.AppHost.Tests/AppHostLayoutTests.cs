using Aspire.Hosting;

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
        IDistributedApplicationBuilder item = DistributedApplication.CreateBuilder(new DistributedApplicationOptions
        {
            Args = [],
            AllowUnsecuredTransport = true,
            AssemblyName = "FinanceBot.AppHost",
            DisableDashboard = true,
            ProjectDirectory = "/Users/romanosipin/Desktop/personal/finance-bot-platform/aspire/FinanceBot.AppHost"
        });
        item.Configuration["Parameters:telegram-bot-token"] = "token";
        item.Configuration["Parameters:telegram-webhook-secret"] = "hook";
        item.Configuration["Parameters:telegram-key-secret"] = "key";
        IAppHostLayout app = new AppHostLayout();
        app.Add(item);
        Assert.Contains(item.Resources, note => string.Equals(note.Name, "postgres", StringComparison.Ordinal));
        Assert.Contains(item.Resources, note => string.Equals(note.Name, "finance-db", StringComparison.Ordinal));
        Assert.Contains(item.Resources, note => string.Equals(note.Name, "rabbitmq", StringComparison.Ordinal));
        Assert.Contains(item.Resources, note => string.Equals(note.Name, "finance-core", StringComparison.Ordinal));
        Assert.Contains(item.Resources, note => string.Equals(note.Name, "telegram-gateway", StringComparison.Ordinal));
    }
}
