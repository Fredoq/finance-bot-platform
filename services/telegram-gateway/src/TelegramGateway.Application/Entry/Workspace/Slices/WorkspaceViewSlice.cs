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
    private readonly IWorkspaceScreen screen;
    private readonly ITelegramKeys keys;
    internal WorkspaceViewSlice(IOpaqueKey key, ITelegramPort port, ITelegramContextPort context, IWorkspaceScreen screen, ITelegramKeys keys)
    {
        Contract = Name;
        this.key = key ?? throw new ArgumentNullException(nameof(key));
        this.port = port ?? throw new ArgumentNullException(nameof(port));
        this.context = context ?? throw new ArgumentNullException(nameof(context));
        this.screen = screen ?? throw new ArgumentNullException(nameof(screen));
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
        TelegramText note = screen.Message(chatId, item.Payload);
        if (Editable(item.Payload.Frame.State))
        {
            TelegramContextNote? edit = context.Envelope(item.Context.CausationId) ?? context.Conversation(room);
            if (edit is not null)
            {
                await port.Send(new TelegramEditText(edit.ChatId, edit.MessageId, note.Text, note.Keys, keys), token);
                context.Update(room, edit.ChatId, edit.MessageId);
                return;
            }
        }
        await port.Send(note, token);
        if (!Editable(item.Payload.Frame.State))
        {
            context.Clear(room);
        }
    }

    private static bool Editable(string state) => state.StartsWith("transaction.recent.", StringComparison.Ordinal) || string.Equals(state, "summary.month", StringComparison.Ordinal);
}
