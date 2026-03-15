using System.Net.Http;

namespace TelegramGateway.Api.Tests.Infrastructure;

internal static class WebhookUpdate
{
    internal static HttpClient Client(GatewayApiFactory host)
    {
        HttpClient item = host.CreateClient();
        item.DefaultRequestHeaders.Add("X-Telegram-Bot-Api-Secret-Token", "test-secret");
        return item;
    }
    internal static object Body(string text, long date = 1_736_000_000) => new
    {
        update_id = 7,
        message = new
        {
            message_id = 8,
            date,
            text,
            entities = new[]
                {
                    new
                    {
                        type = "bot_command",
                        offset = 0,
                        length = text.Contains(' ', StringComparison.Ordinal) ? text.IndexOf(' ', StringComparison.Ordinal) : text.Length
                    }
                },
            chat = new
            {
                id = 100,
                type = "private"
            },
            from = new
            {
                id = 42,
                first_name = "Alex",
                last_name = "Doe",
                username = "alex",
                language_code = "en"
            }
        }
    };
}
