using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using FinanceCore.Infrastructure.Persistence.Postgres.Migrations;

namespace FinanceCore.Infrastructure.Messaging.RabbitMq;

internal sealed class RabbitMqBoot : IHostedService
{
    private readonly IBrokerState state;
    private readonly ILogger<RabbitMqBoot> log;
    internal RabbitMqBoot(IBrokerState state, ILogger<RabbitMqBoot> log)
    {
        this.state = state ?? throw new ArgumentNullException(nameof(state));
        this.log = log ?? throw new ArgumentNullException(nameof(log));
    }
    public Task StartAsync(CancellationToken cancellationToken) => StartupRetry.Run("RabbitMQ warm-up failed on attempt {Attempt} of {Count}", state.Ensure, log, cancellationToken);
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
