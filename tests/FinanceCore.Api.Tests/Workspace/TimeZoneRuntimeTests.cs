using System.Text.Json;
using Finance.Application.Contracts.Entry;
using Finance.Application.Contracts.Messaging;
using FinanceCore.Api.Tests.Infrastructure;

namespace FinanceCore.Api.Tests.Workspace;

/// <summary>
/// Covers workspace time zone settings behavior with real PostgreSQL and RabbitMQ dependencies.
/// </summary>
public sealed class TimeZoneRuntimeTests : FinanceCoreRuntimeSuite
{
    /// <summary>
    /// Verifies that home exposes the time zone action and opens the edit screen.
    /// </summary>
    [Fact(DisplayName = "Opens the time zone edit screen from home")]
    public async Task Opens_time_zone()
    {
        string queue = $"view-{Guid.CreateVersion7():N}";
        await using var host = new CoreApiFactory(Settings("finance-core-time-zone-open"));
        using HttpClient client = host.CreateClient();
        await Ready(client);
        await Reset();
        await Bind(queue, "workspace.view.requested");
        await Publish(Envelope("actor-zone-open", "room-zone-open", string.Empty, "zone-open-home"));
        MessageEnvelope<WorkspaceViewRequestedCommand> home = await Take(queue, "zone-open-home");
        Assert.Contains("profile.timezone.show", home.Payload.Frame.Actions, StringComparer.Ordinal);
        await Publish(Input("actor-zone-open", "room-zone-open", "action", "profile.timezone.show", "zone-open-show"));
        MessageEnvelope<WorkspaceViewRequestedCommand> view = await Take(queue, "zone-open-show");
        Assert.Equal("profile.timezone.edit", view.Payload.Frame.State);
        Assert.Equal("Etc/UTC", TimeZone(view.Payload.Frame.StateData));
        Assert.Equal(["profile.timezone.cancel"], view.Payload.Frame.Actions);
    }

    /// <summary>
    /// Verifies that a valid time zone is stored and applied by the next summary screen.
    /// </summary>
    [Fact(DisplayName = "Updates the time zone and applies it to the next summary")]
    public async Task Updates_time_zone()
    {
        const string zone = "Europe/Moscow";
        string queue = $"view-{Guid.CreateVersion7():N}";
        await using var host = new CoreApiFactory(Settings("finance-core-time-zone-update"));
        using HttpClient client = host.CreateClient();
        await Ready(client);
        await Reset();
        await Bind(queue, "workspace.view.requested");
        await Create(queue, "actor-zone-update", "room-zone-update", "Cash", "USD", "100", "zone-update-account");
        await Publish(Input("actor-zone-update", "room-zone-update", "action", "profile.timezone.show", "zone-update-show"));
        _ = await Take(queue, "zone-update-show");
        await Publish(Input("actor-zone-update", "room-zone-update", "text", zone, "zone-update-save"));
        MessageEnvelope<WorkspaceViewRequestedCommand> home = await Take(queue, "zone-update-save");
        Assert.Equal("home", home.Payload.Frame.State);
        Assert.Equal("Time zone was updated", Notice(home.Payload.Frame.StateData));
        Assert.Equal(zone, await Scalar("select time_zone from finance.user_account where actor_key = 'actor-zone-update'"));
        await Publish(InputAt("actor-zone-update", "room-zone-update", "action", "summary.month.show", "zone-update-summary", new DateTimeOffset(2026, 3, 31, 21, 30, 0, TimeSpan.Zero)));
        MessageEnvelope<WorkspaceViewRequestedCommand> summary = await Take(queue, "zone-update-summary");
        Assert.Equal("summary.month", summary.Payload.Frame.State);
        Assert.Equal(2026, Year(summary.Payload.Frame.StateData));
        Assert.Equal(4, Month(summary.Payload.Frame.StateData));
        Assert.Equal(zone, SummaryTimeZone(summary.Payload.Frame.StateData));
    }

