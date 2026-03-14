using Microsoft.Extensions.Hosting;

namespace TelegramGateway.Infrastructure.Messaging;

internal sealed class RabbitMqBoot(IBrokerState state) : IHostedService
{
    /// <summary>
    /// Warms up the broker connection during host startup.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes when the broker warm-up finishes.</returns>
    public Task StartAsync(CancellationToken cancellationToken) => state.Ensure(cancellationToken).AsTask();
    /// <summary>
    /// Stops the hosted service.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A completed task.</returns>
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
