namespace TelegramGateway.Application.Telegram.Delivery;

internal sealed record TelegramButton
{
    public TelegramButton(string text, string data)
    {
        Text = !string.IsNullOrWhiteSpace(text) ? text.Trim() : throw new ArgumentException("Telegram button text is required", nameof(text));
        Data = !string.IsNullOrWhiteSpace(data) ? data.Trim() : throw new ArgumentException("Telegram button data is required", nameof(data));
    }
    public string Text { get; }
    public string Data { get; }
}
