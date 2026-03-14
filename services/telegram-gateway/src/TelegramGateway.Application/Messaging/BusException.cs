namespace TelegramGateway.Application.Messaging;

/// <summary>
/// Represents a gateway bus failure that should surface as a transport error to the webhook caller.
/// </summary>
public sealed class BusException : Exception
{
    /// <summary>
    /// Initializes a new instance of the exception.
    /// </summary>
    /// <param name="message">The failure message.</param>
    /// <param name="inner">The inner failure.</param>
    public BusException(string message, Exception? inner = null) : base(message, inner)
    {
    }
}
