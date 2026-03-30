namespace TelegramGateway.Application.Telegram.Delivery;

internal sealed record TelegramEditText : TelegramOperation
{
    private const string Html = "HTML";
    private readonly ITelegramKeys item;
    public TelegramEditText(long chatId, long messageId, string text, IReadOnlyList<TelegramRow> keys, ITelegramKeys item) : base("editMessageText")
    {
        ChatId = chatId;
        MessageId = messageId > 0 ? messageId : throw new ArgumentOutOfRangeException(nameof(messageId));
        Text = !string.IsNullOrWhiteSpace(text) ? text.Trim() : throw new ArgumentException("Telegram text is required", nameof(text));
        this.item = item ?? throw new ArgumentNullException(nameof(item));
        Keys = this.item.Rows(keys, nameof(keys));
        ParseMode = Html;
    }
    public long ChatId { get; }
    public long MessageId { get; }
    public string Text { get; }
    public IReadOnlyList<TelegramRow> Keys { get; }
    public string ParseMode { get; }
    public override object Payload() => new
    {
        chat_id = ChatId,
        message_id = MessageId,
        text = Text,
        parse_mode = ParseMode,
        reply_markup = item.Markup(Keys)
    };
}
