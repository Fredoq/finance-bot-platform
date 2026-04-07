using Microsoft.Extensions.Logging;
using TelegramGateway.Application.Telegram.Delivery;

namespace TelegramGateway.Infrastructure.Telegram;

internal sealed class TelegramContextPort : ITelegramContextPort
{
    private static readonly TimeSpan span = TimeSpan.FromMinutes(15);
    private readonly ITelegramContextStore store;
    private readonly ILogger<TelegramContextPort> log;
    internal TelegramContextPort(ITelegramContextStore store, ILogger<TelegramContextPort> log)
    {
        this.store = store ?? throw new ArgumentNullException(nameof(store));
        this.log = log ?? throw new ArgumentNullException(nameof(log));
    }
    public void Save(Guid id, string room, long chat, long message, string query)
    {
        try
        {
            store.Save(EnvelopeKey(id.ToString()), new TelegramContextNote(chat, message, query), span);
            store.Save(ConversationKey(room), new TelegramContextNote(chat, message, string.Empty), span);
        }
        catch (Exception error)
        {
            log.LogWarning(error, "Telegram context save failed for room {Room}", room);
        }
    }
    public TelegramContextNote? Envelope(string id)
    {
        try
        {
            return store.Load(EnvelopeKey(id));
        }
        catch (Exception error)
        {
            log.LogWarning(error, "Telegram context envelope lookup failed for id {Id}", id);
            return null;
        }
    }
    public TelegramContextNote? Conversation(string room)
    {
        try
        {
            return store.Load(ConversationKey(room));
        }
        catch (Exception error)
        {
            log.LogWarning(error, "Telegram context conversation lookup failed for room {Room}", room);
            return null;
        }
    }
    public void Update(string room, long chat, long message)
    {
        try
        {
            store.Save(ConversationKey(room), new TelegramContextNote(chat, message, string.Empty), span);
        }
        catch (Exception error)
        {
            log.LogWarning(error, "Telegram context update failed for room {Room}", room);
        }
    }
    public void Clear(string room)
    {
        try
        {
            store.Delete(ConversationKey(room));
        }
        catch (Exception error)
        {
            log.LogWarning(error, "Telegram context clear failed for room {Room}", room);
        }
    }
    private static string EnvelopeKey(string id) => $"envelope:{id}";
    private static string ConversationKey(string room) => $"conversation:{room}";
}
