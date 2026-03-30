using Microsoft.Extensions.Caching.Memory;
using TelegramGateway.Application.Telegram.Delivery;

namespace TelegramGateway.Infrastructure.Telegram;

internal sealed class TelegramContextPort : ITelegramContextPort
{
    private static readonly TimeSpan span = TimeSpan.FromMinutes(15);
    private readonly IMemoryCache cache;
    internal TelegramContextPort(IMemoryCache cache) => this.cache = cache ?? throw new ArgumentNullException(nameof(cache));
    public void Save(Guid id, string room, long chat, long message, string query)
    {
        cache.Set(EnvelopeKey(id.ToString()), new TelegramContextNote(chat, message, query), span);
        Update(room, chat, message);
    }
    public TelegramContextNote? Envelope(string id) => cache.TryGetValue(EnvelopeKey(id), out TelegramContextNote? item) ? item : null;
    public TelegramContextNote? Conversation(string room) => cache.TryGetValue(ConversationKey(room), out TelegramContextNote? item) ? item : null;
    public void Update(string room, long chat, long message) => cache.Set(ConversationKey(room), new TelegramContextNote(chat, message, string.Empty), span);
    public void Clear(string room) => cache.Remove(ConversationKey(room));
    private static string EnvelopeKey(string id) => $"envelope:{id}";
    private static string ConversationKey(string room) => $"conversation:{room}";
}
