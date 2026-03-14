using DbUp;
using DbUp.Engine;
using FinanceCore.Infrastructure.Configuration.Postgres;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;

namespace FinanceCore.Infrastructure.Persistence.Postgres.Migrations;

internal sealed class MigrationBoot : IHostedService
{
    private const int count = 5;
    private const long key = 5_832_147_011;
    private static readonly TimeSpan pause = TimeSpan.FromSeconds(1);
    private readonly IOptions<PostgresOptions> option;
    private readonly ILoggerFactory factory;
    private readonly ILogger<MigrationBoot> log;
    internal MigrationBoot(IOptions<PostgresOptions> option, ILoggerFactory factory, ILogger<MigrationBoot> log)
    {
        this.option = option ?? throw new ArgumentNullException(nameof(option));
        this.factory = factory ?? throw new ArgumentNullException(nameof(factory));
        this.log = log ?? throw new ArgumentNullException(nameof(log));
    }
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        TimeSpan span = pause;
        for (int item = 1; item <= count; item++)
        {
            try
            {
                await Ensure(cancellationToken);
                return;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception error) when (item < count)
            {
                log.LogWarning(error, "Postgres migration warm-up failed on attempt {Attempt} of {Count}", item, count);
                await Task.Delay(span, cancellationToken);
                span = TimeSpan.FromSeconds(Math.Min(span.TotalSeconds * 2, 15));
            }
        }
    }
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    private async ValueTask Ensure(CancellationToken token)
    {
        string text = option.Value.ConnectionString;
        await using NpgsqlConnection link = new(text);
        await link.OpenAsync(token);
        await using NpgsqlCommand note = new("create schema if not exists finance", link);
        _ = await note.ExecuteNonQueryAsync(token);
        await Lock(link, token);
        UpgradeEngine item = DeployChanges.To.PostgresqlDatabase(text).JournalToPostgresqlTable("finance", "schema_journal").WithScriptsEmbeddedInAssembly(typeof(MigrationBoot).Assembly, Name).LogTo(new DbUpLog(factory.CreateLogger<DbUpLog>())).Build();
        try
        {
            DatabaseUpgradeResult data = item.PerformUpgrade();
            if (!data.Successful)
            {
                throw new InvalidOperationException("Postgres migration failed", data.Error);
            }
        }
        finally
        {
            await Unlock(link, CancellationToken.None);
        }
    }
    private static bool Name(string text) => text.Contains(".Persistence.Postgres.Migrations.Scripts.", StringComparison.Ordinal) && text.EndsWith(".sql", StringComparison.Ordinal);
    private static async ValueTask Lock(NpgsqlConnection link, CancellationToken token)
    {
        using var note = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        using var item = CancellationTokenSource.CreateLinkedTokenSource(token, note.Token);
        while (true)
        {
            try
            {
                await using NpgsqlCommand gate = new("select pg_try_advisory_lock(@key)", link);
                gate.Parameters.AddWithValue("key", key);
                object? data = await gate.ExecuteScalarAsync(item.Token);
                if (data is bool state && state)
                {
                    return;
                }
                await Task.Delay(250, item.Token);
            }
            catch (OperationCanceledException) when (note.IsCancellationRequested)
            {
                throw new TimeoutException("Postgres migration lock timed out");
            }
        }
    }
    private static async ValueTask Unlock(NpgsqlConnection link, CancellationToken token)
    {
        await using NpgsqlCommand note = new("select pg_advisory_unlock(@key)", link);
        note.Parameters.AddWithValue("key", key);
        _ = await note.ExecuteScalarAsync(token);
    }
}
