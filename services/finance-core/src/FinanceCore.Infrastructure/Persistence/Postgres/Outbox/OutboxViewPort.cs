using Finance.Application.Contracts.Entry;
using Finance.Application.Contracts.Messaging;
using FinanceCore.Application.Runtime.Ports;
using FinanceCore.Application.Workspace.Ports;
using FinanceCore.Domain.Workspace.Models;

namespace FinanceCore.Infrastructure.Persistence.Postgres.Outbox;

internal sealed class OutboxViewPort : IViewPort
{
    private const string Contract = "workspace.view.requested";
    private const string RoutingKey = "workspace.view.requested";
    private const string Source = "finance-core";
    private readonly IOutboxPort port;
    internal OutboxViewPort(IOutboxPort port) => this.port = port ?? throw new ArgumentNullException(nameof(port));
    public ValueTask Save(WorkspaceView view, MessageContext context, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(view);
        ArgumentNullException.ThrowIfNull(context);
        var item = new WorkspaceViewRequestedCommand(view.Identity, view.Profile, view.State.Code, view.State.Data, view.Actions, view.IsNewUser, view.IsNewWorkspace, view.OccurredUtc);
        var note = new MessageEnvelope<WorkspaceViewRequestedCommand>(Guid.CreateVersion7(), Contract, view.OccurredUtc, new MessageContext(context.CorrelationId, context.CausationId, context.IdempotencyKey), Source, item);
        return port.Save(note, RoutingKey, token);
    }
}
