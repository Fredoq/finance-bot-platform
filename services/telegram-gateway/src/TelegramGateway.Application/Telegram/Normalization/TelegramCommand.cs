using TelegramGateway.Application.Telegram.Contracts;

namespace TelegramGateway.Application.Telegram.Normalization;

internal sealed record TelegramCommand
{
    /// <summary>
    /// Normalizes a Telegram message into command parts.
    /// </summary>
    /// <param name="message">The Telegram message.</param>
    public TelegramCommand(TelegramMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);
        string text = message.Text?.Trim() ?? string.Empty;
        if (message.Entities is { Length: > 0 })
        {
            foreach (TelegramEntity item in message.Entities)
            {
                if (item.Type != "bot_command" || item.Offset != 0 || item.Length <= 0 || item.Offset + item.Length > text.Length)
                {
                    continue;
                }
                string head = text[item.Offset..(item.Offset + item.Length)];
                int mark = head.IndexOf('@');
                Name = mark >= 0 ? head[..mark] : head;
                Payload = text[item.Length..].Trim();
                return;
            }
        }
        if (!text.StartsWith('/'))
        {
            Name = string.Empty;
            Payload = string.Empty;
            return;
        }
        int edge = text.IndexOf(' ');
        string token = edge >= 0 ? text[..edge] : text;
        int spot = token.IndexOf('@');
        Name = spot >= 0 ? token[..spot] : token;
        Payload = edge < 0 ? string.Empty : text[(edge + 1)..].Trim();
    }
    /// <summary>
    /// Gets the normalized command name.
    /// </summary>
    public string Name { get; }
    /// <summary>
    /// Gets the normalized command payload.
    /// </summary>
    public string Payload { get; }
}
