using System.ComponentModel.DataAnnotations;
using FinanceCore.Infrastructure.Configuration.Postgres;
using FinanceCore.Infrastructure.Configuration.RabbitMq;

namespace FinanceCore.Api.Tests;

/// <summary>
/// Covers infrastructure option validation behavior.
/// </summary>
public sealed class ConfigurationOptionsTests
{
    /// <summary>
    /// Verifies that invalid RabbitMQ settings produce validation errors.
    /// </summary>
    /// <returns>A task that completes when the assertions finish.</returns>
    [Fact(DisplayName = "Returns validation errors for invalid RabbitMQ settings")]
    public Task Rejects_invalid_rabbit()
    {
        var item = new RabbitMqOptions
        {
            Host = string.Empty,
            Port = 0,
            VirtualHost = string.Empty,
            Username = string.Empty,
            Password = string.Empty,
            Exchange = string.Empty,
            Queue = "queue",
            RetryQueue = "queue",
            DeadQueue = "queue",
            Client = string.Empty,
            Prefetch = 0,
            RetryDelaySeconds = 0,
            MaxAttempts = 0,
            OutboxBatchSize = 0
        };
        ValidationResult[] list = item.Validate(new ValidationContext(item)).ToArray();
        Assert.Equal(14, list.Length);
        return Task.CompletedTask;
    }
    /// <summary>
    /// Verifies that an empty PostgreSQL connection string is rejected.
    /// </summary>
    /// <returns>A task that completes when the assertions finish.</returns>
    [Fact(DisplayName = "Returns a validation error for an empty PostgreSQL connection string")]
    public Task Rejects_empty_postgres()
    {
        var item = new PostgresOptions { ConnectionString = string.Empty };
        ValidationResult[] list = item.Validate(new ValidationContext(item)).ToArray();
        Assert.Single(list);
        return Task.CompletedTask;
    }
}
