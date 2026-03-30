namespace TelegramGateway.Application.Telegram.Delivery;

internal interface ITelegramKeys
{
    IReadOnlyList<TelegramRow> Rows(IReadOnlyList<TelegramRow> keys, string name);
    object Markup(IReadOnlyList<TelegramRow> keys);
}

internal sealed class TelegramKeys : ITelegramKeys
{
    public IReadOnlyList<TelegramRow> Rows(IReadOnlyList<TelegramRow> keys, string name)
    {
        ArgumentNullException.ThrowIfNull(keys);
        if (keys.Any(item => item is null))
        {
            throw new ArgumentException("Telegram keyboard row is required", name);
        }
        return Array.AsReadOnly(keys.ToArray());
    }
    public object Markup(IReadOnlyList<TelegramRow> keys) => keys.Count > 0 ? new
    {
        inline_keyboard = keys.Select(Row).ToArray()
    } : new
    {
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