    /// <summary>
    /// Verifies that an invalid time zone keeps the user on the edit screen with an error.
    /// </summary>
    [Fact(DisplayName = "Rejects an invalid time zone id")]
    public async Task Rejects_time_zone()
    {
        const string zone = "Europe/Moscow";
        string queue = $"view-{Guid.CreateVersion7():N}";
        await using var host = new CoreApiFactory(Settings("finance-core-time-zone-invalid"));
        using HttpClient client = host.CreateClient();
        await Ready(client);
        await Reset();
        await Bind(queue, "workspace.view.requested");
        await Create(queue, "actor-zone-invalid", "room-zone-invalid", "Cash", "USD", "100", "zone-invalid-account");
        await Zone("actor-zone-invalid", zone);
        await Publish(Envelope("actor-zone-invalid", "room-zone-invalid", string.Empty, "zone-invalid-home"));
        _ = await Take(queue, "zone-invalid-home");
        await Publish(Input("actor-zone-invalid", "room-zone-invalid", "action", "profile.timezone.show", "zone-invalid-show"));
        _ = await Take(queue, "zone-invalid-show");
        await Publish(Input("actor-zone-invalid", "room-zone-invalid", "text", "Mars/Olympus", "zone-invalid-save"));
        MessageEnvelope<WorkspaceViewRequestedCommand> view = await Take(queue, "zone-invalid-save");
        Assert.Equal("profile.timezone.edit", view.Payload.Frame.State);
        Assert.Equal("Send a valid IANA time zone id", Error(view.Payload.Frame.StateData));
        Assert.Equal(zone, TimeZone(view.Payload.Frame.StateData));
        Assert.Equal(zone, await Scalar("select time_zone from finance.user_account where actor_key = 'actor-zone-invalid'"));
        await Publish(Input("actor-zone-invalid", "room-zone-invalid", "action", "profile.timezone.cancel", "zone-invalid-cancel"));
        _ = await Take(queue, "zone-invalid-cancel");
        await Publish(InputAt("actor-zone-invalid", "room-zone-invalid", "action", "summary.month.show", "zone-invalid-summary", new DateTimeOffset(2026, 4, 20, 12, 0, 0, TimeSpan.Zero)));
        MessageEnvelope<WorkspaceViewRequestedCommand> summary = await Take(queue, "zone-invalid-summary");
        Assert.Equal("summary.month", summary.Payload.Frame.State);
        Assert.Equal(zone, SummaryTimeZone(summary.Payload.Frame.StateData));
    }

    /// <summary>
    /// Verifies that the user can cancel the time zone edit flow.
    /// </summary>
    [Fact(DisplayName = "Cancels the time zone edit flow")]
    public async Task Cancels_time_zone()
    {
        const string zone = "Europe/Moscow";
        string queue = $"view-{Guid.CreateVersion7():N}";
        await using var host = new CoreApiFactory(Settings("finance-core-time-zone-cancel"));
        using HttpClient client = host.CreateClient();
        await Ready(client);
        await Reset();
        await Bind(queue, "workspace.view.requested");
        await Create(queue, "actor-zone-cancel", "room-zone-cancel", "Cash", "USD", "100", "zone-cancel-account");
        await Zone("actor-zone-cancel", zone);
        await Publish(Envelope("actor-zone-cancel", "room-zone-cancel", string.Empty, "zone-cancel-home"));
        _ = await Take(queue, "zone-cancel-home");
        await Publish(Input("actor-zone-cancel", "room-zone-cancel", "action", "profile.timezone.show", "zone-cancel-show"));
        _ = await Take(queue, "zone-cancel-show");
        await Publish(Input("actor-zone-cancel", "room-zone-cancel", "action", "profile.timezone.cancel", "zone-cancel-apply"));
        MessageEnvelope<WorkspaceViewRequestedCommand> home = await Take(queue, "zone-cancel-apply");
        Assert.Equal("home", home.Payload.Frame.State);
        Assert.Equal("Time zone update was cancelled", Notice(home.Payload.Frame.StateData));
        Assert.Equal(zone, await Scalar("select time_zone from finance.user_account where actor_key = 'actor-zone-cancel'"));
        await Publish(InputAt("actor-zone-cancel", "room-zone-cancel", "action", "summary.month.show", "zone-cancel-summary", new DateTimeOffset(2026, 4, 20, 12, 0, 0, TimeSpan.Zero)));
        MessageEnvelope<WorkspaceViewRequestedCommand> summary = await Take(queue, "zone-cancel-summary");
        Assert.Equal(zone, SummaryTimeZone(summary.Payload.Frame.StateData));
    }

