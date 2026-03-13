using System.Collections.Concurrent;
using System.Text.Json;
using Finance.Application.Contracts.Entry;
using Finance.Application.Contracts.Messaging;
using TelegramGateway.Application.Messaging;

namespace TelegramGateway.Application.Tests.Entry.Workspace;

/// <summary>
/// Captures published workspace commands for application tests.
/// Example:
/// <code>
/// var port = new RecordingBusPort();
/// </code>
/// </summary>
internal sealed class RecordingBusPort : IBusPort
{
    private readonly ConcurrentQueue<MessageEnvelope<WorkspaceRequestedCommand>> list = new();
    /// <summary>
    /// Gets the captured publish collection.
    /// Example:
    /// <code>
    /// IReadOnlyList&lt;MessageEnvelope&lt;WorkspaceRequestedCommand&gt;&gt; items = port.Items;
    /// </code>
    /// </summary>
    public IReadOnlyList<MessageEnvelope<WorkspaceRequestedCommand>> Items => list.ToArray();
    /// <summary>
    /// Captures the published workspace command.
    /// Example:
    /// <code>
    /// await port.Publish(message, token);
    /// </code>
    /// </summary>
    /// <param name="message">The envelope to capture.</param>
    /// <param name="token">The cancellation token.</param>
    /// <returns>A completed task.</returns>
    public ValueTask Publish<TMessage>(MessageEnvelope<TMessage> message, CancellationToken token) where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(message);
        var data = JsonSerializer.SerializeToUtf8Bytes(message);
        var item = JsonSerializer.Deserialize<MessageEnvelope<WorkspaceRequestedCommand>>(data);
        if (item is null)
        {
            throw new InvalidOperationException("Message capture failed");
        }
        list.Enqueue(item);
        return ValueTask.CompletedTask;
    }
}
