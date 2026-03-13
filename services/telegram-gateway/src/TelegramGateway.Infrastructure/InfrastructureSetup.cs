using Microsoft.Extensions.DependencyInjection;
using TelegramGateway.Application.Messaging;
using TelegramGateway.Infrastructure.Configuration;
using TelegramGateway.Infrastructure.Messaging;
using TelegramGateway.Infrastructure.Observability;

namespace TelegramGateway.Infrastructure;

/// <summary>
/// Registers the infrastructure services required by the gateway runtime.
/// Example:
/// <code>
/// builder.Services.AddTelegramGatewayInfrastructure();
/// </code>
/// </summary>
public static class InfrastructureSetup
{
    /// <summary>
    /// Adds the RabbitMQ transport, startup warm-up, and broker health checks.
    /// Example:
    /// <code>
    /// builder.Services.AddTelegramGatewayInfrastructure();
    /// </code>
    /// </summary>
    /// <param name="items">The service collection.</param>
    /// <returns>The same service collection.</returns>
    public static IServiceCollection AddTelegramGatewayInfrastructure(this IServiceCollection items)
    {
        items.AddOptionsWithValidateOnStart<RabbitMqOptions>().BindConfiguration(RabbitMqOptions.Section).ValidateDataAnnotations();
        items.AddSingleton<IBrokerState, RabbitMqLink>();
        items.AddSingleton<IBusPort, RabbitMqBusPort>();
        items.AddHostedService<RabbitMqBoot>();
        items.AddHealthChecks().AddCheck<BrokerHealthCheck>("broker", tags: ["ready"]);
        return items;
    }
}
