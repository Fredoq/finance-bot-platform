using Finance.Application.Contracts.Entry;
using Finance.Application.Contracts.Messaging;

namespace FinanceCore.Application.Workspace.Ports;

/// <summary>
/// Persists workspace input commands.
/// </summary>
public interface IWorkspaceInputPort
{
    /// <summary>
    /// Saves the workspace input result.
    /// </summary>
    /// <param name="message">The inbound envelope.</param>
    /// <param name="token">The cancellation token.</param>
    /// <returns>A task that completes when persistence finishes.</returns>
    ValueTask Save(MessageEnvelope<WorkspaceInputRequestedCommand> message, CancellationToken token);
}
