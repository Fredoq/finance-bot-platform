using Microsoft.Extensions.Logging.Abstractions;
using TelegramGateway.Application.Entry.Workspace;
using TelegramGateway.Application.Keys;
using TelegramGateway.Application.Messaging;
using TelegramGateway.Application.Telegram.Contracts;
using TelegramGateway.Application.Telegram.Flow;

namespace TelegramGateway.Application.Tests.Entry.Workspace;

/// <summary>
/// Covers the workspace entry mapping behavior.
/// Example:
/// <code>
/// var test = new WorkspaceFlowTests();
/// </code>
/// </summary>
public sealed class WorkspaceFlowTests
{
    /// <summary>
    /// Verifies that a private start message becomes a workspace command.
    /// Example:
    /// <code>
    /// await test.Maps_start_message();
    /// </code>
    /// </summary>
    [Fact(DisplayName = "Maps a private /start message into one workspace command")]
    public async Task Maps_start_message()
    {
        var port = new RecordingBusPort();
        var item = Flow(port);
        await item.Run(Update("/start", [Entity(0, 6)]), "trace-1", CancellationToken.None);
        Assert.Single(port.Items);
        Assert.Equal("workspace.requested", port.Items[0].Contract);
        Assert.Equal("Alex Doe", port.Items[0].Payload.Name);
        Assert.Equal(string.Empty, port.Items[0].Payload.Payload);
    }
    /// <summary>
    /// Verifies that a start message payload is preserved.
    /// Example:
    /// <code>
    /// await test.Maps_payload();
    /// </code>
    /// </summary>
    [Fact(DisplayName = "Maps a /start payload into the workspace payload field")]
    public async Task Maps_payload()
    {
        var port = new RecordingBusPort();
        var item = Flow(port);
        await item.Run(Update("/start promo-42", [Entity(0, 6)]), "trace-1", CancellationToken.None);
        Assert.Single(port.Items);
        Assert.Equal("promo-42", port.Items[0].Payload.Payload);
    }
    /// <summary>
    /// Verifies that a bot-specific start token is normalized.
    /// Example:
    /// <code>
    /// await test.Maps_bot_name();
    /// </code>
    /// </summary>
    [Fact(DisplayName = "Maps a /start@botname payload into the same workspace command")]
    public async Task Maps_bot_name()
    {
        var port = new RecordingBusPort();
        var item = Flow(port);
        await item.Run(Update("/start@financebot promo-42", [Entity(0, 17)]), "trace-1", CancellationToken.None);
        Assert.Single(port.Items);
        Assert.Equal("promo-42", port.Items[0].Payload.Payload);
    }
    /// <summary>
    /// Verifies that unsupported updates are ignored.
    /// Example:
    /// <code>
    /// await test.Ignores_update();
    /// </code>
    /// </summary>
    [Fact(DisplayName = "Ignores an update that is not a private start command")]
    public async Task Ignores_update()
    {
        var port = new RecordingBusPort();
        var item = Flow(port);
        await item.Run(Update("/help", [Entity(0, 5)]), "trace-1", CancellationToken.None);
        Assert.Empty(port.Items);
    }
    /// <summary>
    /// Verifies that opaque keys are deterministic and do not leak raw ids.
    /// Example:
    /// <code>
    /// await test.Maps_keys();
    /// </code>
    /// </summary>
    [Fact(DisplayName = "Builds deterministic opaque keys for the same Telegram ids")]
    public async Task Maps_keys()
    {
        var first = new RecordingBusPort();
        var second = new RecordingBusPort();
        var left = Flow(first);
        var right = Flow(second);
        await left.Run(Update("/start", [Entity(0, 6)]), "trace-1", CancellationToken.None);
        await right.Run(Update("/start", [Entity(0, 6)]), "trace-2", CancellationToken.None);
        Assert.Single(first.Items);
        Assert.Single(second.Items);
        Assert.Equal(first.Items[0].Payload.ActorKey, second.Items[0].Payload.ActorKey);
        Assert.Equal(first.Items[0].Payload.ConversationKey, second.Items[0].Payload.ConversationKey);
        Assert.DoesNotContain("42", first.Items[0].Payload.ActorKey, StringComparison.Ordinal);
        Assert.DoesNotContain("100", first.Items[0].Payload.ConversationKey, StringComparison.Ordinal);
    }
    /// <summary>
    /// Creates the flow under test.
    /// Example:
    /// <code>
    /// ITelegramFlow item = Flow(port);
    /// </code>
    /// </summary>
    /// <param name="port">The outbound port.</param>
    /// <returns>The workspace flow.</returns>
    private static ITelegramFlow Flow(IBusPort port)
    {
        return new TelegramFlow([new StartSlice(new OpaqueKey(), port)], NullLogger<TelegramFlow>.Instance);
    }
    /// <summary>
    /// Creates the Telegram update fixture.
    /// Example:
    /// <code>
    /// TelegramUpdate item = Update("/start", [Entity(0, 6)]);
    /// </code>
    /// </summary>
    /// <param name="text">The message text.</param>
    /// <param name="items">The entity collection.</param>
    /// <param name="type">The chat type.</param>
    /// <returns>The Telegram update fixture.</returns>
    private static TelegramUpdate Update(string text, TelegramEntity[] items, string type = "private")
    {
        return new TelegramUpdate
        {
            UpdateId = 7,
            Message = new TelegramMessage
            {
                MessageId = 8,
                Date = 1_736_000_000,
                Text = text,
                Entities = items,
                Chat = new TelegramChat
                {
                    Id = 100,
                    Type = type
                },
                From = new TelegramUser
                {
                    Id = 42,
                    FirstName = "Alex",
                    LastName = "Doe",
                    Username = "alex",
                    LanguageCode = "en"
                }
            }
        };
    }
    /// <summary>
    /// Creates a Telegram entity fixture.
    /// Example:
    /// <code>
    /// TelegramEntity item = Entity(0, 6);
    /// </code>
    /// </summary>
    /// <param name="offset">The entity offset.</param>
    /// <param name="length">The entity length.</param>
    /// <returns>The Telegram entity fixture.</returns>
    private static TelegramEntity Entity(int offset, int length)
    {
        return new TelegramEntity
        {
            Type = "bot_command",
            Offset = offset,
            Length = length
        };
    }
}
