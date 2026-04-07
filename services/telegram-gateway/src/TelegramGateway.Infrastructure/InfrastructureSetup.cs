using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using TelegramGateway.Application.Keys;
using TelegramGateway.Application.Messaging;
using TelegramGateway.Application.Telegram.Delivery;
using TelegramGateway.Infrastructure.Configuration;
using TelegramGateway.Infrastructure.Messaging;
using TelegramGateway.Infrastructure.Observability;
using TelegramGateway.Infrastructure.Telegram;

namespace TelegramGateway.Infrastructure;

/// <summary>
/// Registers the infrastructure services required by the gateway runtime.
/// </summary>
public static class InfrastructureSetup
{
    /// <summary>
    /// Adds the RabbitMQ transport, startup warm-up, and broker health checks.
    /// </summary>
    /// <param name="items">The service collection.</param>
    /// <returns>The configured service collection.</returns>
    public static IServiceCollection AddTelegramGatewayInfrastructure(this IServiceCollection items)
    {
        items.AddOptionsWithValidateOnStart<RabbitMqOptions>().BindConfiguration(RabbitMqOptions.Section).ValidateDataAnnotations();
        items.AddOptionsWithValidateOnStart<RedisOptions>().BindConfiguration(RedisOptions.Section).ValidateDataAnnotations();
        items.AddOptionsWithValidateOnStart<TelegramBotOptions>().BindConfiguration(TelegramBotOptions.Section).ValidateDataAnnotations();
        items.AddOptionsWithValidateOnStart<OpaqueKeyOptions>().BindConfiguration(OpaqueKeyOptions.Section).ValidateDataAnnotations();
        items.AddSingleton<IOpaqueKey>(item =>
        {
            OpaqueKeyOptions note = item.GetRequiredService<Microsoft.Extensions.Options.IOptions<OpaqueKeyOptions>>().Value;
            return new OpaqueKey(note.CurrentSecret, note.PreviousSecrets);
        });
        items.AddSingleton<IConnectionMultiplexer>(item =>
        {
            RedisOptions note = item.GetRequiredService<IOptions<RedisOptions>>().Value;
            return ConnectionMultiplexer.Connect(note.ConnectionString);
        });
        items.AddSingleton<IBrokerState, RabbitMqLink>();
        items.AddSingleton<IBusPort, RabbitMqBusPort>();
        items.AddSingleton<ITelegramContextStore>(item =>
        {
            RedisOptions note = item.GetRequiredService<IOptions<RedisOptions>>().Value;
            return new RedisContextStore(item.GetRequiredService<IConnectionMultiplexer>(), note);
        });
        items.AddSingleton<ITelegramContextPort>(item => new TelegramContextPort(item.GetRequiredService<ITelegramContextStore>(), item.GetRequiredService<ILogger<TelegramContextPort>>()));
        items.AddHttpClient<ITelegramPort, TelegramBotPort>((item, client) =>
        {
            TelegramBotOptions note = item.GetRequiredService<Microsoft.Extensions.Options.IOptions<TelegramBotOptions>>().Value;
            client.BaseAddress = new Uri(note.BaseUrl[^1] == '/' ? note.BaseUrl : $"{note.BaseUrl}/", UriKind.Absolute);
            client.Timeout = TimeSpan.FromSeconds(note.TimeoutSeconds);
        });
        items.AddHostedService<RabbitMqBoot>();
        items.AddHostedService<RabbitMqDeliveryLoop>();
        items.AddHealthChecks().AddCheck<BrokerHealthCheck>("broker", tags: ["ready"]);
        return items;
    }
}
