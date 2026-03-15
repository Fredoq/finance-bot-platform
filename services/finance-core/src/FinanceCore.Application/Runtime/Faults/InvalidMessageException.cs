namespace FinanceCore.Application.Runtime.Faults;

/// <summary>
/// Represents a malformed or unsupported inbound message.
/// </summary>
public sealed class InvalidMessageException : Exception
{
    /// <summary>
    /// Initializes a new instance of the exception.
    /// </summary>
    /// <param name="message">The error message.</param>
    public InvalidMessageException(string message) : base(message)
    {
    }
    /// <summary>
    /// Initializes a new instance of the exception.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="error">The inner exception.</param>
    public InvalidMessageException(string message, Exception error) : base(message, error)
    {
    }
}
