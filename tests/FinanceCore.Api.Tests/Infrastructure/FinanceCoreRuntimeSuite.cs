using System.Net;
using System.Text.Json;
using Finance.Application.Contracts.Entry;
using Finance.Application.Contracts.Messaging;
using Npgsql;
using RabbitMQ.Client;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;

namespace FinanceCore.Api.Tests.Infrastructure;

/// <summary>
/// Provides shared external dependency setup for finance core integration tests.
/// </summary>
public abstract class FinanceCoreRuntimeSuite : IAsyncLifetime
{
    private static readonly JsonSerializerOptions json = new(JsonSerializerDefaults.Web);
    private readonly PostgreSqlContainer sql = new PostgreSqlBuilder("postgres:17").WithUsername("finance").WithPassword("finance").WithDatabase("finance_core").Build();
    private readonly RabbitMqContainer box = new RabbitMqBuilder("rabbitmq:management").WithUsername("finance").WithPassword("finance").Build();
    private string rabbit = string.Empty;
    private string postgres = string.Empty;
    /// <summary>
    /// Starts the external dependencies.
    /// </summary>
    /// <returns>A task that completes when startup finishes.</returns>
    public async Task InitializeAsync()
    {
        await sql.StartAsync();
        await box.StartAsync();
        rabbit = box.GetConnectionString();
        postgres = sql.GetConnectionString();
    }
    /// <summary>
    /// Stops the external dependencies.
    /// </summary>
    /// <returns>A task that completes when disposal finishes.</returns>
    public async Task DisposeAsync()
    {
        await box.DisposeAsync();
        await sql.DisposeAsync();
    }
    /// <summary>
    /// Waits until the readiness endpoint becomes healthy.
    /// </summary>
    /// <param name="client">The HTTP client.</param>
    /// <returns>A task that completes when readiness is confirmed.</returns>
    protected static async Task Ready(HttpClient client)
    {
        using var note = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        while (!note.IsCancellationRequested)
        {
            try
            {
                HttpResponseMessage response = await client.GetAsync("/health/ready", note.Token);
                if (response.StatusCode == HttpStatusCode.OK)
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
    /// Clears the finance core tables.
    /// </summary>
    /// <returns>A task that completes when the reset finishes.</returns>
    protected Task Reset() => Execute("truncate table finance.outbox_message, finance.inbox_message, finance.workspace, finance.user_account restart identity cascade");
    /// <summary>
    /// Executes a SQL command.
    /// </summary>
    /// <param name="text">The SQL text.</param>
    /// <returns>A task that completes when execution finishes.</returns>
    protected async Task Execute(string text)
    {
        await using NpgsqlConnection link = new(postgres);
        await link.OpenAsync();
        await using NpgsqlCommand note = new(text, link);
        _ = await note.ExecuteNonQueryAsync();
    }
    /// <summary>
    /// Executes a scalar query and parses the result as a number.
    /// </summary>
    /// <param name="text">The SQL text.</param>
    /// <returns>The scalar number result.</returns>
    protected async Task<long> Number(string text)
    {
        string data = await Scalar(text);
        return long.Parse(data, System.Globalization.CultureInfo.InvariantCulture);
    }
    /// <summary>
    /// Executes a scalar query and returns its string value.
    /// </summary>
    /// <param name="text">The SQL text.</param>
    /// <returns>The scalar value.</returns>
    protected async Task<string> Scalar(string text)
    {
        await using NpgsqlConnection link = new(postgres);
        await link.OpenAsync();
        await using NpgsqlCommand note = new(text, link);
        return (await note.ExecuteScalarAsync())?.ToString() ?? string.Empty;
    }
    /// <summary>
    /// Declares and binds a transient view queue.
    /// </summary>
    /// <param name="queue">The queue name.</param>
    /// <param name="routingKey">The routing key.</param>
    /// <returns>A task that completes when the binding finishes.</returns>
    protected async Task Bind(string queue, string routingKey)
    {
        var item = new ConnectionFactory { Uri = new Uri(rabbit) };
        await using IConnection link = await item.CreateConnectionAsync();
        await using IChannel lane = await link.CreateChannelAsync(cancellationToken: default);
        await lane.ExchangeDeclareAsync("finance.delivery", ExchangeType.Topic, true, false, null, false, false, default);
        _ = await lane.QueueDeclareAsync(queue, false, false, true, null, false, false, default);
        await lane.QueueBindAsync(queue, "finance.delivery", routingKey, null, false, default);
    }
    /// <summary>
    /// Publishes a workspace request envelope.
    /// </summary>
    /// <param name="note">The workspace request envelope.</param>
    /// <returns>A task that completes when publish finishes.</returns>
    protected Task Publish(MessageEnvelope<WorkspaceRequestedCommand> note)
    {
        byte[] data = JsonSerializer.SerializeToUtf8Bytes(note, json);
        return Publish("workspace.requested", note.Contract, data);
    }
    /// <summary>
    /// Publishes a raw message to RabbitMQ.
    /// </summary>
    /// <param name="routingKey">The routing key.</param>
    /// <param name="contract">The contract name.</param>
    /// <param name="body">The message body.</param>
    /// <returns>A task that completes when publish finishes.</returns>
    protected async Task Publish(string routingKey, string contract, byte[] body)
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
        await lane.BasicPublishAsync("finance.command", routingKey, true, data, body, default);
    }
    /// <summary>
    /// Reads a published workspace view from the queue.
    /// </summary>
    /// <param name="queue">The queue name.</param>
    /// <param name="span">The optional wait timeout.</param>
    /// <returns>The view envelope when it is available.</returns>
    protected async Task<MessageEnvelope<WorkspaceViewRequestedCommand>?> View(string queue, TimeSpan? span = null)
    {
        var item = new ConnectionFactory { Uri = new Uri(rabbit) };
        await using IConnection link = await item.CreateConnectionAsync();
        await using IChannel lane = await link.CreateChannelAsync(cancellationToken: default);
        using var note = new CancellationTokenSource(span ?? TimeSpan.FromSeconds(10));
        while (!note.IsCancellationRequested)
        {
            BasicGetResult? data;
            try
            {
                data = await lane.BasicGetAsync(queue, true, note.Token);
            }
            catch (OperationCanceledException) when (note.IsCancellationRequested)
            {
                break;
            }
            if (data is not null)
            {
                return JsonSerializer.Deserialize<MessageEnvelope<WorkspaceViewRequestedCommand>>(data.Body.Span, json);
            }
            try
            {
                await Task.Delay(100, note.Token);
            }
            catch (OperationCanceledException) when (note.IsCancellationRequested)
            {
                break;
            }
        }
        return null;
    }
    /// <summary>
    /// Reads one message from the dead queue.
    /// </summary>
    /// <returns>The dead-lettered message when it is available.</returns>
    protected async Task<BasicGetResult?> Dead()
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
                data = await lane.BasicGetAsync("finance-core.command.dead", true, note.Token);
            }
            catch (OperationCanceledException) when (note.IsCancellationRequested)
            {
                break;
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
                break;
            }
        }
        return null;
    }
    /// <summary>
    /// Creates a workspace request envelope for tests.
    /// </summary>
    /// <param name="actor">The actor key.</param>
    /// <param name="conversation">The conversation key.</param>
    /// <param name="payload">The raw payload.</param>
    /// <param name="idempotencyKey">The idempotency key.</param>
    /// <returns>The workspace request envelope.</returns>
    protected static MessageEnvelope<WorkspaceRequestedCommand> Envelope(
        string actor,
        string conversation,
        string payload,
        string idempotencyKey) => new(
        Guid.CreateVersion7(),
        "workspace.requested",
        DateTimeOffset.UtcNow,
        new MessageContext(
            $"trace-{Guid.CreateVersion7():N}",
            $"cause-{Guid.CreateVersion7():N}",
            idempotencyKey),
        "telegram-gateway",
        new WorkspaceRequestedCommand(
            new WorkspaceIdentity(actor, conversation),
            new WorkspaceProfile("Alex", "en"),
            payload,
            DateTimeOffset.UtcNow));
    /// <summary>
    /// Builds configuration overrides for the finance core host.
    /// </summary>
    /// <param name="name">The RabbitMQ client name.</param>
    /// <returns>The configuration overrides.</returns>
    protected Dictionary<string, string?> Settings(string name)
    {
        var item = new Uri(rabbit);
        string[] data = item.UserInfo.Split(':', 2, StringSplitOptions.None);
        return new Dictionary<string, string?>
        {
            ["Postgres:ConnectionString"] = postgres,
            ["RabbitMq:Host"] = item.Host,
            ["RabbitMq:Port"] = item.Port.ToString(),
            ["RabbitMq:VirtualHost"] = Uri.UnescapeDataString(item.AbsolutePath),
            ["RabbitMq:Username"] = data.Length > 0 ? Uri.UnescapeDataString(data[0]) : string.Empty,
            ["RabbitMq:Password"] = data.Length > 1 ? Uri.UnescapeDataString(data[1]) : string.Empty,
            ["RabbitMq:CommandExchange"] = "finance.command",
            ["RabbitMq:DeliveryExchange"] = "finance.delivery",
            ["RabbitMq:Queue"] = "finance-core.command",
            ["RabbitMq:RetryQueue"] = "finance-core.command.retry",
            ["RabbitMq:DeadQueue"] = "finance-core.command.dead",
            ["RabbitMq:Client"] = name,
            ["RabbitMq:Prefetch"] = "16",
            ["RabbitMq:RetryDelaySeconds"] = "1",
            ["RabbitMq:MaxAttempts"] = "5"
        };
    }
    /// <summary>
    /// Returns the PostgreSQL connection string.
    /// </summary>
    /// <returns>The PostgreSQL connection string.</returns>
    protected string Postgres() => postgres;
}
