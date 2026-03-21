using System.ComponentModel.DataAnnotations;

namespace Finance.Platform.RabbitMq;

internal static class RabbitMqValidation
{
    internal static void Connection(List<ValidationResult> list, RabbitMqConnectionOptions option)
    {
        ArgumentNullException.ThrowIfNull(list);
        ArgumentNullException.ThrowIfNull(option);
        if (!string.IsNullOrWhiteSpace(option.ConnectionString))
        {
            Address(list, option.ConnectionString, nameof(option.ConnectionString), "RabbitMq connection string must be absolute");
            Require(list, option.Client, nameof(option.Client), "RabbitMq client is required");
            return;
        }
        Require(list, option.Host, nameof(option.Host), "RabbitMq host is required");
        Range(list, option.Port, 1, 65535, nameof(option.Port), "RabbitMq port must be between 1 and 65535");
        Require(list, option.VirtualHost, nameof(option.VirtualHost), "RabbitMq virtual host is required");
        Require(list, option.Username, nameof(option.Username), "RabbitMq username is required");
        Require(list, option.Password, nameof(option.Password), "RabbitMq password is required");
        Require(list, option.Client, nameof(option.Client), "RabbitMq client is required");
    }

    private static void Address(List<ValidationResult> list, string value, string name, string error)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out Uri? item) || item.Scheme is not ("amqp" or "amqps"))
        {
            list.Add(new ValidationResult(error, [name]));
        }
    }
    internal static void Require(List<ValidationResult> list, string value, string name, string error)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            list.Add(new ValidationResult(error, [name]));
        }
    }

    private static void Range(List<ValidationResult> list, int value, int low, int high, string name, string error)
    {
        if (value < low || value > high)
        {
            list.Add(new ValidationResult(error, [name]));
        }
    }
    internal static void Positive(List<ValidationResult> list, int value, string name, string error)
    {
        if (value <= 0)
        {
            list.Add(new ValidationResult(error, [name]));
        }
    }
    internal static void Distinct(List<ValidationResult> list, string left, string right, string leftName, string rightName, string error)
    {
        if (!string.IsNullOrWhiteSpace(left) && !string.IsNullOrWhiteSpace(right) && string.Equals(left, right, StringComparison.Ordinal))
        {
            list.Add(new ValidationResult(error, [leftName, rightName]));
        }
    }
}
