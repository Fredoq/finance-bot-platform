namespace TelegramGateway.Api.Tests.Infrastructure;

internal static class WebhookUpdate
{
    internal static HttpClient Client(GatewayApiFactory host)
    {
        HttpClient item = host.CreateClient();
        item.DefaultRequestHeaders.Add("X-Telegram-Bot-Api-Secret-Token", "test-secret");
        return item;
    }
    internal static object Body(string text, long date = 1_736_000_000)
    {
        ArgumentNullException.ThrowIfNull(text);
        int length = text.IndexOf(' ');
        length = length >= 0 ? length : text.Length;
        object[]? entities = text.StartsWith('/')
            ? [new
            {
                type = "bot_command",
                offset = 0,
                length
            }]
            : null;
        return new
        {
            update_id = 7,
            message = new
            {
                message_id = 8,
                date,
                text,
                entities,
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
    internal static object Callback(string data)
    {
        ArgumentNullException.ThrowIfNull(data);
        return new
        {
            update_id = 9,
            callback_query = new
            {
                id = "callback-1",
                data,
                from = new
                {
                    id = 42,
                    first_name = "Alex",
                    last_name = "Doe",
                    username = "alex",
                    language_code = "en"
                },
                message = new
                {
                    message_id = 8,
                    date = 1_736_000_000,
                    chat = new
                    {
                        id = 100,
                        type = "private"
                    }
                }
            }
        };
    }
}
