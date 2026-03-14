using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FinanceCore.Infrastructure.Messaging.RabbitMq;

internal sealed class RabbitMqBoot : IHostedService
{
    private const int count = 5;
    private static readonly TimeSpan pause = TimeSpan.FromSeconds(1);
    private readonly IBrokerState state;
    private readonly ILogger<RabbitMqBoot> log;
    internal RabbitMqBoot(IBrokerState state, ILogger<RabbitMqBoot> log)
    {
        this.state = state ?? throw new ArgumentNullException(nameof(state));
        this.log = log ?? throw new ArgumentNullException(nameof(log));
    }
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
                log.LogWarning(error, "RabbitMQ warm-up failed on attempt {Attempt} of {Count}", item, count);
                await Task.Delay(span, cancellationToken);
                span = TimeSpan.FromSeconds(Math.Min(span.TotalSeconds * 2, 15));
            }
        }
    }
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
