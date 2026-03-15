namespace FinanceCore.Application.Runtime.Flow;

/// <summary>
/// Routes inbound messages to the matching application slice.
/// </summary>
public interface ICommandFlow
{
    /// <summary>
    /// Routes an inbound message body by contract name.
    /// </summary>
    /// <param name="contract">The contract name.</param>
    /// <param name="body">The serialized message body.</param>
    /// <param name="token">The cancellation token.</param>
    /// <returns>A task that completes when routing finishes.</returns>
    ValueTask Run(string contract, ReadOnlyMemory<byte> body, CancellationToken token);
}
