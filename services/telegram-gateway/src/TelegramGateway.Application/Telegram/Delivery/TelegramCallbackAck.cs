namespace TelegramGateway.Application.Telegram.Delivery;

internal sealed record TelegramCallbackAck : TelegramOperation
{
    public TelegramCallbackAck(string query) : base("answerCallbackQuery") => Query = !string.IsNullOrWhiteSpace(query) ? query.Trim() : throw new ArgumentException("Telegram callback query is required", nameof(query));
    public string Query { get; }
    public override object Payload() => new
    {
        callback_query_id = Query
    };
}
