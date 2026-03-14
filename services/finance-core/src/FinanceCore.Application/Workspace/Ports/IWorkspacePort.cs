using Finance.Application.Contracts.Entry;
using Finance.Application.Contracts.Messaging;

namespace FinanceCore.Application.Workspace.Ports;

/// <summary>
/// Persists the workspace state for inbound commands.
/// </summary>
public interface IWorkspacePort
{
    /// <summary>
    /// Saves the workspace request result.
    /// </summary>
    /// <param name="message">The inbound envelope.</param>
    /// <param name="token">The cancellation token.</param>
    /// <returns>A task that completes when persistence finishes.</returns>
    ValueTask Save(MessageEnvelope<WorkspaceRequestedCommand> message, CancellationToken token);
}
