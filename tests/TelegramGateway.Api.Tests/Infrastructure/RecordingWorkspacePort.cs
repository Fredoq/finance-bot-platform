using System.Collections.Concurrent;
using System.Text.Json;
using Finance.Application.Contracts.Messaging;
using TelegramGateway.Application.Messaging;

namespace TelegramGateway.Api.Tests.Infrastructure;

internal sealed class RecordingWorkspacePort : IBusPort
{
    private readonly Exception? error;
    private readonly ConcurrentQueue<RecordNote> list = new();
    internal RecordingWorkspacePort(Exception? error = null) => this.error = error;
    public IReadOnlyCollection<RecordNote> Items => list.ToArray();
    public ValueTask Publish<TMessage>(MessageEnvelope<TMessage> message, CancellationToken token) where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(message);
        token.ThrowIfCancellationRequested();
        if (error is not null)
        {
            throw error;
        }
        list.Enqueue(new RecordNote(message.Contract, JsonSerializer.Serialize(message)));
        return ValueTask.CompletedTask;
    }
}

internal sealed record RecordNote
{
    internal RecordNote(string contract, string payload)
    {
        Contract = contract ?? throw new ArgumentNullException(nameof(contract));
        Payload = payload ?? throw new ArgumentNullException(nameof(payload));
    }
    internal string Contract { get; }
    internal string Payload { get; }
    internal MessageEnvelope<TMessage> Note<TMessage>() where TMessage : class => JsonSerializer.Deserialize<MessageEnvelope<TMessage>>(Payload) ?? throw new InvalidOperationException("Message capture failed");
}
