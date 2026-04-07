using TelegramGateway.Application.Telegram.Delivery;
using TelegramGateway.Infrastructure.Telegram;

namespace TelegramGateway.Api.Tests.Infrastructure;

internal sealed class MemoryContextStore : ITelegramContextStore
{
    private readonly Dictionary<string, TelegramContextNote> map = [];

    public void Save(string key, TelegramContextNote note, TimeSpan ttl)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        map[key] = note ?? throw new ArgumentNullException(nameof(note));
    }

    public TelegramContextNote? Load(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        return map.TryGetValue(key, out TelegramContextNote? item) ? item : null;
    }

    public void Delete(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        map.Remove(key);
    }
}
