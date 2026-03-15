namespace TelegramGateway.Application.Telegram.Delivery;

internal interface ITelegramDeliverySlice
{
    string Contract { get; }
    ValueTask Run(ReadOnlyMemory<byte> body, CancellationToken token);
}
