using System.ComponentModel.DataAnnotations;
using TelegramGateway.Api;
using TelegramGateway.Infrastructure.Configuration;

namespace TelegramGateway.Api.Tests;

/// <summary>
/// Covers configuration validation behavior for webhook and RabbitMQ options.
/// </summary>
public sealed class ConfigurationValidationTests
{
    /// <summary>
    /// Verifies that blank webhook secrets are rejected.
    /// </summary>
    [Fact(DisplayName = "Rejects a blank webhook secret")]
    public void Rejects_secret_blank()
    {
        var item = new TelegramWebhookOptions { SecretToken = " " };
        ValidationResult[] note = item.Validate(new ValidationContext(item)).ToArray();
        Assert.Contains(note, data => data.MemberNames.SequenceEqual([nameof(TelegramWebhookOptions.SecretToken)]));
    }
    /// <summary>
    /// Verifies that blank RabbitMQ credentials are rejected.
    /// </summary>
    [Fact(DisplayName = "Rejects blank RabbitMQ credentials")]
    public void Rejects_password_blank()
    {
        var item = new RabbitMqOptions { Username = " ", Password = " " };
        ValidationResult[] note = item.Validate(new ValidationContext(item)).ToArray();
        Assert.Contains(note, data => data.MemberNames.Contains(nameof(RabbitMqOptions.Password), StringComparer.Ordinal));
    }
    /// <summary>
    /// Verifies that a blank exchange name is rejected.
    /// </summary>
    [Fact(DisplayName = "Rejects a blank RabbitMQ exchange name")]
    public void Rejects_exchange_blank()
    {
        var item = new RabbitMqOptions { Username = "guest", Password = "guest", Exchange = " " };
        ValidationResult[] note = item.Validate(new ValidationContext(item)).ToArray();
        Assert.Contains(note, data => data.MemberNames.Contains(nameof(RabbitMqOptions.Exchange), StringComparer.Ordinal));
    }
}
