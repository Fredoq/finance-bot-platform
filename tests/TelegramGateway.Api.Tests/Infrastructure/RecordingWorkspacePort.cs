using System.Collections.Concurrent;
using System.Text.Json;
using Finance.Application.Contracts.Entry;
using Finance.Application.Contracts.Messaging;
using TelegramGateway.Application.Messaging;

namespace TelegramGateway.Api.Tests.Infrastructure;

/// <summary>
/// Captures published workspace commands for API tests.
/// Example:
/// <code>
/// var port = new RecordingWorkspacePort();
/// </code>
/// </summary>
internal sealed class RecordingWorkspacePort(Exception? error = null) : IBusPort
{
    private readonly ConcurrentQueue<MessageEnvelope<WorkspaceRequestedCommand>> list = new();
    /// <summary>
    /// Gets the captured publish collection.
    /// Example:
    /// <code>
    /// IReadOnlyCollection&lt;MessageEnvelope&lt;WorkspaceRequestedCommand&gt;&gt; items = port.Items;
    /// </code>
    /// </summary>
    public IReadOnlyCollection<MessageEnvelope<WorkspaceRequestedCommand>> Items => list.ToArray();
    /// <summary>
    /// Publishes the envelope into the in-memory capture.
    /// Example:
    /// <code>
    /// await port.Publish(message, token);
    /// </code>
    /// </summary>
    /// <param name="message">The envelope to capture.</param>
    /// <param name="token">The cancellation token.</param>
    /// <returns>A task that completes when the capture finishes.</returns>
    public ValueTask Publish<TMessage>(MessageEnvelope<TMessage> message, CancellationToken token) where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(message);
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
