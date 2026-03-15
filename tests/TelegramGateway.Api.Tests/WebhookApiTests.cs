using System.Net;
using System.Net.Http.Json;
using System.Text;
using TelegramGateway.Api.Tests.Infrastructure;
using TelegramGateway.Application.Messaging;

namespace TelegramGateway.Api.Tests;

/// <summary>
/// Covers webhook API behavior with fakes.
/// </summary>
public sealed class WebhookApiTests
{
    /// <summary>
    /// Verifies that an invalid secret is rejected.
    /// </summary>
    /// <returns>A task that completes when the operation finishes.</returns>
    [Fact(DisplayName = "Returns 403 when the webhook secret is invalid")]
    public async Task Rejects_secret()
    {
        var port = new RecordingWorkspacePort();
        await using var host = new GatewayApiFactory(Note(), port, new ReadyBrokerState(), hosted: false);
        using HttpClient client = host.CreateClient();
        client.DefaultRequestHeaders.Add("X-Telegram-Bot-Api-Secret-Token", "wrong");
        HttpResponseMessage response = await client.PostAsJsonAsync("/telegram/webhook", Body("/start"));
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Empty(port.Items);
    }
    /// <summary>
    /// Verifies that an invalid secret is rejected before request body binding.
    /// </summary>
    /// <returns>A task that completes when the operation finishes.</returns>
    [Fact(DisplayName = "Returns 403 before binding the webhook body when the secret is invalid")]
    public async Task Rejects_invalid_payload()
    {
        var port = new RecordingWorkspacePort();
        await using var host = new GatewayApiFactory(Note(), port, new ReadyBrokerState(), hosted: false);
        using HttpClient client = host.CreateClient();
        client.DefaultRequestHeaders.Add("X-Telegram-Bot-Api-Secret-Token", "wrong");
        using var payload = new StringContent("{", Encoding.UTF8, "application/json");
        HttpResponseMessage response = await client.PostAsync("/telegram/webhook", payload);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Empty(port.Items);
    }
    /// <summary>
    /// Verifies that unsupported updates are ignored.
    /// </summary>
    /// <returns>A task that completes when the operation finishes.</returns>
    [Fact(DisplayName = "Returns 200 and does not publish when the update is unsupported")]
    public async Task Ignores_update()
    {
        var port = new RecordingWorkspacePort();
        await using var host = new GatewayApiFactory(Note(), port, new ReadyBrokerState(), hosted: false);
        using HttpClient client = Client(host);
        HttpResponseMessage response = await client.PostAsJsonAsync("/telegram/webhook", Body("/help"));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Empty(port.Items);
    }
    /// <summary>
    /// Verifies that out-of-range Telegram timestamps are ignored.
    /// </summary>
    /// <returns>A task that completes when the operation finishes.</returns>
    [Fact(DisplayName = "Returns 200 and does not publish when the Telegram timestamp is out of range")]
    public async Task Ignores_timestamp()
    {
        var port = new RecordingWorkspacePort();
        await using var host = new GatewayApiFactory(Note(), port, new ReadyBrokerState(), hosted: false);
        using HttpClient client = Client(host);
        HttpResponseMessage response = await client.PostAsJsonAsync("/telegram/webhook", Body("/start", DateTimeOffset.MaxValue.ToUnixTimeSeconds() + 1));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Empty(port.Items);
    }
    /// <summary>
    /// Verifies that a start command is published.
    /// </summary>
    /// <returns>A task that completes when the operation finishes.</returns>
    [Fact(DisplayName = "Returns 200 and publishes one workspace command for /start")]
    public async Task Accepts_start()
    {
        var port = new RecordingWorkspacePort();
        await using var host = new GatewayApiFactory(Note(), port, new ReadyBrokerState(), hosted: false);
        using HttpClient client = Client(host);
        HttpResponseMessage response = await client.PostAsJsonAsync("/telegram/webhook", Body("/start promo-42"));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Single(port.Items);
        Assert.Equal("promo-42", port.Items.Single().Payload.Payload);
        Assert.Equal("edge-update-7", port.Items.Single().Context.CausationId);
        Assert.Equal("workspace-requested-7", port.Items.Single().Context.IdempotencyKey);
        Assert.NotEqual(port.Items.Single().Context.CausationId, port.Items.Single().Context.IdempotencyKey);
    }
    /// <summary>
    /// Verifies that publish faults become service unavailable responses.
    /// </summary>
    /// <returns>A task that completes when the operation finishes.</returns>
    [Fact(DisplayName = "Returns 503 when publish fails")]
    public async Task Rejects_publish()
    {
        var port = new RecordingWorkspacePort(new BusException("Message publish failed"));
        await using var host = new GatewayApiFactory(Note(), port, new ReadyBrokerState(), hosted: false);
        using HttpClient client = Client(host);
        HttpResponseMessage response = await client.PostAsJsonAsync("/telegram/webhook", Body("/start"));
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }
    /// <summary>
    /// Verifies that the readiness endpoint is healthy with the fake broker.
    /// </summary>
    /// <returns>A task that completes when the operation finishes.</returns>
    [Fact(DisplayName = "Returns 200 for the readiness endpoint when the broker state is ready")]
    public async Task Ready()
    {
        await using var host = new GatewayApiFactory(Note(), new RecordingWorkspacePort(), new ReadyBrokerState(), hosted: false);
        using HttpClient client = host.CreateClient();
        HttpResponseMessage response = await client.GetAsync("/health/ready");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
    private static HttpClient Client(GatewayApiFactory host)
    {
        HttpClient client = host.CreateClient();
        client.DefaultRequestHeaders.Add("X-Telegram-Bot-Api-Secret-Token", "test-secret");
        return client;
    }
    private static Dictionary<string, string?> Note() => new Dictionary<string, string?>
    {
        ["Telegram:Webhook:SecretToken"] = "test-secret",
        ["Telegram:Bot:Token"] = "test-bot-token",
        ["Telegram:Bot:BaseUrl"] = "https://api.telegram.org",
        ["Telegram:Bot:TimeoutSeconds"] = "10",
        ["RabbitMq:Host"] = "localhost",
        ["RabbitMq:Port"] = "5672",
        ["RabbitMq:VirtualHost"] = "/",
        ["RabbitMq:Username"] = "guest",
        ["RabbitMq:Password"] = "guest",
        ["RabbitMq:CommandExchange"] = "finance.command",
        ["RabbitMq:DeliveryExchange"] = "finance.delivery",
        ["RabbitMq:DeliveryQueue"] = "telegram-gateway.delivery",
        ["RabbitMq:DeliveryRetryQueue"] = "telegram-gateway.delivery.retry",
        ["RabbitMq:DeliveryDeadQueue"] = "telegram-gateway.delivery.dead",
        ["RabbitMq:Client"] = "telegram-gateway-tests",
        ["RabbitMq:DeliveryPrefetch"] = "16",
        ["RabbitMq:DeliveryRetryDelaySeconds"] = "1",
        ["RabbitMq:DeliveryMaxAttempts"] = "5"
    };
    private static object Body(string text, long date = 1_736_000_000) => new
    {
        update_id = 7,
        message = new
        {
            message_id = 8,
            date,
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
