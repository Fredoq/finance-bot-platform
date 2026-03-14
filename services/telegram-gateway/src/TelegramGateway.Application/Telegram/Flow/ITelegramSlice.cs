using TelegramGateway.Application.Telegram.Contracts;

namespace TelegramGateway.Application.Telegram.Flow;

internal interface ITelegramSlice
{
    bool Match(TelegramUpdate update);
    ValueTask Run(TelegramUpdate update, string trace, CancellationToken token);
}
