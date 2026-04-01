using Microsoft.Extensions.Caching.Memory;
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
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var item = new TelegramContextPort(cache);
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
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var item = new TelegramContextPort(cache);
        item.Update("room", 100, 7);
        TelegramContextNote? conversation = item.Conversation("room");
        Assert.Equal(7, conversation?.MessageId);
        Assert.True(string.IsNullOrEmpty(conversation?.QueryId));
        item.Clear("room");
        Assert.Null(item.Conversation("room"));
    }
}
