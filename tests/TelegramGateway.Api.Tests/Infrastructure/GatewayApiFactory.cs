using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TelegramGateway.Application.Messaging;
using TelegramGateway.Infrastructure.Messaging;

namespace TelegramGateway.Api.Tests.Infrastructure;

internal sealed class GatewayApiFactory(IDictionary<string, string?> note, IBusPort? port = null, IBrokerState? state = null) : WebApplicationFactory<Program>
{
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
