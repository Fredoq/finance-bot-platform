using System.Text;
using Finance.Application.Contracts.Entry;
using Finance.Application.Contracts.Messaging;
using FinanceCore.Api.Tests.Infrastructure;
using RabbitMQ.Client;

namespace FinanceCore.Api.Tests.Workspace;

/// <summary>
/// Covers finance core behavior with real PostgreSQL and RabbitMQ dependencies.
/// </summary>
public sealed class WorkspaceRuntimeTests : FinanceCoreRuntimeSuite
{
    /// <summary>
    /// Verifies that the first workspace request creates state and publishes a view.
    /// </summary>
    /// <returns>A task that completes when the operation finishes.</returns>
    [Fact(DisplayName = "Creates workspace state and publishes one view for the first request")]
    public async Task Creates_workspace()
    {
        string queue = $"view-{Guid.CreateVersion7():N}";
        await using var host = new CoreApiFactory(Settings("finance-core-create"));
        using HttpClient client = host.CreateClient();
        await Ready(client);
        await Reset();
        await Bind(queue, "workspace.view.requested");
        await Publish(Envelope("actor-1", "room-1", "promo-42", "workspace-requested-1"));
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
        await using var host = new CoreApiFactory(Settings("finance-core-dedupe"));
        using HttpClient client = host.CreateClient();
        await Ready(client);
        await Reset();
        await Bind(queue, "workspace.view.requested");
        await Publish(Envelope("actor-2", "room-2", "promo-21", "workspace-requested-2"));
        await Publish(Envelope("actor-2", "room-2", "promo-21", "workspace-requested-2"));
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
        await using var host = new CoreApiFactory(Settings("finance-core-conversation"));
        using HttpClient client = host.CreateClient();
        await Ready(client);
        await Reset();
        await Bind(queue, "workspace.view.requested");
        await Publish(Envelope("actor-3", "room-3", string.Empty, "workspace-requested-3"));
        _ = await View(queue);
        await Publish(Envelope("actor-3", "room-4", string.Empty, "workspace-requested-4"));
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
        await using var host = new CoreApiFactory(Settings("finance-core-state"));
        using HttpClient client = host.CreateClient();
        await Ready(client);
        await Reset();
        await Bind(queue, "workspace.view.requested");
        await Publish(Envelope("actor-4", "room-5", "first", "workspace-requested-5"));
        _ = await View(queue);
        await Execute("update finance.workspace set state_code = 'expense-draft', state_data = '{\"step\":\"amount\"}'::jsonb where conversation_key = 'room-5'");
        await Publish(Envelope("actor-4", "room-5", "second", "workspace-requested-6"));
        MessageEnvelope<WorkspaceViewRequestedCommand>? view = await View(queue);
        Assert.NotNull(view);
        Assert.Equal("expense-draft", view!.Payload.State);
        Assert.Equal("second", await Scalar("select last_payload from finance.workspace where conversation_key = 'room-5'"));
        Assert.Equal(2L, await Number("select revision from finance.workspace where conversation_key = 'room-5'"));
    }
    /// <summary>
    /// Verifies that unknown contracts are moved to the dead queue.
    /// </summary>
    /// <returns>A task that completes when the operation finishes.</returns>
    [Fact(DisplayName = "Moves unsupported contracts to the dead queue")]
    public async Task Rejects_contract()
    {
        await using var host = new CoreApiFactory(Settings("finance-core-unknown"));
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
        await using var host = new CoreApiFactory(Settings("finance-core-payload"));
        using HttpClient client = host.CreateClient();
        await Ready(client);
        await Reset();
        await Publish("workspace.requested", "workspace.requested", Encoding.UTF8.GetBytes("{"));
        BasicGetResult? data = await Dead();
        Assert.NotNull(data);
        Assert.Equal(0, await Number("select count(*) from finance.user_account"));
    }
}
