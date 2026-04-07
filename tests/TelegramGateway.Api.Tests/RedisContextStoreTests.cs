using StackExchange.Redis;
using TelegramGateway.Application.Telegram.Delivery;
using TelegramGateway.Infrastructure.Configuration;
using TelegramGateway.Infrastructure.Telegram;
using Testcontainers.Redis;

namespace TelegramGateway.Api.Tests;

/// <summary>
/// Covers Redis-backed Telegram transport context persistence behavior.
/// </summary>
public sealed class RedisContextStoreTests : IAsyncLifetime
{
    private readonly RedisContainer redis = new RedisBuilder("redis:7.2").Build();
    /// <summary>
    /// Starts the Redis test dependency.
    /// </summary>
    /// <returns>A task that completes when the dependency is ready.</returns>
    public Task InitializeAsync() => redis.StartAsync();
    /// <summary>
    /// Stops the Redis test dependency.
    /// </summary>
    /// <returns>A task that completes when cleanup finishes.</returns>
    public Task DisposeAsync() => redis.DisposeAsync().AsTask();
    /// <summary>
    /// Verifies that the Redis store persists and removes transport context payloads.
    /// </summary>
    [Fact(DisplayName = "Stores and removes Telegram context notes in Redis")]
    public void Roundtrip()
    {
        using IConnectionMultiplexer link = ConnectionMultiplexer.Connect(redis.GetConnectionString());
        var store = new RedisContextStore(link, new RedisOptions { ConnectionString = redis.GetConnectionString() });
        store.Save("envelope:test", new TelegramContextNote(100, 7, "callback-1"), TimeSpan.FromMinutes(15));
        TelegramContextNote? note = store.Load("envelope:test");
        Assert.Equal(100, note?.ChatId);
        Assert.Equal(7, note?.MessageId);
        Assert.Equal("callback-1", note?.QueryId);
        store.Delete("envelope:test");
        Assert.Null(store.Load("envelope:test"));
    }
}
