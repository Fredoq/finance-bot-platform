using System.Text.Json;
using Finance.Application.Contracts.Entry;
using Finance.Application.Contracts.Messaging;
using FinanceCore.Application.Runtime.Ports;
using FinanceCore.Domain.Workspace.Models;
using FinanceCore.Infrastructure.Persistence.Postgres.Outbox;

namespace FinanceCore.Api.Tests.Workspace;

/// <summary>
/// Covers workspace view outbox mapping behavior.
/// </summary>
public sealed class OutboxViewPortTests
{
    /// <summary>
    /// Verifies that the workspace view is mapped to the public outbound contract.
    /// </summary>
    /// <returns>A task that completes when the assertions finish.</returns>
    [Fact(DisplayName = "Maps a workspace view to the outbound workspace view contract")]
    public async Task Maps_view_to_contract()
    {
        var port = new OutboxStub();
        var item = new OutboxViewPort(port);
        var view = new WorkspaceView(new WorkspaceIdentity("actor-9", "room-9"), new WorkspaceProfile("Alex", "en"), new WorkspaceState("home", "{\"accounts\":[]}", 1), ["account.add"], false, true, DateTimeOffset.UtcNow);
        var mark = new MessageContext("trace", "cause", "view-key");
        await item.Save(view, mark, default);
        MessageEnvelope<WorkspaceViewRequestedCommand> note = port.Note();
        Assert.Equal("workspace.view.requested", note.Contract);
        Assert.Equal("workspace.view.requested", port.RoutingKey);
        Assert.Equal("finance-core", note.Source);
        Assert.Equal("home", note.Payload.State);
        Assert.Equal("{\"accounts\":[]}", note.Payload.StateData);
        Assert.True(note.Payload.IsNewWorkspace);
    }
    private sealed class OutboxStub : IOutboxPort
    {
        private string json = string.Empty;
        public string RoutingKey { get; private set; } = string.Empty;
        public ValueTask Save<TMessage>(MessageEnvelope<TMessage> message, string routingKey, CancellationToken token) where TMessage : class
        {
            RoutingKey = routingKey;
            json = JsonSerializer.Serialize(message);
            return ValueTask.CompletedTask;
        }
        public MessageEnvelope<WorkspaceViewRequestedCommand> Note() => JsonSerializer.Deserialize<MessageEnvelope<WorkspaceViewRequestedCommand>>(json) ?? throw new InvalidOperationException("Outbox envelope was not captured");
    }
}
