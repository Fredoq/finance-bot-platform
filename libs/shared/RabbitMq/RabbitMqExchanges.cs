namespace Finance.Platform.RabbitMq;

internal sealed record RabbitMqExchanges
{
    internal RabbitMqExchanges(string command, string delivery, string retry, string resume, string dead)
    {
        Command = command;
        Delivery = delivery;
        Retry = retry;
        Resume = resume;
        Dead = dead;
    }
    internal string Command { get; }
    internal string Delivery { get; }
    internal string Retry { get; }
    internal string Resume { get; }
    internal string Dead { get; }
}
