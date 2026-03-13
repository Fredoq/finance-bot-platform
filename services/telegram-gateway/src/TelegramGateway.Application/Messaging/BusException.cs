namespace TelegramGateway.Application.Messaging;

/// <summary>
/// Represents a gateway bus failure that should surface as a transport error to the webhook caller.
/// Example:
/// <code>
/// throw new BusException("Message publish failed");
/// </code>
/// </summary>
public sealed class BusException(string message, Exception? inner = null) : Exception(message, inner);
