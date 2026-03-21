using FinanceCore.Application.Runtime.Flow;
using FinanceCore.Application.Runtime.Ports;
using FinanceCore.Application.Workspace.Ports;
using FinanceCore.Domain.Workspace.Policies;
using FinanceCore.Infrastructure.Configuration.Postgres;
using FinanceCore.Infrastructure.Configuration.RabbitMq;
using FinanceCore.Infrastructure.Messaging.RabbitMq;
using FinanceCore.Infrastructure.Observability.Checks;
using FinanceCore.Infrastructure.Persistence.Postgres.Inbox;
using FinanceCore.Infrastructure.Persistence.Postgres.Migrations;
using FinanceCore.Infrastructure.Persistence.Postgres.Outbox;
using FinanceCore.Infrastructure.Persistence.Postgres.Workspace;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace FinanceCore.Infrastructure.Composition;

/// <summary>
/// Registers the infrastructure services required by the finance core runtime.
/// </summary>
public static class InfrastructureSetup
{
    /// <summary>
    /// Adds the PostgreSQL and RabbitMQ infrastructure services.
    /// </summary>
    /// <param name="items">The service collection.</param>
    /// <returns>The configured service collection.</returns>
    public static IServiceCollection AddFinanceCoreInfrastructure(this IServiceCollection items)
    {
        items.AddOptionsWithValidateOnStart<PostgresOptions>().BindConfiguration(PostgresOptions.Section).ValidateDataAnnotations();
        items.AddOptionsWithValidateOnStart<RabbitMqOptions>().BindConfiguration(RabbitMqOptions.Section).ValidateDataAnnotations();
        items.AddSingleton<IWorkspaceActions, WorkspaceActions>();
        items.AddSingleton(item =>
        {
            PostgresOptions data = item.GetRequiredService<Microsoft.Extensions.Options.IOptions<PostgresOptions>>().Value;
            return new NpgsqlDataSourceBuilder(data.ConnectionString).Build();
        });
        items.AddSingleton<IBrokerState>(item => new RabbitMqLink(item.GetRequiredService<Microsoft.Extensions.Options.IOptions<RabbitMqOptions>>(), item.GetRequiredService<Microsoft.Extensions.Logging.ILogger<RabbitMqLink>>()));
        items.AddSingleton(item => new PostgresOutboxPort(item.GetRequiredService<NpgsqlDataSource>()));
        items.AddSingleton(item => new PostgresInboxPort(item.GetRequiredService<NpgsqlDataSource>()));
        items.AddSingleton<IOutboxPort>(item => item.GetRequiredService<PostgresOutboxPort>());
        items.AddSingleton<IInboxPort>(item => item.GetRequiredService<PostgresInboxPort>());
        items.AddSingleton<IViewPort>(item => new OutboxViewPort(item.GetRequiredService<IOutboxPort>()));
        items.AddSingleton<IWorkspacePort>(item => new PostgresWorkspacePort(item.GetRequiredService<NpgsqlDataSource>(), item.GetRequiredService<IWorkspaceActions>()));
        items.AddSingleton(item => new MigrationBoot(item.GetRequiredService<Microsoft.Extensions.Options.IOptions<PostgresOptions>>(), item.GetRequiredService<Microsoft.Extensions.Logging.ILoggerFactory>(), item.GetRequiredService<Microsoft.Extensions.Logging.ILogger<MigrationBoot>>()));
        items.AddSingleton(item => new RabbitMqBoot(item.GetRequiredService<IBrokerState>(), item.GetRequiredService<Microsoft.Extensions.Logging.ILogger<RabbitMqBoot>>()));
        items.AddSingleton(item => new RabbitMqOutboxLoop(item.GetRequiredService<IBrokerState>(), item.GetRequiredService<Microsoft.Extensions.Options.IOptions<RabbitMqOptions>>(), item.GetRequiredService<PostgresOutboxPort>(), item.GetRequiredService<Microsoft.Extensions.Logging.ILogger<RabbitMqOutboxLoop>>()));
        items.AddSingleton(item => new RabbitMqIngressLoop(item.GetRequiredService<IBrokerState>(), item.GetRequiredService<Microsoft.Extensions.Options.IOptions<RabbitMqOptions>>(), item.GetRequiredService<ICommandFlow>(), item.GetRequiredService<Microsoft.Extensions.Logging.ILogger<RabbitMqIngressLoop>>()));
        items.AddHostedService(item => item.GetRequiredService<MigrationBoot>());
        items.AddHostedService(item => item.GetRequiredService<RabbitMqBoot>());
        items.AddHostedService(item => item.GetRequiredService<RabbitMqOutboxLoop>());
        items.AddHostedService(item => item.GetRequiredService<RabbitMqIngressLoop>());
        items.AddHealthChecks().AddCheck<PostgresHealthCheck>("postgres", tags: ["ready"]).AddCheck<BrokerHealthCheck>("broker", tags: ["ready"]);
        return items;
    }
}
