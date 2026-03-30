namespace TelegramGateway.Application.Telegram.Delivery;

internal static class TelegramKeys
{
    public static IReadOnlyList<TelegramRow> Read(IReadOnlyList<TelegramRow> keys, string name)
    {
        ArgumentNullException.ThrowIfNull(keys);
        if (keys.Any(item => item is null))
        {
            throw new ArgumentException("Telegram keyboard row is required", name);
        }
        return Array.AsReadOnly(keys.ToArray());
    }
    public static object? Markup(IReadOnlyList<TelegramRow> keys) => keys.Count > 0 ? new
    {
        inline_keyboard = keys.Select(Row).ToArray()
    } : null;
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
