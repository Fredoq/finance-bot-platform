namespace Finance.Platform.RabbitMq;

internal sealed record RabbitMqQueues
{
    internal RabbitMqQueues(string live, string retry, string dead, int retryDelaySeconds, string liveRouting, string deadRouting)
    {
        Live = live;
        Retry = retry;
        Dead = dead;
        RetryDelaySeconds = retryDelaySeconds;
        LiveRouting = liveRouting;
        DeadRouting = deadRouting;
    }
    internal string Live { get; }
    internal string Retry { get; }
    internal string Dead { get; }
    internal int RetryDelaySeconds { get; }
    internal string LiveRouting { get; }
    internal string DeadRouting { get; }
}
