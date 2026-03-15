namespace TelegramGateway.Application.Telegram.Delivery;

/// <summary>
/// Represents a Telegram delivery failure and whether retry is allowed.
/// </summary>
public sealed class DeliveryException : Exception
{
    /// <summary>
    /// Initializes a new instance of the exception.
    /// </summary>
    /// <param name="message">The failure message.</param>
    /// <param name="retryable">Indicates whether the failure may be retried.</param>
    /// <param name="inner">The inner failure.</param>
    public DeliveryException(string message, bool retryable, Exception? inner = null) : base(message, inner) => Retryable = retryable;
    /// <summary>
    /// Gets a value indicating whether retry is allowed.
    /// </summary>
    public bool Retryable { get; }
}
