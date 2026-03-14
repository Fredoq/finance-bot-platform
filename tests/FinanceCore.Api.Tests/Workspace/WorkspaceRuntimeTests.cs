using System.Net;
using System.Text;
using System.Text.Json;
using Finance.Application.Contracts.Entry;
using Finance.Application.Contracts.Messaging;
using FinanceCore.Api.Tests.Infrastructure;
using Npgsql;
using RabbitMQ.Client;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;

namespace FinanceCore.Api.Tests;

/// <summary>
/// Covers finance core behavior with real PostgreSQL and RabbitMQ dependencies.
/// </summary>
public sealed class WorkspaceRuntimeTests : IAsyncLifetime
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
    /// Verifies that the first workspace request creates state and publishes a view.
    /// </summary>
    /// <returns>A task that completes when the operation finishes.</returns>
    [Fact(DisplayName = "Creates workspace state and publishes one view for the first request")]
    public async Task Creates_workspace()
    {
        string queue = $"view-{Guid.CreateVersion7():N}";
        await using var host = new CoreApiFactory(Note("finance-core-create"));
        using HttpClient client = host.CreateClient();
        await Ready(client);
        await Reset();
        await Bind(queue, "workspace.view.requested");
        await Publish(Note("actor-1", "room-1", "promo-42", "workspace-requested-1"));
        MessageEnvelope<WorkspaceViewRequestedCommand>? view = await View(queue);
        Assert.NotNull(view);
        Assert.True(view!.Payload.IsNewUser);
        Assert.True(view.Payload.IsNewWorkspace);
        Assert.Equal("home", view.Payload.State);
        Assert.Equal("promo-42", await Scalar("select last_payload from finance.workspace where conversation_key = 'room-1'"));
        Assert.Equal(1, await Number("select count(*) from finance.user_account"));
        Assert.Equal(1, await Number("select count(*) from finance.workspace"));
        Assert.Equal(1, await Number("select count(*) from finance.inbox_message"));
        Assert.Equal(1, await Number("select count(*) from finance.outbox_message where published_utc is not null"));
    }
    /// <summary>
    /// Verifies that duplicate idempotency keys do not create duplicates.
    /// </summary>
    /// <returns>A task that completes when the operation finishes.</returns>
    [Fact(DisplayName = "Processes the same idempotency key once")]
    public async Task Deduplicates_workspace()
    {
        string queue = $"view-{Guid.CreateVersion7():N}";
        await using var host = new CoreApiFactory(Note("finance-core-dedupe"));
        using HttpClient client = host.CreateClient();
        await Ready(client);
        await Reset();
        await Bind(queue, "workspace.view.requested");
        await Publish(Note("actor-2", "room-2", "promo-21", "workspace-requested-2"));
        await Publish(Note("actor-2", "room-2", "promo-21", "workspace-requested-2"));
        MessageEnvelope<WorkspaceViewRequestedCommand>? view = await View(queue);
        Assert.NotNull(view);
        Assert.Equal(1, await Number("select count(*) from finance.user_account"));
        Assert.Equal(1, await Number("select count(*) from finance.workspace"));
        Assert.Equal(1, await Number("select count(*) from finance.inbox_message"));
        Assert.Equal(1, await Number("select count(*) from finance.outbox_message"));
        Assert.Null(await View(queue, TimeSpan.FromSeconds(1)));
    }
    /// <summary>
    /// Verifies that a new conversation creates a new workspace for an existing actor.
    /// </summary>
    /// <returns>A task that completes when the operation finishes.</returns>
    [Fact(DisplayName = "Creates a new workspace for an existing actor in a new conversation")]
    public async Task Creates_conversation()
    {
        string queue = $"view-{Guid.CreateVersion7():N}";
        await using var host = new CoreApiFactory(Note("finance-core-conversation"));
        using HttpClient client = host.CreateClient();
        await Ready(client);
        await Reset();
        await Bind(queue, "workspace.view.requested");
        await Publish(Note("actor-3", "room-3", string.Empty, "workspace-requested-3"));
        _ = await View(queue);
        await Publish(Note("actor-3", "room-4", string.Empty, "workspace-requested-4"));
        MessageEnvelope<WorkspaceViewRequestedCommand>? view = await View(queue);
        Assert.NotNull(view);
        Assert.False(view!.Payload.IsNewUser);
        Assert.True(view.Payload.IsNewWorkspace);
        Assert.Equal(1, await Number("select count(*) from finance.user_account"));
        Assert.Equal(2, await Number("select count(*) from finance.workspace"));
    }
    /// <summary>
    /// Verifies that an existing workspace keeps its current state on repeated requests.
    /// </summary>
    /// <returns>A task that completes when the operation finishes.</returns>
    [Fact(DisplayName = "Preserves the current state for an existing workspace")]
    public async Task Preserves_state()
    {
        string queue = $"view-{Guid.CreateVersion7():N}";
        await using var host = new CoreApiFactory(Note("finance-core-state"));
        using HttpClient client = host.CreateClient();
        await Ready(client);
        await Reset();
        await Bind(queue, "workspace.view.requested");
        await Publish(Note("actor-4", "room-5", "first", "workspace-requested-5"));
        _ = await View(queue);
        await Execute("update finance.workspace set state_code = 'expense-draft', state_data = '{\"step\":\"amount\"}'::jsonb where conversation_key = 'room-5'");
        await Publish(Note("actor-4", "room-5", "second", "workspace-requested-6"));
        MessageEnvelope<WorkspaceViewRequestedCommand>? view = await View(queue);
        Assert.NotNull(view);
        Assert.Equal("expense-draft", view!.Payload.State);
        Assert.Equal("second", await Scalar("select last_payload from finance.workspace where conversation_key = 'room-5'"));
        Assert.Equal(2L, long.Parse(await Scalar("select revision::text from finance.workspace where conversation_key = 'room-5'"), System.Globalization.CultureInfo.InvariantCulture));
    }
    /// <summary>
    /// Verifies that unknown contracts are moved to the dead queue.
    /// </summary>
    /// <returns>A task that completes when the operation finishes.</returns>
    [Fact(DisplayName = "Moves unsupported contracts to the dead queue")]
    public async Task Rejects_contract()
    {
        await using var host = new CoreApiFactory(Note("finance-core-unknown"));
        using HttpClient client = host.CreateClient();
        await Ready(client);
        await Reset();
        await Publish("workspace.requested", "budget.unknown", Encoding.UTF8.GetBytes("{\"contract\":\"budget.unknown\"}"));
        BasicGetResult? data = await Dead();
        Assert.NotNull(data);
        Assert.Equal(0, await Number("select count(*) from finance.user_account"));
        Assert.Equal(0, await Number("select count(*) from finance.workspace"));
    }
    /// <summary>
    /// Verifies that malformed payloads are moved to the dead queue.
    /// </summary>
    /// <returns>A task that completes when the operation finishes.</returns>
    [Fact(DisplayName = "Moves malformed payloads to the dead queue")]
    public async Task Rejects_payload()
    {
        await using var host = new CoreApiFactory(Note("finance-core-payload"));
        using HttpClient client = host.CreateClient();
        await Ready(client);
        await Reset();
        await Publish("workspace.requested", "workspace.requested", Encoding.UTF8.GetBytes("{"));
        BasicGetResult? data = await Dead();
        Assert.NotNull(data);
        Assert.Equal(0, await Number("select count(*) from finance.user_account"));
    }
    private static async Task Ready(HttpClient client)
    {
        using var note = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        while (!note.IsCancellationRequested)
        {
            HttpResponseMessage response = await client.GetAsync("/health/ready", note.Token);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                return;
            }
            await Task.Delay(250, note.Token);
        }
    }
    private Task Reset() => Execute("truncate table finance.outbox_message, finance.inbox_message, finance.workspace, finance.user_account restart identity cascade");
    private async Task Execute(string text)
    {
        await using NpgsqlConnection link = new(postgres);
        await link.OpenAsync();
        await using NpgsqlCommand note = new(text, link);
        _ = await note.ExecuteNonQueryAsync();
    }
    private async Task<long> Number(string text)
    {
        string data = await Scalar(text);
        return long.Parse(data, System.Globalization.CultureInfo.InvariantCulture);
    }
    private async Task<string> Scalar(string text)
    {
        await using NpgsqlConnection link = new(postgres);
        await link.OpenAsync();
        await using NpgsqlCommand note = new(text, link);
        return (await note.ExecuteScalarAsync())?.ToString() ?? string.Empty;
    }
    private async Task Bind(string queue, string routingKey)
    {
        var item = new ConnectionFactory { Uri = new Uri(rabbit) };
        await using IConnection link = await item.CreateConnectionAsync();
        await using IChannel lane = await link.CreateChannelAsync(cancellationToken: default);
        await lane.ExchangeDeclareAsync("finance.command", ExchangeType.Topic, true, false, null, false, false, default);
        _ = await lane.QueueDeclareAsync(queue, false, false, true, null, false, false, default);
        await lane.QueueBindAsync(queue, "finance.command", routingKey, null, false, default);
    }
    private Task Publish(MessageEnvelope<WorkspaceRequestedCommand> note)
    {
        byte[] data = JsonSerializer.SerializeToUtf8Bytes(note, json);
        return Publish("workspace.requested", note.Contract, data);
    }
    private async Task Publish(string routingKey, string contract, byte[] body)
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
    private async Task<MessageEnvelope<WorkspaceViewRequestedCommand>?> View(string queue, TimeSpan? span = null)
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
    private async Task<BasicGetResult?> Dead()
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
    private static MessageEnvelope<WorkspaceRequestedCommand> Note(string actor, string conversation, string payload, string idempotencyKey) => new(Guid.CreateVersion7(), "workspace.requested", DateTimeOffset.UtcNow, new MessageContext($"trace-{Guid.CreateVersion7():N}", $"cause-{Guid.CreateVersion7():N}", idempotencyKey), "telegram-gateway", new WorkspaceRequestedCommand(new WorkspaceIdentity(actor, conversation), new WorkspaceProfile("Alex", "en"), payload, DateTimeOffset.UtcNow));
    private Dictionary<string, string?> Note(string name)
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
            ["RabbitMq:Exchange"] = "finance.command",
            ["RabbitMq:Queue"] = "finance-core.command",
            ["RabbitMq:RetryQueue"] = "finance-core.command.retry",
            ["RabbitMq:DeadQueue"] = "finance-core.command.dead",
            ["RabbitMq:Client"] = name,
            ["RabbitMq:Prefetch"] = "16",
            ["RabbitMq:RetryDelaySeconds"] = "1",
            ["RabbitMq:MaxAttempts"] = "5"
        };
    }
}
