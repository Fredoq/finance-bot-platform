namespace Finance.Platform.RabbitMq;

/// <summary>
/// Represents RabbitMQ connection settings shared by service runtimes.
/// </summary>
public abstract class RabbitMqConnectionOptions
{
    /// <summary>
    /// Gets or sets the broker host name.
    /// </summary>
    public string Host { get; init; } = "localhost";
    /// <summary>
    /// Gets or sets the broker port.
    /// </summary>
    public int Port { get; init; } = 5672;
    /// <summary>
    /// Gets or sets the broker virtual host.
    /// </summary>
    public string VirtualHost { get; init; } = "/";
    /// <summary>
    /// Gets or sets the broker user name.
    /// </summary>
    public string Username { get; init; } = string.Empty;
    /// <summary>
    /// Gets or sets the broker password.
    /// </summary>
    public string Password { get; init; } = string.Empty;
    /// <summary>
    /// Gets or sets the client name.
    /// </summary>
    public virtual string Client { get; init; } = string.Empty;
}
