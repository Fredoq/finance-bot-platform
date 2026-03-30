namespace TelegramGateway.Application.Telegram.Delivery;

internal sealed record TelegramContextNote
{
    internal TelegramContextNote(long chat, long message, string query)
    {
        ChatId = chat;
        MessageId = message;
        QueryId = query ?? throw new ArgumentNullException(nameof(query));
    }
    internal long ChatId { get; }
    internal long MessageId { get; }
    internal string QueryId { get; }
}
