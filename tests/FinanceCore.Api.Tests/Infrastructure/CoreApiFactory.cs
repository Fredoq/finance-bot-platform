using FinanceCore.Api;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using FinanceCore.Infrastructure.Persistence.Postgres.Migrations;
using FinanceCore.Infrastructure.Messaging.RabbitMq;

namespace FinanceCore.Api.Tests.Infrastructure;

internal sealed class CoreApiFactory : WebApplicationFactory<Program>
{
    private readonly IDictionary<string, string?> note;
    private readonly bool loops;
    internal CoreApiFactory(IDictionary<string, string?> note, bool loops = true)
    {
        this.note = note ?? throw new ArgumentNullException(nameof(note));
        this.loops = loops;
    }
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, data) => data.AddInMemoryCollection(note));
        if (!loops)
        {
            builder.ConfigureServices(items =>
            {
                items.RemoveAll<IHostedService>();
                items.AddHostedService(item => item.GetRequiredService<MigrationBoot>());
                items.AddHostedService(item => item.GetRequiredService<RabbitMqBoot>());
                items.RemoveAll<RabbitMqOutboxLoop>();
                items.RemoveAll<RabbitMqIngressLoop>();
            });
        }
    }
}
