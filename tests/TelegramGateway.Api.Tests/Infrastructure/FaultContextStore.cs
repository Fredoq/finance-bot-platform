using TelegramGateway.Application.Telegram.Delivery;
using TelegramGateway.Infrastructure.Telegram;

namespace TelegramGateway.Api.Tests.Infrastructure;

internal sealed class FaultContextStore : ITelegramContextStore
{
    public void Save(string key, TelegramContextNote note, TimeSpan ttl) => throw new InvalidOperationException("Context store write failed");
    public TelegramContextNote? Load(string key) => throw new InvalidOperationException("Context store read failed");
    public void Delete(string key) => throw new InvalidOperationException("Context store delete failed");
}
