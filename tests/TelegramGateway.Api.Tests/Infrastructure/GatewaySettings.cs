namespace TelegramGateway.Api.Tests.Infrastructure;

internal static class GatewaySettings
{
    internal static Dictionary<string, string?> Note(string name, string host, string port, string vhost, string username, string password) => new()
    {
        ["Telegram:Webhook:SecretToken"] = "test-secret",
        ["Telegram:Bot:Token"] = "test-bot-token",
        ["Telegram:Bot:BaseUrl"] = "https://api.telegram.org",
        ["Telegram:Bot:TimeoutSeconds"] = "10",
        ["Telegram:Keys:CurrentSecret"] = "test-current-secret",
        ["RabbitMq:Host"] = host,
        ["RabbitMq:Port"] = port,
        ["RabbitMq:VirtualHost"] = vhost,
        ["RabbitMq:Username"] = username,
        ["RabbitMq:Password"] = password,
        ["RabbitMq:CommandExchange"] = "finance.command",
        ["RabbitMq:DeliveryExchange"] = "finance.delivery",
        ["RabbitMq:DeliveryQueue"] = "telegram-gateway.delivery",
        ["RabbitMq:DeliveryRetryQueue"] = "telegram-gateway.delivery.retry",
        ["RabbitMq:DeliveryDeadQueue"] = "telegram-gateway.delivery.dead",
        ["RabbitMq:Client"] = name,
        ["RabbitMq:DeliveryPrefetch"] = "16",
        ["RabbitMq:DeliveryRetryDelaySeconds"] = "1",
        ["RabbitMq:DeliveryMaxAttempts"] = "5"
    };
}
