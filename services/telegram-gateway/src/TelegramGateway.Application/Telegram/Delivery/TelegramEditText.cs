namespace TelegramGateway.Application.Telegram.Delivery;

internal sealed record TelegramEditText : TelegramOperation
{
    private const string Html = "HTML";
    public TelegramEditText(long chatId, long messageId, string text, IReadOnlyList<TelegramRow> keys) : base("editMessageText")
    {
        ChatId = chatId;
        MessageId = messageId > 0 ? messageId : throw new ArgumentOutOfRangeException(nameof(messageId));
        Text = !string.IsNullOrWhiteSpace(text) ? text.Trim() : throw new ArgumentException("Telegram text is required", nameof(text));
        ArgumentNullException.ThrowIfNull(keys);
        if (keys.Any(item => item is null))
        {
            throw new ArgumentException("Telegram keyboard row is required", nameof(keys));
        }
        Keys = Array.AsReadOnly(keys.ToArray());
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
        reply_markup = Keys.Count > 0 ? new
        {
            inline_keyboard = Keys.Select(Row).ToArray()
        } : null
    };
    private static object[] Row(TelegramRow item) => [.. item.Cells.Select(Cell)];
    private static object Cell(TelegramButton item) => string.IsNullOrWhiteSpace(item.Style)
        ? new
        {
            text = item.Text,
            callback_data = item.Data
        }
        : new
        {
            text = item.Text,
            callback_data = item.Data,
            style = item.Style
        };
}
