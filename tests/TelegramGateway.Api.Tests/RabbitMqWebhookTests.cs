using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Finance.Application.Contracts.Entry;
using Finance.Application.Contracts.Messaging;
using RabbitMQ.Client;
using TelegramGateway.Api.Tests.Infrastructure;
using Testcontainers.RabbitMq;

namespace TelegramGateway.Api.Tests;

/// <summary>
/// Covers webhook behavior with a real RabbitMQ broker.
/// </summary>
public sealed class RabbitMqWebhookTests : IAsyncLifetime
{
    private static readonly JsonSerializerOptions json = new(JsonSerializerDefaults.Web);
    private readonly RabbitMqContainer box = new RabbitMqBuilder("rabbitmq:management").WithUsername("finance").WithPassword("finance").Build();
    private string uri = string.Empty;
    /// <summary>
    /// Starts the RabbitMQ container and waits for broker connectivity.
    /// </summary>
    /// <returns>A task that completes when the operation finishes.</returns>
    public async Task InitializeAsync()
    {
        await box.StartAsync();
        uri = box.GetConnectionString();
    }
    /// <summary>
    /// Stops the RabbitMQ container.
    /// </summary>
    /// <returns>A task that completes when the operation finishes.</returns>
    public Task DisposeAsync() => box.DisposeAsync().AsTask();
    /// <summary>
    /// Verifies end-to-end publish with a bound queue.
    /// </summary>
    /// <returns>A task that completes when the operation finishes.</returns>
    [Fact(DisplayName = "Publishes one workspace command to RabbitMQ when the queue is bound")]
    public async Task Accepts_publish()
    {
        string name = $"workspace-{Guid.CreateVersion7():N}";
        await Bind(name);
        await using var host = new GatewayApiFactory(Note("telegram-gateway-rabbit"));
        using HttpClient item = WebhookUpdate.Client(host);
        HttpResponseMessage note = await item.PostAsJsonAsync("/telegram/webhook", WebhookUpdate.Body("/start promo-42"));
        MessageEnvelope<WorkspaceRequestedCommand>? data = await Message(name);
        Assert.Equal(HttpStatusCode.OK, note.StatusCode);
        Assert.NotNull(data);
        Assert.Equal("promo-42", data!.Payload.Payload);
    }
    /// <summary>
    /// Verifies that an unroutable publish becomes a service unavailable response.
    /// </summary>
    /// <returns>A task that completes when the operation finishes.</returns>
    [Fact(DisplayName = "Returns 503 when RabbitMQ cannot route the command")]
    public async Task Rejects_publish()
    {
        await using var host = new GatewayApiFactory(Note("telegram-gateway-rabbit"));
        using HttpClient item = WebhookUpdate.Client(host);
        HttpResponseMessage note = await item.PostAsJsonAsync("/telegram/webhook", WebhookUpdate.Body("/start"));
        Assert.Equal(HttpStatusCode.ServiceUnavailable, note.StatusCode);
    }
    private async Task Bind(string name)
    {
        var item = new ConnectionFactory { Uri = new Uri(uri) };
        await using IConnection link = await item.CreateConnectionAsync();
        await using IChannel lane = await link.CreateChannelAsync(cancellationToken: default);
        await lane.ExchangeDeclareAsync("finance.command", ExchangeType.Topic, true, false, null, false, false, default);
        _ = await lane.QueueDeclareAsync(name, false, false, true, null, false, false, default);
        await lane.QueueBindAsync(name, "finance.command", "workspace.requested", null, false, default);
    }
    private async Task<MessageEnvelope<WorkspaceRequestedCommand>?> Message(string name)
    {
        var item = new ConnectionFactory { Uri = new Uri(uri) };
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
                break;
            }
            if (data is not null)
            {
                return JsonSerializer.Deserialize<MessageEnvelope<WorkspaceRequestedCommand>>(data.Body.Span, json);
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
    private Dictionary<string, string?> Note(string name)
    {
        var item = new Uri(uri);
        return GatewaySettings.Note(name, item);
    }
}
