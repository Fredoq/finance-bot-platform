namespace TelegramGateway.Application.Telegram.Delivery;

internal abstract record TelegramOperation
{
    protected TelegramOperation(string method) => Method = !string.IsNullOrWhiteSpace(method) ? method.Trim() : throw new ArgumentException("Telegram method is required", nameof(method));
    public string Method { get; }
}
