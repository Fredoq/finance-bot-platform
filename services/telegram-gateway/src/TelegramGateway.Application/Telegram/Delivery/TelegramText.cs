namespace TelegramGateway.Application.Telegram.Delivery;

internal sealed record TelegramText : TelegramOperation
{
    public TelegramText(long chatId, string text, IReadOnlyList<TelegramRow> keys) : base("sendMessage")
    {
        ChatId = chatId;
        Text = !string.IsNullOrWhiteSpace(text) ? text.Trim() : throw new ArgumentException("Telegram text is required", nameof(text));
        ArgumentNullException.ThrowIfNull(keys);
        if (keys.Any(item => item is null))
        {
            throw new ArgumentException("Telegram keyboard row is required", nameof(keys));
        }
        Keys = Array.AsReadOnly(keys.ToArray());
    }
    public long ChatId { get; }
    public string Text { get; }
    public IReadOnlyList<TelegramRow> Keys { get; }
    public override object Payload() => new
    {
        chat_id = ChatId,
        text = Text,
        reply_markup = Keys.Count > 0 ? new
        {
            inline_keyboard = Keys.Select(item => item.Cells.Select(item => new
            {
                text = item.Text,
                callback_data = item.Data
            }).ToArray()).ToArray()
        } : null
    };
}
