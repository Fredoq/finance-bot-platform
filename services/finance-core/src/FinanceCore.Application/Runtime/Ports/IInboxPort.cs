using Finance.Application.Contracts.Entry;
using Finance.Application.Contracts.Messaging;

namespace FinanceCore.Application.Runtime.Ports;

/// <summary>
/// Persists inbound envelopes for idempotent command handling.
/// </summary>
public interface IInboxPort
{
    /// <summary>
    /// Saves an inbound workspace request envelope.
    /// </summary>
    /// <param name="message">The inbound envelope.</param>
    /// <param name="token">The cancellation token.</param>
    /// <returns>A task that completes when persistence finishes.</returns>
    ValueTask Save(MessageEnvelope<WorkspaceRequestedCommand> message, CancellationToken token);
}
