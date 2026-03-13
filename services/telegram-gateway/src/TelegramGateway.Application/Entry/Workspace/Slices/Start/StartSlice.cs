using Finance.Application.Contracts.Entry;
using Finance.Application.Contracts.Messaging;
using TelegramGateway.Application.Keys;
using TelegramGateway.Application.Messaging;
using TelegramGateway.Application.Telegram.Contracts;
using TelegramGateway.Application.Telegram.Flow;
using TelegramGateway.Application.Telegram.Normalization;
using TelegramGateway.Domain.Entry.Workspace;

namespace TelegramGateway.Application.Entry.Workspace;

/// <summary>
/// Processes a private Telegram `/start` interaction into one workspace command publish.
/// Example:
/// <code>
/// bool item = slice.Match(update);
/// await slice.Run(update, "trace-1", token);
/// </code>
/// </summary>
internal sealed class StartSlice(IOpaqueKey text, IBusPort bus) : ITelegramSlice
{
    private const string Contract = "workspace.requested";
    private const string Source = "telegram-gateway";
    /// <summary>
    /// Indicates whether the slice supports the inbound update.
    /// Example:
    /// <code>
    /// bool item = slice.Match(update);
    /// </code>
    /// </summary>
    /// <param name="update">The inbound update.</param>
    /// <returns><see langword="true"/> when the slice supports the update.</returns>
    public bool Match(TelegramUpdate update)
    {
        ArgumentNullException.ThrowIfNull(update);
        if (update.Message is null || update.Message.Chat is null || update.Message.From is null || update.Message.Chat.Type != "private" || update.Message.Date <= 0)
        {
            return false;
        }
        var command = new TelegramCommand(update.Message);
        return command.Name == "/start";
    }
    /// <summary>
    /// Publishes the workspace command for a matched `/start` update.
    /// Example:
    /// <code>
    /// await slice.Run(update, "trace-1", token);
    /// </code>
    /// </summary>
    /// <param name="update">The inbound update.</param>
    /// <param name="trace">The correlation value for the current request.</param>
    /// <param name="token">The cancellation token.</param>
    /// <returns>A task that completes when the publish finishes.</returns>
    public async ValueTask Run(TelegramUpdate update, string trace, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(update);
        ArgumentException.ThrowIfNullOrWhiteSpace(trace);
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(bus);
        if (!Match(update))
        {
            throw new InvalidOperationException("Telegram update did not match the start slice");
        }
        var message = update.Message ?? throw new InvalidOperationException("Telegram update did not contain a message");
        var chat = message.Chat ?? throw new InvalidOperationException("Telegram update did not contain a chat");
        var sender = message.From ?? throw new InvalidOperationException("Telegram update did not contain a sender");
        var command = new TelegramCommand(message);
        var user = new TelegramIdentity(sender);
        var actor = new ActorKey(text.Text("actor", "telegram:user", user.Id));
        var room = new ConversationKey(text.Text("conversation", "telegram:chat", chat.Id));
        var payload = new StartPayload(command.Payload);
        var note = new WorkspaceRequestedCommand(actor.Value, room.Value, user.Name, user.Locale, payload.Value, DateTimeOffset.FromUnixTimeSeconds(message.Date));
        var data = new MessageEnvelope<WorkspaceRequestedCommand>(Guid.CreateVersion7(), Contract, note.OccurredUtc, trace, $"edge-update-{update.UpdateId}", $"edge-update-{update.UpdateId}", Source, note);
        await bus.Publish(data, token);
    }
}
