namespace TelegramGateway.Application.Telegram.Delivery;

internal sealed record TelegramText : TelegramOperation
{
    private const string Html = "HTML";
    public TelegramText(long chatId, string text, IReadOnlyList<TelegramRow> keys) : base("sendMessage")
    {
        ChatId = chatId;
        Text = !string.IsNullOrWhiteSpace(text) ? text.Trim() : throw new ArgumentException("Telegram text is required", nameof(text));
        Keys = TelegramKeys.Read(keys, nameof(keys));
        ParseMode = Html;
    }
    public long ChatId { get; }
    public string Text { get; }
    public IReadOnlyList<TelegramRow> Keys { get; }
    public string ParseMode { get; }
    public override object Payload() => new
    {
        chat_id = ChatId,
        text = Text,
        parse_mode = ParseMode,
        reply_markup = TelegramKeys.Markup(Keys)
    };
}
