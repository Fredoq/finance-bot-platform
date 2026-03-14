namespace FinanceCore.Infrastructure.Persistence.Postgres.Outbox;

internal sealed record OutboxItem(Guid MessageId, string Contract, string RoutingKey, string Source, string CorrelationId, string CausationId, byte[] Payload, DateTimeOffset OccurredUtc, int Attempt);
