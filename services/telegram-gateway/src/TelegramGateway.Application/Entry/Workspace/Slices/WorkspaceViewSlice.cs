using System.Text.Json;
using Finance.Application.Contracts.Entry;
using Finance.Application.Contracts.Messaging;
using TelegramGateway.Application.Keys;
using TelegramGateway.Application.Telegram.Delivery;

namespace TelegramGateway.Application.Entry.Workspace.Slices;

internal sealed class WorkspaceViewSlice : ITelegramDeliverySlice
{
    private static readonly JsonSerializerOptions json = new(JsonSerializerDefaults.Web);
    private const string Name = "workspace.view.requested";
    private readonly IOpaqueKey key;
    private readonly ITelegramPort port;
    private readonly ITelegramContextPort context;
    private readonly ITelegramKeys keys;
    internal WorkspaceViewSlice(IOpaqueKey key, ITelegramPort port, ITelegramContextPort context, ITelegramKeys keys)
    {
        Contract = Name;
        this.key = key ?? throw new ArgumentNullException(nameof(key));
        this.port = port ?? throw new ArgumentNullException(nameof(port));
        this.context = context ?? throw new ArgumentNullException(nameof(context));
        this.keys = keys ?? throw new ArgumentNullException(nameof(keys));
    }
    public string Contract { get; }
    public async ValueTask Run(ReadOnlyMemory<byte> body, CancellationToken token)
    {
        MessageEnvelope<WorkspaceViewRequestedCommand>? item;
        try
        {
            item = JsonSerializer.Deserialize<MessageEnvelope<WorkspaceViewRequestedCommand>>(body.Span, json);
        }
        catch (JsonException error)
        {
            throw new DeliveryException("Telegram delivery payload is invalid", false, error);
        }
        if (item is null)
        {
            throw new DeliveryException("Telegram delivery payload is missing", false);
        }
        if (item.Payload?.Identity is null || string.IsNullOrWhiteSpace(item.Payload.Identity.ConversationKey))
        {
            throw new DeliveryException("Telegram delivery payload identity is missing", false);
        }
        long chatId;
        try
        {
            chatId = key.Id("conversation", "telegram:chat", item.Payload.Identity.ConversationKey);
        }
        catch (ArgumentException error)
        {
            throw new DeliveryException("Telegram conversation key is invalid", false, error);
        }
        string room = item.Payload.Identity.ConversationKey;
        TelegramText note = WorkspaceScreen.Message(chatId, item.Payload, keys);
        if (item.Payload.Frame.State.StartsWith("transaction.recent.", StringComparison.Ordinal))
        {
            TelegramContextNote? edit = context.Envelope(item.Context.CausationId) ?? context.Conversation(room);
            if (edit is not null)
            {
                await port.Send(new TelegramEditText(edit.ChatId, edit.MessageId, note.Text, note.Keys, keys), token);
                context.Update(room, edit.ChatId, edit.MessageId);
                return;
            }
        }
        else
        {
            context.Clear(room);
        }
        await port.Send(note, token);
    }
}
