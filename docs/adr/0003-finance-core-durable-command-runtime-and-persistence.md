# ADR-0003: Finance core durable command runtime and persistence baseline

## Status

Accepted

## Context

`finance-core` is the primary business and data ownership service for the platform.

The service must consume application commands from RabbitMQ, persist authoritative state in PostgreSQL, and publish downstream messages without losing business changes when the process or broker fails.

The first implemented workflow is `WorkspaceRequestedCommand`, but the runtime must remain open for additional contracts.

## Decision

`finance-core` will be implemented as an ASP.NET Core host with four projects:

- `FinanceCore.Api`
- `FinanceCore.Application`
- `FinanceCore.Domain`
- `FinanceCore.Infrastructure`

RabbitMQ consumption uses:

- one durable main queue
- one durable retry queue with fixed delay
- one durable dead queue
- manual acknowledgements after PostgreSQL commit

PostgreSQL remains the source of truth.

Inbound message handling uses an inbox table with a unique `(contract, idempotency_key)` key.

Outbound delivery uses an outbox table and a separate publish loop with broker confirms.

The outbox relay lives inside `finance-core` in v1 and remains part of the service runtime unless an explicit architecture decision changes that boundary.

PostgreSQL schema changes use SQL-first migrations with `DbUp` and a journal table in `finance.schema_journal`.

`finance-core` uses `Npgsql`, versioned SQL scripts, and explicit migration orchestration in v1 instead of EF Core.

## Rationale

- Inbox and outbox give a practical at-least-once baseline across RabbitMQ and PostgreSQL
- Manual acknowledgements after commit prevent message loss on process failure
- Retry and dead-letter queues isolate transient transport or database faults from malformed or unsupported payloads
- SQL-first migrations keep schema changes reviewable while preserving explicit transaction boundaries
- A separate outbox loop keeps delivery reliability independent from command processing latency
- The layered project structure matches the repository model already used by `telegram-gateway`

## Consequences

### Positive

- Duplicate inbound delivery is safe
- Business state changes and outbound intent are committed atomically
- New contracts can be added as application slices without redesigning the runtime
- Health checks can report broker and database readiness independently

### Negative

- Delivery remains at-least-once, so downstream consumers must also be idempotent
- SQL scripts and the migration journal must be maintained deliberately as the schema evolves
- Retry policy is intentionally simple in v1 and may need richer backoff later

## Guardrails

- PostgreSQL is the only authoritative source of user workspace state
- `DbUp` is the only allowed migration runner for `finance-core` in v1
- Until an explicit rollout command is given, all schema corrections must amend `V0001__finance_core_baseline.sql` instead of creating new migration files
- New versioned migration files may be introduced only after the baseline has been intentionally frozen for rollout
- The service never acknowledges an inbound delivery before the inbox row and state changes commit
- Unsupported contracts and malformed payloads are sent to the dead queue without retry
- Transient failures are retried through the retry queue with bounded attempts
- Redis caching remains out of scope for v1, but workspace revisions are stored for future invalidation

## Follow-Up

Next ADRs should define:

- the first financial transaction command set
- the ledger schema increment
