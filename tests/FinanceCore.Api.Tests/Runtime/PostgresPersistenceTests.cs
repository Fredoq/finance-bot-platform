using Finance.Application.Contracts.Entry;
using Finance.Application.Contracts.Messaging;
using FinanceCore.Api.Tests.Infrastructure;
using FinanceCore.Infrastructure.Persistence.Postgres.Inbox;
using FinanceCore.Infrastructure.Persistence.Postgres.Outbox;
using Npgsql;

namespace FinanceCore.Api.Tests.Runtime;

/// <summary>
/// Covers direct PostgreSQL persistence behavior.
/// </summary>
public sealed class PostgresPersistenceTests : FinanceCoreRuntimeSuite
{
    /// <summary>
    /// Verifies that the inbox port stores an inbound envelope once.
    /// </summary>
    /// <returns>A task that completes when the assertions finish.</returns>
    [Fact(DisplayName = "Stores inbound envelopes in the inbox table")]
    public async Task Stores_inbox_message()
    {
        await using var host = new CoreApiFactory(Settings("finance-core-inbox-port"), false);
        using HttpClient client = host.CreateClient();
        await Ready(client);
        await Reset();
        await using NpgsqlDataSource data = new NpgsqlDataSourceBuilder(Postgres()).Build();
        var item = new PostgresInboxPort(data);
        await item.Save(Envelope("actor-7", "room-7", "payload", "workspace-requested-7"), default);
        Assert.Equal(1, await Number("select count(*) from finance.inbox_message"));
    }
    /// <summary>
    /// Verifies that the outbox port stores, loads, fails, and marks an outbound envelope.
    /// </summary>
    /// <returns>A task that completes when the assertions finish.</returns>
    [Fact(DisplayName = "Stores and updates outbound envelopes in the outbox table")]
    public async Task Stores_outbox_message()
    {
        await using var host = new CoreApiFactory(Settings("finance-core-outbox-port"), false);
        using HttpClient client = host.CreateClient();
        await Ready(client);
        await Reset();
        await using NpgsqlDataSource data = new NpgsqlDataSourceBuilder(Postgres()).Build();
        var item = new PostgresOutboxPort(data);
        MessageEnvelope<WorkspaceViewRequestedCommand> note = ViewEnvelope();
        await item.Save(note, "workspace.view.requested", default);
        IReadOnlyList<OutboxItem> list = await item.Items(10, default);
        Assert.Single(list);
        await item.Fail(note.MessageId, "broken", default);
        await item.Mark(note.MessageId, default);
        Assert.Equal(1, await Number("select count(*) from finance.outbox_message where attempt = 1 and error = 'broken' and published_utc is not null"));
    }
    private static MessageEnvelope<WorkspaceViewRequestedCommand> ViewEnvelope() => new(
        Guid.CreateVersion7(),
        "workspace.view.requested",
        DateTimeOffset.UtcNow,
        new MessageContext($"trace-{Guid.CreateVersion7():N}", $"cause-{Guid.CreateVersion7():N}", $"view-{Guid.CreateVersion7():N}"),
        "finance-core",
        new WorkspaceViewRequestedCommand(new WorkspaceIdentity("actor-8", "room-8"), new WorkspaceProfile("Alex", "en"), "home", "{\"accounts\":[]}", ["account.add"], false, false, DateTimeOffset.UtcNow));
}
