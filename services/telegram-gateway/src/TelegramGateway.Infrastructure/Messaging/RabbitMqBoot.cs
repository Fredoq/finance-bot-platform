using Microsoft.Extensions.Hosting;

namespace TelegramGateway.Infrastructure.Messaging;

/// <summary>
/// Warms the broker connection and topology during service startup.
/// Example:
/// <code>
/// await boot.StartAsync(cancellationToken);
/// </code>
/// </summary>
internal sealed class RabbitMqBoot(IBrokerState state) : IHostedService
{
    /// <summary>
    /// Starts the warm-up flow.
    /// Example:
    /// <code>
    /// await boot.StartAsync(cancellationToken);
    /// </code>
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes when the broker is ready.</returns>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        return state.Ensure(cancellationToken).AsTask();
    }
    /// <summary>
    /// Stops the warm-up flow.
    /// Example:
    /// <code>
    /// await boot.StopAsync(cancellationToken);
    /// </code>
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A completed task.</returns>
    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