    private async Task<MessageEnvelope<WorkspaceViewRequestedCommand>> Create(string queue, string actor, string room, string name, string currency, string balance, string id)
    {
        await Publish(Input(actor, room, "action", "account.add", $"{id}-1"));
        await Take(queue, $"{id}-1");
        await Publish(Input(actor, room, "text", name, $"{id}-2"));
        await Take(queue, $"{id}-2");
        await Publish(Currency(actor, room, currency, $"{id}-3"));
        await Take(queue, $"{id}-3");
        await Publish(Input(actor, room, "text", balance, $"{id}-4"));
        await Take(queue, $"{id}-4");
        await Publish(Input(actor, room, "action", "account.create", $"{id}-5"));
        return await Take(queue, $"{id}-5");
    }

    private async Task<MessageEnvelope<WorkspaceViewRequestedCommand>> Take(string queue, string step)
    {
        MessageEnvelope<WorkspaceViewRequestedCommand>? item = await View(queue);
        return item ?? throw new InvalidOperationException($"Workspace view is missing after '{step}'");
    }

    private static MessageEnvelope<WorkspaceInputRequestedCommand> Currency(string actor, string room, string currency, string id)
    {
        string text = currency.Trim().ToUpperInvariant();
        return text switch
        {
            "RUB" => Input(actor, room, "action", "account.currency.rub", id),
            "USD" => Input(actor, room, "action", "account.currency.usd", id),
            "EUR" => Input(actor, room, "action", "account.currency.eur", id),
            _ => Input(actor, room, "text", currency, id)
        };
    }

    private static MessageEnvelope<WorkspaceInputRequestedCommand> InputAt(string actor, string room, string kind, string value, string id, DateTimeOffset when) => new(
        Guid.CreateVersion7(),
        "workspace.input.requested",
        when,
        new MessageContext($"trace-{Guid.CreateVersion7():N}", $"cause-{Guid.CreateVersion7():N}", id),
        "telegram-gateway",
        new WorkspaceInputRequestedCommand(new WorkspaceIdentity(actor, room), new WorkspaceProfile("Alex", "en"), kind, value, when));

    private static string TimeZone(string data)
    {
        using var item = JsonDocument.Parse(data);
        return item.RootElement.GetProperty("profile").GetProperty("timeZone").GetString() ?? string.Empty;
    }

    private static string Notice(string data)
    {
        using var item = JsonDocument.Parse(data);
        return item.RootElement.GetProperty("status").GetProperty("notice").GetString() ?? string.Empty;
    }

    private static string Error(string data)
    {
        using var item = JsonDocument.Parse(data);
        return item.RootElement.GetProperty("status").GetProperty("error").GetString() ?? string.Empty;
    }

    private static int Year(string data)
    {
        using var item = JsonDocument.Parse(data);
        return item.RootElement.GetProperty("summary").GetProperty("year").GetInt32();
    }

    private static int Month(string data)
    {
        using var item = JsonDocument.Parse(data);
        return item.RootElement.GetProperty("summary").GetProperty("month").GetInt32();
    }

    private static string SummaryTimeZone(string data)
    {
        using var item = JsonDocument.Parse(data);
        return item.RootElement.GetProperty("summary").GetProperty("timeZone").GetString() ?? string.Empty;
    }
}
