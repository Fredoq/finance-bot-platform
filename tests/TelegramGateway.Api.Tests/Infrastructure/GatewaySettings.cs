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
        ["RabbitMq:DeliveryMaxAttempts"] = "5",
        ["Redis:ConnectionString"] = "localhost:6379",
        ["Redis:KeyPrefix"] = ""
    };
    internal static Dictionary<string, string?> Note(string name, Uri address)
    {
        ArgumentNullException.ThrowIfNull(address);
        string[] data = address.UserInfo.Split(':', 2, StringSplitOptions.None);
        return Note(name, address.Host, address.Port.ToString(), Vhost(address.AbsolutePath), data.Length > 0 ? Uri.UnescapeDataString(data[0]) : string.Empty, data.Length > 1 ? Uri.UnescapeDataString(data[1]) : string.Empty);
    }
    internal static string Vhost(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        string item = Uri.UnescapeDataString(path).TrimStart('/');
        return string.IsNullOrWhiteSpace(item) ? "/" : item;
    }
}
