using Microsoft.Extensions.Logging.Abstractions;
using TelegramGateway.Api.Tests.Infrastructure;
using TelegramGateway.Application.Telegram.Delivery;
using TelegramGateway.Infrastructure.Telegram;

namespace TelegramGateway.Api.Tests;

/// <summary>
/// Covers Telegram transport context persistence behavior.
/// </summary>
public sealed class TelegramContextPortTests
{
    /// <summary>
    /// Verifies that save stores both envelope and conversation lookups.
    /// </summary>
    [Fact(DisplayName = "Stores envelope and conversation notes")]
    public void Store()
    {
        var item = new TelegramContextPort(new MemoryContextStore(), NullLogger<TelegramContextPort>.Instance);
        var id = Guid.CreateVersion7();
        item.Save(id, "room", 100, 7, "callback-1");
        TelegramContextNote? envelope = item.Envelope(id.ToString());
        TelegramContextNote? conversation = item.Conversation("room");
        Assert.Equal(100, envelope?.ChatId);
        Assert.Equal(7, envelope?.MessageId);
        Assert.Equal("callback-1", envelope?.QueryId);
        Assert.Equal(100, conversation?.ChatId);
        Assert.Equal(7, conversation?.MessageId);
    }
    /// <summary>
    /// Verifies that update replaces the conversation note and clear removes it.
    /// </summary>
    [Fact(DisplayName = "Updates and clears the conversation note")]
    public void Clear()
    {
        var item = new TelegramContextPort(new MemoryContextStore(), NullLogger<TelegramContextPort>.Instance);
        item.Update("room", 100, 7);
        TelegramContextNote? conversation = item.Conversation("room");
        Assert.Equal(7, conversation?.MessageId);
        Assert.True(string.IsNullOrEmpty(conversation?.QueryId));
        item.Clear("room");
        Assert.Null(item.Conversation("room"));
    }

    /// <summary>
    /// Verifies that store faults degrade to cache misses instead of bubbling to callers.
    /// </summary>
    [Fact(DisplayName = "Treats context store faults as misses")]
    public void Miss()
    {
        var item = new TelegramContextPort(new FaultContextStore(), NullLogger<TelegramContextPort>.Instance);
        item.Save(Guid.CreateVersion7(), "room", 100, 7, "callback-1");
        Assert.Null(item.Envelope("envelope"));
        Assert.Null(item.Conversation("room"));
        item.Update("room", 100, 7);
        item.Clear("room");
    }
}
