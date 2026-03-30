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

internal sealed class TextSlice : ITelegramSlice
{
    private const string Contract = "workspace.input.requested";
    private const string Source = "telegram-gateway";
    private static readonly long MaxUnixSeconds = DateTimeOffset.MaxValue.ToUnixTimeSeconds();
    private readonly IOpaqueKey key;
    private readonly IBusPort port;
    private readonly ITelegramContextPort context;
    internal TextSlice(IOpaqueKey key, IBusPort port, ITelegramContextPort context)
    {
        this.key = key ?? throw new ArgumentNullException(nameof(key));
        this.port = port ?? throw new ArgumentNullException(nameof(port));
        this.context = context ?? throw new ArgumentNullException(nameof(context));
    }
    public bool Match(TelegramUpdate update)
    {
        ArgumentNullException.ThrowIfNull(update);
        if (update.Message is null || update.Message.Chat is null || update.Message.From is null || update.Message.Chat.Type != "private" || update.Message.Date <= 0 || update.Message.Date > MaxUnixSeconds || string.IsNullOrWhiteSpace(update.Message.Text))
        {
            return false;
        }
        var command = new TelegramCommand(update.Message);
        return string.IsNullOrWhiteSpace(command.Name);
    }
    public async ValueTask Run(TelegramUpdate update, string trace, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(update);
        ArgumentException.ThrowIfNullOrWhiteSpace(trace);
        if (!Match(update))
        {
            throw new InvalidOperationException("Telegram update did not match the text slice");
        }
        TelegramMessage item = update.Message!;
        TelegramIdentity user = new(item.From!);
        var actor = new ActorKey(key.Text("actor", "telegram:user", user.Id));
        var room = new ConversationKey(key.Text("conversation", "telegram:chat", item.Chat!.Id));
        var body = new WorkspaceInputRequestedCommand(new WorkspaceIdentity(actor.Value, room.Value), new WorkspaceProfile(user.Name, user.Locale), "text", item.Text!.Trim(), DateTimeOffset.FromUnixTimeSeconds(item.Date));
        string cause = $"edge-update-{update.UpdateId}";
        string stamp = $"workspace-input-{update.UpdateId}";
        var note = new MessageEnvelope<WorkspaceInputRequestedCommand>(Guid.CreateVersion7(), Contract, body.OccurredUtc, new MessageContext(trace, cause, stamp), Source, body);
        TelegramContextNote? current = context.Conversation(room.Value);
        if (current is not null)
        {
            context.Save(note.MessageId, room.Value, current.ChatId, current.MessageId, string.Empty);
        }
        await port.Publish(note, token);
    }
}
