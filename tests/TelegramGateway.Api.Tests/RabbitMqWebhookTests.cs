using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Finance.Application.Contracts.Entry;
using Finance.Application.Contracts.Messaging;
using RabbitMQ.Client;
using TelegramGateway.Api.Tests.Infrastructure;

namespace TelegramGateway.Api.Tests;

/// <summary>
/// Covers webhook behavior with a real RabbitMQ broker.
/// Example:
/// <code>
/// var test = new RabbitMqWebhookTests();
/// </code>
/// </summary>
public sealed class RabbitMqWebhookTests : IAsyncLifetime
{
    private readonly IContainer box = new ContainerBuilder("rabbitmq:4.1-management").WithPortBinding(5672, true).WithWaitStrategy(Wait.ForUnixContainer().UntilExternalTcpPortIsAvailable(5672)).Build();
    private string uri = string.Empty;
    /// <summary>
    /// Starts the RabbitMQ container and waits for broker connectivity.
    /// Example:
    /// <code>
    /// await test.InitializeAsync();
    /// </code>
    /// </summary>
    /// <returns>A task that completes when the broker is ready.</returns>
    public async Task InitializeAsync()
    {
        await box.StartAsync();
        uri = $"amqp://guest:guest@{box.Hostname}:{box.GetMappedPublicPort(5672)}/";
        using var note = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        while (!note.IsCancellationRequested)
        {
            try
            {
                var item = new ConnectionFactory { Uri = new Uri(uri) };
                await using var link = await item.CreateConnectionAsync(note.Token);
                await using var lane = await link.CreateChannelAsync(cancellationToken: note.Token);
                await lane.CloseAsync(note.Token);
                return;
            }
            catch
            {
                await Task.Delay(250, note.Token);
            }
        }
        throw new InvalidOperationException("RabbitMQ broker was not ready");
    }
    /// <summary>
    /// Stops the RabbitMQ container.
    /// Example:
    /// <code>
    /// await test.DisposeAsync();
    /// </code>
    /// </summary>
    /// <returns>A task that completes when the container is stopped.</returns>
    public Task DisposeAsync()
    {
        return box.DisposeAsync().AsTask();
    }
    /// <summary>
    /// Verifies end-to-end publish with a bound queue.
    /// Example:
    /// <code>
    /// await test.Accepts_publish();
    /// </code>
    /// </summary>
    [Fact(DisplayName = "Publishes one workspace command to RabbitMQ when the queue is bound")]
    public async Task Accepts_publish()
    {
        var name = $"workspace-{Guid.CreateVersion7():N}";
        await Bind(name);
        await using var host = new GatewayApiFactory(Note("telegram-gateway-rabbit"));
        using var item = Client(host);
        var note = await item.PostAsJsonAsync("/telegram/webhook", Body("/start promo-42"));
        var data = await Message(name);
        Assert.Equal(HttpStatusCode.OK, note.StatusCode);
        Assert.NotNull(data);
        Assert.Equal("promo-42", data!.Payload.Payload);
    }
    /// <summary>
    /// Verifies that an unroutable publish becomes a service unavailable response.
    /// Example:
    /// <code>
    /// await test.Rejects_publish();
    /// </code>
    /// </summary>
    [Fact(DisplayName = "Returns 503 when RabbitMQ cannot route the command")]
    public async Task Rejects_publish()
    {
        await using var host = new GatewayApiFactory(Note("telegram-gateway-rabbit"));
        using var item = Client(host);
        var note = await item.PostAsJsonAsync("/telegram/webhook", Body("/start"));
        Assert.Equal(HttpStatusCode.ServiceUnavailable, note.StatusCode);
    }
    /// <summary>
    /// Declares and binds a queue for the workspace routing key.
    /// Example:
    /// <code>
    /// await Bind("workspace");
    /// </code>
    /// </summary>
    /// <param name="name">The queue name.</param>
    /// <returns>A task that completes when the binding exists.</returns>
    private async Task Bind(string name)
    {
        var item = new ConnectionFactory { Uri = new Uri(uri) };
        await using IConnection link = await item.CreateConnectionAsync();
        await using IChannel lane = await link.CreateChannelAsync(cancellationToken: default);
        await lane.ExchangeDeclareAsync("finance.command", ExchangeType.Topic, true, false, null, false, false, default);
        _ = await lane.QueueDeclareAsync(name, false, false, true, null, false, false, default);
        await lane.QueueBindAsync(name, "finance.command", "workspace.requested", null, false, default);
    }
    /// <summary>
    /// Reads the published workspace command from the queue.
    /// Example:
    /// <code>
    /// MessageEnvelope&lt;WorkspaceRequestedCommand&gt;? item = await Message("workspace");
    /// </code>
    /// </summary>
    /// <param name="name">The queue name.</param>
    /// <returns>The published command envelope or null when none arrives in time.</returns>
    private async Task<MessageEnvelope<WorkspaceRequestedCommand>?> Message(string name)
    {
        var item = new ConnectionFactory { Uri = new Uri(uri) };
        await using var link = await item.CreateConnectionAsync();
        await using var lane = await link.CreateChannelAsync(cancellationToken: default);
        using var note = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        while (!note.IsCancellationRequested)
        {
            var data = await lane.BasicGetAsync(name, true, note.Token);
            if (data is not null)
            {
                return JsonSerializer.Deserialize<MessageEnvelope<WorkspaceRequestedCommand>>(data.Body.Span, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            }
            await Task.Delay(100, note.Token);
        }
        return null;
    }
    /// <summary>
    /// Creates the configured API client.
    /// Example:
    /// <code>
    /// using HttpClient item = Client(host);
    /// </code>
    /// </summary>
    /// <param name="item">The web host factory.</param>
    /// <returns>The configured client.</returns>
    private static HttpClient Client(GatewayApiFactory item)
    {
        var note = item.CreateClient();
        note.DefaultRequestHeaders.Add("X-Telegram-Bot-Api-Secret-Token", "test-secret");
        return note;
    }
    /// <summary>
    /// Creates the host configuration overrides.
    /// Example:
    /// <code>
    /// IDictionary&lt;string, string?&gt; items = Note("bot");
    /// </code>
    /// </summary>
    /// <param name="name">The RabbitMQ client name.</param>
    /// <returns>The configuration collection.</returns>
    private Dictionary<string, string?> Note(string name)
    {
        var item = new Uri(uri);
        return new Dictionary<string, string?>
        {
            ["Telegram:Webhook:SecretToken"] = "test-secret",
            ["RabbitMq:Host"] = item.Host,
            ["RabbitMq:Port"] = item.Port.ToString(),
            ["RabbitMq:VirtualHost"] = Uri.UnescapeDataString(item.AbsolutePath),
            ["RabbitMq:Username"] = "guest",
            ["RabbitMq:Password"] = "guest",
            ["RabbitMq:Exchange"] = "finance.command",
            ["RabbitMq:Client"] = name
        };
    }
    /// <summary>
    /// Creates the webhook payload fixture.
    /// Example:
    /// <code>
    /// object item = Body("/start");
    /// </code>
    /// </summary>
    /// <param name="text">The message text.</param>
    /// <returns>The webhook payload.</returns>
    private static object Body(string text)
    {
        return new
        {
            update_id = 7,
            message = new
            {
                message_id = 8,
                date = 1_736_000_000,
                text,
                entities = new[]
                {
                    new
                    {
                        type = "bot_command",
                        offset = 0,
                        length = text.Contains(' ', StringComparison.Ordinal) ? text.IndexOf(' ', StringComparison.Ordinal) : text.Length
                    }
                },
                chat = new
                {
                    id = 100,
                    type = "private"
                },
                from = new
                {
                    id = 42,
                    first_name = "Alex",
                    last_name = "Doe",
                    username = "alex",
                    language_code = "en"
                }
            }
        };
    }
}
