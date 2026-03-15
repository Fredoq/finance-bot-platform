namespace TelegramGateway.Application.Telegram.Delivery;

internal interface ITelegramPort
{
    ValueTask Send(TelegramOperation message, CancellationToken token);
}
