using System.Text.Json;
using StackExchange.Redis;
using TelegramGateway.Application.Telegram.Delivery;
using TelegramGateway.Infrastructure.Configuration;

namespace TelegramGateway.Infrastructure.Telegram;

internal sealed class RedisContextStore : ITelegramContextStore
{
    private static readonly JsonSerializerOptions json = new(JsonSerializerDefaults.Web);
    private readonly IConnectionMultiplexer link;
    private readonly string prefix;

    internal RedisContextStore(IConnectionMultiplexer link, RedisOptions options)
    {
        this.link = link ?? throw new ArgumentNullException(nameof(link));
        ArgumentNullException.ThrowIfNull(options);
        prefix = options.KeyPrefix ?? string.Empty;
    }

    public void Save(string key, TelegramContextNote note, TimeSpan ttl)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(note);
        string body = JsonSerializer.Serialize(new Entry
        {
            ChatId = note.ChatId,
            MessageId = note.MessageId,
            QueryId = note.QueryId
        }, json);
        if (!link.GetDatabase().StringSet(Name(key), body, ttl))
        {
            throw new InvalidOperationException($"Redis context write failed for key {key}");
        }
    }

    public TelegramContextNote? Load(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        RedisValue item = link.GetDatabase().StringGet(Name(key));
        if (!item.HasValue)
        {
            return null;
        }
        Entry note = JsonSerializer.Deserialize<Entry>(item.ToString(), json) ?? throw new InvalidOperationException($"Redis context payload is invalid for key {key}");
        return new TelegramContextNote(note.ChatId, note.MessageId, note.QueryId ?? string.Empty);
    }

    public void Delete(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        _ = link.GetDatabase().KeyDelete(Name(key));
    }

    private string Name(string key) => string.IsNullOrWhiteSpace(prefix) ? key : $"{prefix}{key}";

    private sealed class Entry
    {
        public long ChatId { get; init; }
        public long MessageId { get; init; }
        public string QueryId { get; init; } = string.Empty;
    }
}
