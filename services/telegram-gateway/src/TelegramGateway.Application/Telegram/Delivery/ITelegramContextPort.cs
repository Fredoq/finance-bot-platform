namespace TelegramGateway.Application.Telegram.Delivery;

internal interface ITelegramContextPort
{
    void Save(Guid id, string room, long chat, long message, string query);
    TelegramContextNote? Envelope(string id);
    TelegramContextNote? Conversation(string room);
    void Update(string room, long chat, long message);
    void Clear(string room);
}
