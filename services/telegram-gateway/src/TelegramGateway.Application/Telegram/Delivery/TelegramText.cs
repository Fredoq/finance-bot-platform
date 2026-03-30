namespace TelegramGateway.Application.Telegram.Delivery;

internal sealed record TelegramText : TelegramOperation
{
    private const string Html = "HTML";
    private readonly ITelegramKeys item;
    public TelegramText(long chatId, string text, IReadOnlyList<TelegramRow> keys, ITelegramKeys item) : base("sendMessage")
    {
        ChatId = chatId;
        Text = !string.IsNullOrWhiteSpace(text) ? text.Trim() : throw new ArgumentException("Telegram text is required", nameof(text));
        this.item = item ?? throw new ArgumentNullException(nameof(item));
        Keys = this.item.Rows(keys, nameof(keys));
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
        reply_markup = item.Markup(Keys)
    };
}
