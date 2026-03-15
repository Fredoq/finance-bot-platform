using System.Collections.Concurrent;
using TelegramGateway.Application.Telegram.Delivery;

namespace TelegramGateway.Api.Tests.Infrastructure;

internal sealed class RecordingTelegramPort : ITelegramPort
{
    private readonly Exception? error;
    private readonly ConcurrentQueue<TelegramOperation> list = new();
    internal RecordingTelegramPort(Exception? error = null) => this.error = error;
    public IReadOnlyCollection<TelegramOperation> Items => list.ToArray();
    public ValueTask Send(TelegramOperation message, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(message);
        token.ThrowIfCancellationRequested();
        if (error is not null)
        {
            throw error;
        }
        list.Enqueue(message);
        return ValueTask.CompletedTask;
    }
}
