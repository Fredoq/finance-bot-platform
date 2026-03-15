namespace TelegramGateway.Application.Telegram.Delivery;

internal interface ITelegramDeliveryFlow
{
    ValueTask Run(string contract, ReadOnlyMemory<byte> body, CancellationToken token);
}
