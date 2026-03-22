using System.Net;
using System.Net.Http.Json;
using System.Text;
using Finance.Application.Contracts.Entry;
using Finance.Application.Contracts.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TelegramGateway.Api.Tests.Infrastructure;
using TelegramGateway.Application.Messaging;
using TelegramGateway.Application.Telegram.Delivery;

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
        HttpResponseMessage response = await client.PostAsJsonAsync("/telegram/webhook", WebhookUpdate.Body("/start"));
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
        HttpResponseMessage response = await client.PostAsJsonAsync("/telegram/webhook", WebhookUpdate.Body("/help"));
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
        HttpResponseMessage response = await client.PostAsJsonAsync("/telegram/webhook", WebhookUpdate.Body("/start", DateTimeOffset.MaxValue.ToUnixTimeSeconds() + 1));
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
        HttpResponseMessage response = await client.PostAsJsonAsync("/telegram/webhook", WebhookUpdate.Body("/start promo-42"));
        MessageEnvelope<WorkspaceRequestedCommand> item = port.Items.Single().Note<WorkspaceRequestedCommand>();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Single(port.Items);
        Assert.Equal("workspace.requested", port.Items.Single().Contract);
        Assert.Equal("promo-42", item.Payload.Payload);
        Assert.Equal("edge-update-7", item.Context.CausationId);
        Assert.Equal("workspace-requested-7", item.Context.IdempotencyKey);
        Assert.NotEqual(item.Context.CausationId, item.Context.IdempotencyKey);
    }
    /// <summary>
    /// Verifies that a callback query is published.
    /// </summary>
    /// <returns>A task that completes when the operation finishes.</returns>
    [Fact(DisplayName = "Returns 200 and publishes one workspace input command for callback queries")]
    public async Task Accepts_callback()
    {
        var port = new RecordingWorkspacePort();
        var gate = new RecordingTelegramPort();
        await using var host = new GatewayApiFactory(Note(), port, new ReadyBrokerState(), data =>
        {
            data.RemoveAll<ITelegramPort>();
            data.AddSingleton<ITelegramPort>(gate);
        }, false);
        using HttpClient client = Client(host);
        HttpResponseMessage response = await client.PostAsJsonAsync("/telegram/webhook", WebhookUpdate.Callback("account.add"));
        MessageEnvelope<WorkspaceInputRequestedCommand> item = port.Items.Single().Note<WorkspaceInputRequestedCommand>();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Single(port.Items);
        Assert.Single(gate.Items);
        Assert.IsType<TelegramCallbackAck>(gate.Items.Single());
        Assert.Equal("workspace.input.requested", port.Items.Single().Contract);
        Assert.Equal("action", item.Payload.Kind);
        Assert.Equal("account.add", item.Payload.Value);
        Assert.Equal("edge-update-9", item.Context.CausationId);
    }
    /// <summary>
    /// Verifies that a plain text message is published as workspace input.
    /// </summary>
    /// <returns>A task that completes when the operation finishes.</returns>
    [Fact(DisplayName = "Returns 200 and publishes one workspace input command for plain text")]
    public async Task Accepts_text()
    {
        var port = new RecordingWorkspacePort();
        await using var host = new GatewayApiFactory(Note(), port, new ReadyBrokerState(), hosted: false);
        using HttpClient client = Client(host);
        HttpResponseMessage response = await client.PostAsJsonAsync("/telegram/webhook", WebhookUpdate.Body("Cash"));
        MessageEnvelope<WorkspaceInputRequestedCommand> item = port.Items.Single().Note<WorkspaceInputRequestedCommand>();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Single(port.Items);
        Assert.Equal("workspace.input.requested", port.Items.Single().Contract);
        Assert.Equal("text", item.Payload.Kind);
        Assert.Equal("Cash", item.Payload.Value);
        Assert.Equal("workspace-input-7", item.Context.IdempotencyKey);
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
        HttpResponseMessage response = await client.PostAsJsonAsync("/telegram/webhook", WebhookUpdate.Body("/start"));
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
    private static HttpClient Client(GatewayApiFactory host) => WebhookUpdate.Client(host);
    private static Dictionary<string, string?> Note() => GatewaySettings.Note("telegram-gateway-tests", "localhost", "5672", "/", "guest", "guest");
}
