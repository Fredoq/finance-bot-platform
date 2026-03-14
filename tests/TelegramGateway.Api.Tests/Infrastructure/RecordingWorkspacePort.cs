using System.Collections.Concurrent;
using System.Text.Json;
using Finance.Application.Contracts.Entry;
using Finance.Application.Contracts.Messaging;
using TelegramGateway.Application.Messaging;

namespace TelegramGateway.Api.Tests.Infrastructure;

internal sealed class RecordingWorkspacePort(Exception? error = null) : IBusPort
{
    private readonly ConcurrentQueue<MessageEnvelope<WorkspaceRequestedCommand>> list = new();
    /// <summary>
    /// Gets the captured publish collection.
    /// </summary>
    public IReadOnlyCollection<MessageEnvelope<WorkspaceRequestedCommand>> Items => list.ToArray();
    /// <summary>
    /// Publishes the envelope into the in-memory capture.
    /// </summary>
    /// <param name="message">The message envelope.</param>
    /// <param name="token">The cancellation token.</param>
    /// <returns>A task that completes when the capture finishes.</returns>
    public ValueTask Publish<TMessage>(MessageEnvelope<TMessage> message, CancellationToken token) where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(message);
        token.ThrowIfCancellationRequested();
        if (error is not null)
        {
            throw error;
        }
        byte[] data = JsonSerializer.SerializeToUtf8Bytes(message);
        MessageEnvelope<WorkspaceRequestedCommand> item = JsonSerializer.Deserialize<MessageEnvelope<WorkspaceRequestedCommand>>(data) ?? throw new InvalidOperationException("Message capture failed");
        list.Enqueue(item);
        return ValueTask.CompletedTask;
    }
}
