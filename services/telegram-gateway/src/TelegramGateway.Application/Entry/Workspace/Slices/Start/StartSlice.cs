using Finance.Application.Contracts.Entry;
using Finance.Application.Contracts.Messaging;
using TelegramGateway.Application.Keys;
using TelegramGateway.Application.Messaging;
using TelegramGateway.Application.Telegram.Contracts;
using TelegramGateway.Application.Telegram.Flow;
using TelegramGateway.Application.Telegram.Normalization;
using TelegramGateway.Domain.Entry.Workspace;

namespace TelegramGateway.Application.Entry.Workspace;

internal sealed class StartSlice : ITelegramSlice
{
    private const string Contract = "workspace.requested";
    private const string Source = "telegram-gateway";
    private readonly IOpaqueKey key;
    private readonly IBusPort port;
    private static readonly long MaxUnixSeconds = DateTimeOffset.MaxValue.ToUnixTimeSeconds();
    internal StartSlice(IOpaqueKey text, IBusPort bus)
    {
        key = text ?? throw new ArgumentNullException(nameof(text));
        port = bus ?? throw new ArgumentNullException(nameof(bus));
    }
    /// <summary>
    /// Determines whether the update is a private `/start` command.
    /// </summary>
    /// <param name="update">The inbound Telegram update.</param>
    /// <returns><see langword="true"/> when the start slice matches; otherwise <see langword="false"/>.</returns>
    public bool Match(TelegramUpdate update)
    {
        ArgumentNullException.ThrowIfNull(update);
        if (update.Message is null || update.Message.Chat is null || update.Message.From is null || update.Message.Chat.Type != "private" || update.Message.Date <= 0 || update.Message.Date > MaxUnixSeconds)
        {
            return false;
        }
        var command = new TelegramCommand(update.Message);
        return command.Name == "/start";
    }
    /// <summary>
    /// Publishes a workspace request for the matched start command.
    /// </summary>
    /// <param name="update">The inbound Telegram update.</param>
    /// <param name="trace">The trace identifier.</param>
    /// <param name="token">The cancellation token.</param>
    /// <returns>A task that completes when the publish finishes.</returns>
    public async ValueTask Run(TelegramUpdate update, string trace, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(update);
        ArgumentException.ThrowIfNullOrWhiteSpace(trace);
        if (!Match(update))
        {
            throw new InvalidOperationException("Telegram update did not match the start slice");
        }
        TelegramMessage message = update.Message!;
        TelegramChat chat = message.Chat!;
        TelegramUser sender = message.From!;
        var command = new TelegramCommand(message);
        var user = new TelegramIdentity(sender);
        var actor = new ActorKey(key.Text("actor", "telegram:user", user.Id));
        var room = new ConversationKey(key.Text("conversation", "telegram:chat", chat.Id));
        var payload = new StartPayload(command.Payload);
        var note = new WorkspaceRequestedCommand(new WorkspaceIdentity(actor.Value, room.Value), new WorkspaceProfile(user.Name, user.Locale), payload.Value, DateTimeOffset.FromUnixTimeSeconds(message.Date));
        string cause = $"edge-update-{update.UpdateId}";
        string stamp = $"workspace-requested-{update.UpdateId}";
        var data = new MessageEnvelope<WorkspaceRequestedCommand>(Guid.CreateVersion7(), Contract, note.OccurredUtc, trace, cause, stamp, Source, note);
        await port.Publish(data, token);
    }
}
