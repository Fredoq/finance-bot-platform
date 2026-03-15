using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RabbitMQ.Client;
using TelegramGateway.Api.Tests.Infrastructure;
using TelegramGateway.Application.Telegram.Delivery;

namespace TelegramGateway.Api.Tests;

/// <summary>
/// Covers Telegram delivery behavior with a real RabbitMQ broker.
/// </summary>
public sealed class RabbitMqDeliveryTests : GatewayRuntimeSuite
{
    /// <summary>
    /// Verifies that a workspace view is delivered to the Telegram port.
    /// </summary>
    [Fact(DisplayName = "Consumes a workspace view and sends one Telegram message")]
    public async Task Sends_view()
    {
        var port = new RecordingTelegramPort();
        await using var host = new GatewayApiFactory(Settings("telegram-gateway-delivery"), amend: items =>
        {
            items.RemoveAll<ITelegramPort>();
            items.AddSingleton<ITelegramPort>(port);
        });
        using HttpClient client = host.CreateClient();
        await Ready(client);
        await Publish(Envelope(100));
        using var note = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        while (!note.IsCancellationRequested && port.Items.Count == 0)
        {
            await Task.Delay(100, note.Token);
        }
        Assert.Single(port.Items);
        Assert.Null(await Queue("telegram-gateway.delivery.dead"));
    }
    /// <summary>
    /// Verifies that retryable transport failures move the message to the retry queue.
    /// </summary>
    [Fact(DisplayName = "Moves retryable Telegram failures to the retry queue")]
    public async Task Retries_transport()
    {
        var port = new RecordingTelegramPort(new DeliveryException("Telegram transport failed", true));
        await using var host = new GatewayApiFactory(Settings("telegram-gateway-retry"), amend: items =>
        {
            items.RemoveAll<ITelegramPort>();
            items.AddSingleton<ITelegramPort>(port);
        });
        using HttpClient client = host.CreateClient();
        await Ready(client);
        await Publish(Envelope(100));
        BasicGetResult? item = await Queue("telegram-gateway.delivery.retry");
        Assert.NotNull(item);
    }
    /// <summary>
    /// Verifies that unsupported contracts move to the dead queue.
    /// </summary>
    [Fact(DisplayName = "Moves unsupported delivery contracts to the dead queue")]
    public async Task Rejects_contract()
    {
        var port = new RecordingTelegramPort();
        await using var host = new GatewayApiFactory(Settings("telegram-gateway-dead"), amend: items =>
        {
            items.RemoveAll<ITelegramPort>();
            items.AddSingleton<ITelegramPort>(port);
        });
        using HttpClient client = host.CreateClient();
        await Ready(client);
        await Publish("budget.unknown", "budget.unknown", System.Text.Encoding.UTF8.GetBytes("{\"contract\":\"budget.unknown\"}"));
        BasicGetResult? item = await Queue("telegram-gateway.delivery.dead");
        Assert.NotNull(item);
        Assert.Empty(port.Items);
    }
}
