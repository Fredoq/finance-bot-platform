namespace TelegramGateway.Application.Messaging;

/// <summary>
/// Represents a gateway bus failure that should surface as a transport error to the webhook caller.
/// </summary>
/// <param name="message">The failure message.</param>
/// <param name="inner">The inner failure.</param>
public sealed class BusException(string message, Exception? inner = null) : Exception(message, inner);
