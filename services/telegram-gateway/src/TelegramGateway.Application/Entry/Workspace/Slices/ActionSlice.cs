using Finance.Application.Contracts.Entry;
using Finance.Application.Contracts.Messaging;
using TelegramGateway.Application.Keys;
using TelegramGateway.Application.Messaging;
using TelegramGateway.Application.Telegram.Contracts;
using TelegramGateway.Application.Telegram.Delivery;
using TelegramGateway.Application.Telegram.Flow;
using TelegramGateway.Application.Telegram.Normalization;
using TelegramGateway.Domain.Entry.Workspace;

namespace TelegramGateway.Application.Entry.Workspace.Slices;

internal sealed class ActionSlice : ITelegramSlice
{
    private const string Contract = "workspace.input.requested";
    private const string Source = "telegram-gateway";
    private readonly IOpaqueKey key;
    private readonly IBusPort port;
    private readonly ITelegramPort gate;
    private readonly ITelegramContextPort context;
    internal ActionSlice(IOpaqueKey key, IBusPort port, ITelegramPort gate, ITelegramContextPort context)
    {
        this.key = key ?? throw new ArgumentNullException(nameof(key));
        this.port = port ?? throw new ArgumentNullException(nameof(port));
        this.gate = gate ?? throw new ArgumentNullException(nameof(gate));
        this.context = context ?? throw new ArgumentNullException(nameof(context));
    }
    public bool Match(TelegramUpdate update)
    {
        ArgumentNullException.ThrowIfNull(update);
        return update.Callback is { Id: not null, From: not null, Message.Chat: not null, Data: not null } && update.Callback.Message.Chat!.Type == "private" && !string.IsNullOrWhiteSpace(update.Callback.Data);
    }
    public async ValueTask Run(TelegramUpdate update, string trace, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(update);
        ArgumentException.ThrowIfNullOrWhiteSpace(trace);
        if (!Match(update))
        {
            throw new InvalidOperationException("Telegram update did not match the action slice");
        }
        TelegramCallback item = update.Callback!;
        TelegramIdentity user = new(item.From!);
        var actor = new ActorKey(key.Text("actor", "telegram:user", user.Id));
        var room = new ConversationKey(key.Text("conversation", "telegram:chat", item.Message!.Chat!.Id));
        var body = new WorkspaceInputRequestedCommand(new WorkspaceIdentity(actor.Value, room.Value), new WorkspaceProfile(user.Name, user.Locale), "action", item.Data!, DateTimeOffset.UtcNow);
        string cause = $"edge-update-{update.UpdateId}";
        string stamp = $"workspace-input-{update.UpdateId}";
        var note = new MessageEnvelope<WorkspaceInputRequestedCommand>(Guid.CreateVersion7(), Contract, body.OccurredUtc, new MessageContext(trace, cause, stamp), Source, body);
        context.Save(note.MessageId, room.Value, item.Message.Chat!.Id, item.Message.MessageId, item.Id!);
        await port.Publish(note, token);
        await gate.Send(new TelegramCallbackAck(item.Id!), token);
    }
}
