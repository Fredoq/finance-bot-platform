namespace TelegramGateway.Application.Telegram.Delivery;

internal interface ITelegramDeliverySlice
{
    bool Match(string contract);
    ValueTask Run(ReadOnlyMemory<byte> body, CancellationToken token);
}
