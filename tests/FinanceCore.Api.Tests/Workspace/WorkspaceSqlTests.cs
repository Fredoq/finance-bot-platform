using FinanceCore.Api.Tests.Infrastructure;
using FinanceCore.Infrastructure.Persistence.Postgres.Workspace;
using Npgsql;

namespace FinanceCore.Api.Tests.Workspace;

/// <summary>
/// Covers direct PostgreSQL access methods used by workspace persistence.
/// </summary>
public sealed class WorkspaceSqlTests : FinanceCoreRuntimeSuite
{
    /// <summary>
    /// Verifies that account rows are loaded for one user.
    /// </summary>
    [Fact(DisplayName = "Loads account rows for the requested user")]
    public async Task Loads_accounts()
    {
        await using var host = new CoreApiFactory(Settings("finance-core-workspace-sql"));
        using HttpClient client = host.CreateClient();
        await Ready(client);
        var user = Guid.CreateVersion7();
        var user2 = Guid.CreateVersion7();
        await Execute("insert into finance.user_account(id, actor_key, name, locale, time_zone, created_utc, updated_utc) values (@id, @actor, @name, @locale, @time_zone, @created, @updated)", ("id", user), ("actor", "actor-sql"), ("name", "Alex"), ("locale", "en"), ("time_zone", WorkspaceZone.Default), ("created", DateTimeOffset.UtcNow), ("updated", DateTimeOffset.UtcNow));
        await Execute("insert into finance.user_account(id, actor_key, name, locale, time_zone, created_utc, updated_utc) values (@id, @actor, @name, @locale, @time_zone, @created, @updated)", ("id", user2), ("actor", "actor-sql-2"), ("name", "Sam"), ("locale", "en"), ("time_zone", WorkspaceZone.Default), ("created", DateTimeOffset.UtcNow), ("updated", DateTimeOffset.UtcNow));
        await Execute("insert into finance.account(id, user_id, name, currency_code, opening_amount, current_amount, created_utc, updated_utc) values (@id, @user, @name, @currency, @opening, @current, @created, @updated)", ("id", Guid.CreateVersion7()), ("user", user), ("name", "Cash"), ("currency", "USD"), ("opening", 10m), ("current", 10m), ("created", DateTimeOffset.UtcNow), ("updated", DateTimeOffset.UtcNow));
        await Execute("insert into finance.account(id, user_id, name, currency_code, opening_amount, current_amount, created_utc, updated_utc) values (@id, @user, @name, @currency, @opening, @current, @created, @updated)", ("id", Guid.CreateVersion7()), ("user", user2), ("name", "Card"), ("currency", "EUR"), ("opening", 20m), ("current", 20m), ("created", DateTimeOffset.UtcNow), ("updated", DateTimeOffset.UtcNow));
        await using var source = NpgsqlDataSource.Create(Postgres());
        await using NpgsqlConnection link = await source.OpenConnectionAsync();
        await using NpgsqlTransaction lane = await link.BeginTransactionAsync();
        IReadOnlyList<AccountData> list = await new WorkspaceSql(new WorkspaceBody()).Accounts(link, lane, user, default);
        Assert.Single(list);
        Assert.Equal("Cash", list.Single().Name);
    }
}
