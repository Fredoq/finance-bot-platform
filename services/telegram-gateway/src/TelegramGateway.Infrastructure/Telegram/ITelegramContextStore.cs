using TelegramGateway.Application.Telegram.Delivery;

namespace TelegramGateway.Infrastructure.Telegram;

internal interface ITelegramContextStore
{
    void Save(string key, TelegramContextNote note, TimeSpan ttl);
    TelegramContextNote? Load(string key);
    void Delete(string key);
}
