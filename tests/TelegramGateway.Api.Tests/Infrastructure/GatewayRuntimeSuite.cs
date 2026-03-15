using System.Net;
using System.Text.Json;
using Finance.Application.Contracts.Entry;
using Finance.Application.Contracts.Messaging;
using RabbitMQ.Client;
using Testcontainers.RabbitMq;
using TelegramGateway.Application.Keys;

namespace TelegramGateway.Api.Tests.Infrastructure;

/// <summary>
/// Provides RabbitMQ-backed runtime support for Telegram gateway delivery tests.
/// </summary>
public abstract class GatewayRuntimeSuite : IAsyncLifetime
{
    private static readonly JsonSerializerOptions json = new(JsonSerializerDefaults.Web);
    private readonly RabbitMqContainer box = new RabbitMqBuilder("rabbitmq:management").WithUsername("finance").WithPassword("finance").Build();
    private string rabbit = string.Empty;
    /// <summary>
    /// Starts the RabbitMQ container.
    /// </summary>
    /// <returns>A task that completes when startup finishes.</returns>
    public async Task InitializeAsync()
    {
        await box.StartAsync();
        rabbit = box.GetConnectionString();
    }
    /// <summary>
    /// Stops the RabbitMQ container.
    /// </summary>
    /// <returns>A task that completes when disposal finishes.</returns>
    public Task DisposeAsync() => box.DisposeAsync().AsTask();
    /// <summary>
    /// Waits until the readiness endpoint becomes healthy.
    /// </summary>
    /// <param name="client">The HTTP client.</param>
    /// <returns>A task that completes when the host is ready.</returns>
    protected static async Task Ready(HttpClient client)
    {
        using var note = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        while (!note.IsCancellationRequested)
        {
            try
            {
                HttpResponseMessage data = await client.GetAsync("/health/ready", note.Token);
                if (data.StatusCode == HttpStatusCode.OK)
                {
                    return;
                }
                await Task.Delay(250, note.Token);
            }
            catch (OperationCanceledException) when (note.IsCancellationRequested)
            {
                break;
            }
        }
        throw new TimeoutException("Timed out waiting for /health/ready");
    }
    /// <summary>
    /// Builds configuration overrides for the gateway host.
    /// </summary>
    /// <param name="name">The RabbitMQ client name.</param>
    /// <returns>The configuration overrides.</returns>
    protected Dictionary<string, string?> Settings(string name)
    {
        var item = new Uri(rabbit);
        string[] data = item.UserInfo.Split(':', 2, StringSplitOptions.None);
        return new Dictionary<string, string?>
        {
            ["Telegram:Webhook:SecretToken"] = "test-secret",
            ["Telegram:Bot:Token"] = "test-bot-token",
            ["Telegram:Bot:BaseUrl"] = "https://api.telegram.org",
            ["Telegram:Bot:TimeoutSeconds"] = "10",
            ["Telegram:Keys:CurrentSecret"] = "test-current-secret",
            ["RabbitMq:Host"] = item.Host,
            ["RabbitMq:Port"] = item.Port.ToString(),
            ["RabbitMq:VirtualHost"] = Uri.UnescapeDataString(item.AbsolutePath),
            ["RabbitMq:Username"] = data.Length > 0 ? Uri.UnescapeDataString(data[0]) : string.Empty,
            ["RabbitMq:Password"] = data.Length > 1 ? Uri.UnescapeDataString(data[1]) : string.Empty,
            ["RabbitMq:CommandExchange"] = "finance.command",
            ["RabbitMq:DeliveryExchange"] = "finance.delivery",
            ["RabbitMq:DeliveryQueue"] = "telegram-gateway.delivery",
            ["RabbitMq:DeliveryRetryQueue"] = "telegram-gateway.delivery.retry",
            ["RabbitMq:DeliveryDeadQueue"] = "telegram-gateway.delivery.dead",
            ["RabbitMq:Client"] = name,
            ["RabbitMq:DeliveryPrefetch"] = "16",
            ["RabbitMq:DeliveryRetryDelaySeconds"] = "1",
            ["RabbitMq:DeliveryMaxAttempts"] = "2"
        };
    }
    /// <summary>
    /// Publishes a raw delivery message.
    /// </summary>
    /// <param name="contract">The contract name.</param>
    /// <param name="routingKey">The routing key.</param>
    /// <param name="body">The message body.</param>
    /// <returns>A task that completes when publish finishes.</returns>
    protected async Task Publish(string contract, string routingKey, byte[] body)
    {
        var item = new ConnectionFactory { Uri = new Uri(rabbit) };
        await using IConnection link = await item.CreateConnectionAsync();
        await using IChannel lane = await link.CreateChannelAsync(cancellationToken: default);
        var data = new BasicProperties
        {
            ContentType = "application/json",
            DeliveryMode = DeliveryModes.Persistent,
            MessageId = Guid.CreateVersion7().ToString(),
            CorrelationId = $"trace-{Guid.CreateVersion7():N}",
            Type = contract,
            Headers = new Dictionary<string, object?>
            {
                ["contract"] = contract
            }
        };
        await lane.BasicPublishAsync("finance.delivery", routingKey, true, data, body, default);
    }
    /// <summary>
    /// Publishes a workspace view envelope.
    /// </summary>
    /// <param name="note">The envelope.</param>
    /// <returns>A task that completes when publish finishes.</returns>
    protected Task Publish(MessageEnvelope<WorkspaceViewRequestedCommand> note) => Publish(note.Contract, note.Contract, JsonSerializer.SerializeToUtf8Bytes(note, json));
    /// <summary>
    /// Reads one message from the given queue.
    /// </summary>
    /// <param name="name">The queue name.</param>
    /// <returns>The queued message when it is available.</returns>
    protected async Task<BasicGetResult> Queue(string name)
    {
        var item = new ConnectionFactory { Uri = new Uri(rabbit) };
        await using IConnection link = await item.CreateConnectionAsync();
        await using IChannel lane = await link.CreateChannelAsync(cancellationToken: default);
        using var note = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        while (!note.IsCancellationRequested)
        {
            BasicGetResult? data;
            try
            {
                data = await lane.BasicGetAsync(name, true, note.Token);
            }
            catch (OperationCanceledException) when (note.IsCancellationRequested)
            {
                throw new TimeoutException($"Timed out waiting for message on queue '{name}'");
            }
            if (data is not null)
            {
                return data;
            }
            try
            {
                await Task.Delay(100, note.Token);
            }
            catch (OperationCanceledException) when (note.IsCancellationRequested)
            {
                throw new TimeoutException($"Timed out waiting for message on queue '{name}'");
            }
        }
        throw new TimeoutException($"Timed out waiting for message on queue '{name}'");
    }
    /// <summary>
    /// Checks whether the queue is currently empty.
    /// </summary>
    /// <param name="name">The queue name.</param>
    /// <returns><see langword="true"/> when the queue has no message; otherwise <see langword="false"/>.</returns>
    protected async Task<bool> Missing(string name)
    {
        var item = new ConnectionFactory { Uri = new Uri(rabbit) };
        await using IConnection link = await item.CreateConnectionAsync();
        await using IChannel lane = await link.CreateChannelAsync(cancellationToken: default);
        BasicGetResult? data = await lane.BasicGetAsync(name, true, default);
        return data is null;
    }
    /// <summary>
    /// Creates a workspace view envelope for delivery tests.
    /// </summary>
    /// <param name="chatId">The Telegram chat identifier.</param>
    /// <param name="state">The workspace state.</param>
    /// <param name="actions">The action codes.</param>
    /// <returns>The workspace view envelope.</returns>
    protected static MessageEnvelope<WorkspaceViewRequestedCommand> Envelope(long chatId, string state = "home", IReadOnlyList<string>? actions = null)
    {
        var key = new OpaqueKey("test-current-secret", []);
        return new MessageEnvelope<WorkspaceViewRequestedCommand>(
            Guid.CreateVersion7(),
            "workspace.view.requested",
            DateTimeOffset.UtcNow,
            new MessageContext($"trace-{Guid.CreateVersion7():N}", $"cause-{Guid.CreateVersion7():N}", $"view-{Guid.CreateVersion7():N}"),
            "finance-core",
            new WorkspaceViewRequestedCommand(
                new WorkspaceIdentity(key.Text("actor", "telegram:user", 42), key.Text("conversation", "telegram:chat", chatId)),
                new WorkspaceProfile("Alex", "en"),
                state,
                actions ?? ["transaction.expense.add", "summary.month.show"],
                false,
                false,
                DateTimeOffset.UtcNow));
    }
}
