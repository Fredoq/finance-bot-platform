using Finance.Application.Contracts.Messaging;
using FinanceCore.Domain.Workspace.Models;

namespace FinanceCore.Application.Workspace.Ports;

/// <summary>
/// Persists workspace views for downstream delivery.
/// </summary>
public interface IViewPort
{
    /// <summary>
    /// Saves a workspace view for downstream delivery.
    /// </summary>
    /// <param name="view">The workspace view.</param>
    /// <param name="context">The message context.</param>
    /// <param name="token">The cancellation token.</param>
    /// <returns>A task that completes when persistence finishes.</returns>
    ValueTask Save(WorkspaceView view, MessageContext context, CancellationToken token);
}
