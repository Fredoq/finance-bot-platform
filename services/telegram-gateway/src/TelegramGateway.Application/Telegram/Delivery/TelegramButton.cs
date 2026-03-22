namespace TelegramGateway.Application.Telegram.Delivery;

internal sealed record TelegramButton
{
    private static readonly string[] list = ["danger", "success", "primary"];
    public TelegramButton(string text, string data, string style = "")
    {
        Text = !string.IsNullOrWhiteSpace(text) ? text.Trim() : throw new ArgumentException("Telegram button text is required", nameof(text));
        Data = !string.IsNullOrWhiteSpace(data) ? data.Trim() : throw new ArgumentException("Telegram button data is required", nameof(data));
        if (!string.IsNullOrWhiteSpace(style) && !list.Contains(style, StringComparer.Ordinal))
        {
            throw new ArgumentException("Telegram button style is invalid", nameof(style));
        }
        Style = style?.Trim() ?? string.Empty;
    }
    public string Text { get; }
    public string Data { get; }
    public string Style { get; }
}
