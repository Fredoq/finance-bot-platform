using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using TelegramGateway.Application.Messaging;
using TelegramGateway.Infrastructure.Messaging;
using TelegramGateway.Infrastructure.Telegram;

namespace TelegramGateway.Api.Tests.Infrastructure;

internal sealed class GatewayApiFactory : WebApplicationFactory<Program>
{
    private readonly IDictionary<string, string?> note;
    private readonly IBusPort? port;
    private readonly IBrokerState? state;
    private readonly Action<IServiceCollection>? amend;
    private readonly bool hosted;
    internal GatewayApiFactory(IDictionary<string, string?> note, IBusPort? port = null, IBrokerState? state = null, Action<IServiceCollection>? amend = null, bool hosted = true)
    {
        this.note = note ?? throw new ArgumentNullException(nameof(note));
        this.port = port;
        this.state = state;
        this.amend = amend;
        this.hosted = hosted;
    }
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, data) => data.AddInMemoryCollection(note));
        builder.ConfigureServices(data =>
        {
            data.RemoveAll<ITelegramContextStore>();
            data.AddSingleton<ITelegramContextStore, MemoryContextStore>();
            if (!hosted)
            {
                data.RemoveAll<IHostedService>();
            }
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
            amend?.Invoke(data);
        });
    }
}
