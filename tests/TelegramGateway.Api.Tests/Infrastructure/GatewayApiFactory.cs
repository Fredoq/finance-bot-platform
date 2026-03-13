using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TelegramGateway.Application.Messaging;
using TelegramGateway.Infrastructure.Messaging;

namespace TelegramGateway.Api.Tests.Infrastructure;

/// <summary>
/// Provides a configurable API host for gateway integration tests.
/// Example:
/// <code>
/// await using var host = new GatewayApiFactory(note, port, state);
/// </code>
/// </summary>
internal sealed class GatewayApiFactory(IDictionary<string, string?> note, IBusPort? port = null, IBrokerState? state = null) : WebApplicationFactory<Program>
{
    /// <summary>
    /// Configures the test host.
    /// Example:
    /// <code>
    /// host.ConfigureWebHost(builder);
    /// </code>
    /// </summary>
    /// <param name="builder">The web host builder.</param>
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, data) => data.AddInMemoryCollection(note));
        builder.ConfigureServices(data =>
        {
            if (port is not null)
            {
                data.RemoveAll<IBusPort>();
                data.AddSingleton(port);
            }
            if (state is not null)
            {
                data.RemoveAll<IBrokerState>();
                data.AddSingleton(state);
            }
        });
    }
}
