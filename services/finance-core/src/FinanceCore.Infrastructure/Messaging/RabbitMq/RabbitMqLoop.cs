using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FinanceCore.Infrastructure.Messaging.RabbitMq;

internal abstract class RabbitMqLoop : BackgroundService
{
    protected RabbitMqLoop(ILogger log) => Log = log ?? throw new ArgumentNullException(nameof(log));
    protected ILogger Log { get; }
    protected sealed override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Run(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception item)
            {
                Log.LogError(item, Failure());
                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            }
        }
    }
    protected abstract string Failure();
    protected abstract ValueTask Run(CancellationToken token);
}
