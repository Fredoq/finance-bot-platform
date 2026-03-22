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
        list.Enqueue(new RecordNote(message.Contract, JsonSerializer.Serialize(message, RecordingWorkspaceJson.Value)));
        return ValueTask.CompletedTask;
    }
}

internal sealed record RecordNote
{
    internal RecordNote(string contract, string payload)
    {
        ArgumentNullException.ThrowIfNull(contract);
        ArgumentNullException.ThrowIfNull(payload);
        Contract = !string.IsNullOrWhiteSpace(contract) ? contract : throw new ArgumentException("Contract cannot be empty", nameof(contract));
        Payload = !string.IsNullOrWhiteSpace(payload) ? payload : throw new ArgumentException("Payload cannot be empty", nameof(payload));
    }
    internal string Contract { get; }
    internal string Payload { get; }
    internal MessageEnvelope<TMessage> Note<TMessage>() where TMessage : class => JsonSerializer.Deserialize<MessageEnvelope<TMessage>>(Payload, RecordingWorkspaceJson.Value) ?? throw new InvalidOperationException("Message capture failed");
}

internal static class RecordingWorkspaceJson
{
    internal static readonly JsonSerializerOptions Value = new(JsonSerializerDefaults.Web);
}
