using System.Net;
using System.Net.Http.Json;
using TelegramGateway.Api.Tests.Infrastructure;
using TelegramGateway.Application.Messaging;

namespace TelegramGateway.Api.Tests;

/// <summary>
/// Covers webhook API behavior with fakes.
/// Example:
/// <code>
/// var test = new WebhookApiTests();
/// </code>
/// </summary>
public sealed class WebhookApiTests
{
    /// <summary>
    /// Verifies that an invalid secret is rejected.
    /// Example:
    /// <code>
    /// await test.Rejects_secret();
    /// </code>
    /// </summary>
    [Fact(DisplayName = "Returns 403 when the webhook secret is invalid")]
    public async Task Rejects_secret()
    {
        var port = new RecordingWorkspacePort();
        await using var host = new GatewayApiFactory(Note(), port, new ReadyBrokerState());
        using var item = host.CreateClient();
        item.DefaultRequestHeaders.Add("X-Telegram-Bot-Api-Secret-Token", "wrong");
        var note = await item.PostAsJsonAsync("/telegram/webhook", Body("/start"));
        Assert.Equal(HttpStatusCode.Forbidden, note.StatusCode);
        Assert.Empty(port.Items);
    }
    /// <summary>
    /// Verifies that unsupported updates are ignored.
    /// Example:
    /// <code>
    /// await test.Ignores_update();
    /// </code>
    /// </summary>
    [Fact(DisplayName = "Returns 200 and does not publish when the update is unsupported")]
    public async Task Ignores_update()
    {
        var port = new RecordingWorkspacePort();
        await using var host = new GatewayApiFactory(Note(), port, new ReadyBrokerState());
        using var item = Client(host);
        var note = await item.PostAsJsonAsync("/telegram/webhook", Body("/help"));
        Assert.Equal(HttpStatusCode.OK, note.StatusCode);
        Assert.Empty(port.Items);
    }
    /// <summary>
    /// Verifies that a start command is published.
    /// Example:
    /// <code>
    /// await test.Accepts_start();
    /// </code>
    /// </summary>
    [Fact(DisplayName = "Returns 200 and publishes one workspace command for /start")]
    public async Task Accepts_start()
    {
        var port = new RecordingWorkspacePort();
        await using var host = new GatewayApiFactory(Note(), port, new ReadyBrokerState());
        using var item = Client(host);
        var note = await item.PostAsJsonAsync("/telegram/webhook", Body("/start promo-42"));
        Assert.Equal(HttpStatusCode.OK, note.StatusCode);
        Assert.Single(port.Items);
        Assert.Equal("promo-42", port.Items.Single().Payload.Payload);
    }
    /// <summary>
    /// Verifies that publish faults become service unavailable responses.
    /// Example:
    /// <code>
    /// await test.Rejects_publish();
    /// </code>
    /// </summary>
    [Fact(DisplayName = "Returns 503 when publish fails")]
    public async Task Rejects_publish()
    {
        var port = new RecordingWorkspacePort(new BusException("Message publish failed"));
        await using var host = new GatewayApiFactory(Note(), port, new ReadyBrokerState());
        using var item = Client(host);
        var note = await item.PostAsJsonAsync("/telegram/webhook", Body("/start"));
        Assert.Equal(HttpStatusCode.ServiceUnavailable, note.StatusCode);
    }
    /// <summary>
    /// Verifies that the readiness endpoint is healthy with the fake broker.
    /// Example:
    /// <code>
    /// await test.Ready();
    /// </code>
    /// </summary>
    [Fact(DisplayName = "Returns 200 for the readiness endpoint when the broker state is ready")]
    public async Task Ready()
    {
        await using var host = new GatewayApiFactory(Note(), new RecordingWorkspacePort(), new ReadyBrokerState());
        using var item = host.CreateClient();
        var note = await item.GetAsync("/health/ready");
        Assert.Equal(HttpStatusCode.OK, note.StatusCode);
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
    /// IDictionary&lt;string, string?&gt; items = Note();
    /// </code>
    /// </summary>
    /// <returns>The configuration collection.</returns>
    private static Dictionary<string, string?> Note()
    {
        return new Dictionary<string, string?>
        {
            ["Telegram:Webhook:SecretToken"] = "test-secret",
            ["RabbitMq:Host"] = "localhost",
            ["RabbitMq:Port"] = "5672",
            ["RabbitMq:VirtualHost"] = "/",
            ["RabbitMq:Username"] = "guest",
            ["RabbitMq:Password"] = "guest",
            ["RabbitMq:Exchange"] = "finance.command",
            ["RabbitMq:Client"] = "telegram-gateway-tests"
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
