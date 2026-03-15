namespace TelegramGateway.Application.Telegram.Delivery;

internal sealed record TelegramText : TelegramOperation
{
    public TelegramText(long chatId, string text, IReadOnlyList<TelegramRow> keys) : base("sendMessage")
    {
        ChatId = chatId;
        Text = !string.IsNullOrWhiteSpace(text) ? text.Trim() : throw new ArgumentException("Telegram text is required", nameof(text));
        ArgumentNullException.ThrowIfNull(keys);
        TelegramRow[] list = keys.Where(item => item is not null).ToArray();
        Keys = Array.AsReadOnly(list);
    }
    public long ChatId { get; }
    public string Text { get; }
    public IReadOnlyList<TelegramRow> Keys { get; }
}
