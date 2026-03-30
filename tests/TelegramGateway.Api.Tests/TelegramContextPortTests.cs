using Microsoft.Extensions.Caching.Memory;
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
    public void Stores_notes()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var item = new TelegramContextPort(cache);
        var id = Guid.CreateVersion7();
        item.Save(id, "room", 100, 7, "callback-1");
        Assert.Equal(100, item.Envelope(id.ToString())?.ChatId);
        Assert.Equal(7, item.Envelope(id.ToString())?.MessageId);
        Assert.Equal("callback-1", item.Envelope(id.ToString())?.QueryId);
        Assert.Equal(100, item.Conversation("room")?.ChatId);
        Assert.Equal(7, item.Conversation("room")?.MessageId);
    }
    /// <summary>
    /// Verifies that update replaces the conversation note and clear removes it.
    /// </summary>
    [Fact(DisplayName = "Updates and clears the conversation note")]
    public void Clears_notes()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var item = new TelegramContextPort(cache);
        item.Update("room", 100, 7);
        Assert.Equal(7, item.Conversation("room")?.MessageId);
        item.Clear("room");
        Assert.Null(item.Conversation("room"));
    }
}
