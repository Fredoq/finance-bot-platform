using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace TelegramGateway.Infrastructure.Messaging;

internal sealed class RabbitMqBoot(IBrokerState state, ILogger<RabbitMqBoot> log) : IHostedService
{
    private const int count = 5;
    private static readonly TimeSpan pause = TimeSpan.FromSeconds(1);
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        TimeSpan span = pause;
        for (int item = 1; item <= count; item++)
        {
            try
            {
                await state.Ensure(cancellationToken);
                return;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception error) when (item < count)
            {
                log.LogWarning(error, "RabbitMQ warm-up failed");
                await Task.Delay(span, cancellationToken);
                span = TimeSpan.FromSeconds(Math.Min(span.TotalSeconds * 2, 15));
            }
        }
        await state.Ensure(cancellationToken);
    }
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
