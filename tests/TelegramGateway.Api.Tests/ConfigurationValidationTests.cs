using System.ComponentModel.DataAnnotations;
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
    /// Verifies that a blank command exchange name is rejected.
    /// </summary>
    [Fact(DisplayName = "Rejects a blank RabbitMQ command exchange name")]
    public void Rejects_exchange_blank()
    {
        var item = new RabbitMqOptions { Username = "guest", Password = "guest", CommandExchange = " " };
        ValidationResult[] note = item.Validate(new ValidationContext(item)).ToArray();
        Assert.Contains(note, data => data.MemberNames.Contains(nameof(RabbitMqOptions.CommandExchange), StringComparer.Ordinal));
    }
    /// <summary>
    /// Verifies that a blank RabbitMQ client name is rejected.
    /// </summary>
    [Fact(DisplayName = "Rejects a blank RabbitMQ client name")]
    public void Rejects_client_blank()
    {
        var item = new RabbitMqOptions { Username = "guest", Password = "guest", Client = " " };
        ValidationResult[] note = item.Validate(new ValidationContext(item)).ToArray();
        Assert.Contains(note, data => data.MemberNames.Contains(nameof(RabbitMqOptions.Client), StringComparer.Ordinal));
    }
    /// <summary>
    /// Verifies that an absolute AMQPS connection string is accepted.
    /// </summary>
    [Fact(DisplayName = "Accepts an absolute RabbitMQ connection string")]
    public void Accepts_connection_string()
    {
        var item = new RabbitMqOptions { ConnectionString = "amqps://guest:guest@rabbitmq:5671/%2f", Client = "telegram-gateway" };
        ValidationResult[] note = item.Validate(new ValidationContext(item)).ToArray();
        Assert.Empty(note);
    }
    /// <summary>
    /// Verifies that a non-AMQP connection string is rejected.
    /// </summary>
    [Fact(DisplayName = "Rejects a RabbitMQ connection string with a non AMQP scheme")]
    public void Rejects_connection_string_scheme()
    {
        var item = new RabbitMqOptions { ConnectionString = "https://rabbitmq.local", Client = "telegram-gateway" };
        ValidationResult[] note = item.Validate(new ValidationContext(item)).ToArray();
        Assert.Contains(note, data => data.MemberNames.Contains(nameof(RabbitMqOptions.ConnectionString), StringComparer.Ordinal));
    }
    /// <summary>
    /// Verifies that identical command and delivery exchanges are rejected.
    /// </summary>
    [Fact(DisplayName = "Rejects identical RabbitMQ command and delivery exchanges")]
    public void Rejects_exchange_same()
    {
        var item = new RabbitMqOptions { Username = "guest", Password = "guest", CommandExchange = "finance", DeliveryExchange = "finance" };
        ValidationResult[] note = item.Validate(new ValidationContext(item)).ToArray();
        Assert.Contains(note, data => data.MemberNames.Contains(nameof(RabbitMqOptions.CommandExchange), StringComparer.Ordinal) && data.MemberNames.Contains(nameof(RabbitMqOptions.DeliveryExchange), StringComparer.Ordinal));
    }
    /// <summary>
    /// Verifies that a blank Telegram bot token is rejected.
    /// </summary>
    [Fact(DisplayName = "Rejects a blank Telegram bot token")]
    public void Rejects_bot_token_blank()
    {
        var item = new TelegramBotOptions { Token = " " };
        ValidationResult[] note = item.Validate(new ValidationContext(item)).ToArray();
        Assert.Contains(note, data => data.MemberNames.Contains(nameof(TelegramBotOptions.Token), StringComparer.Ordinal));
    }
    /// <summary>
    /// Verifies that a blank opaque key secret is rejected.
    /// </summary>
    [Fact(DisplayName = "Rejects a blank Telegram key secret")]
    public void Rejects_key_secret_blank()
    {
        var item = new OpaqueKeyOptions { CurrentSecret = " " };
        ValidationResult[] note = item.Validate(new ValidationContext(item)).ToArray();
        Assert.Contains(note, data => data.MemberNames.Contains(nameof(OpaqueKeyOptions.CurrentSecret), StringComparer.Ordinal));
    }
}
