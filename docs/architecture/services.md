# Service Boundaries

## Design Principle

The system should have a small number of services with explicit ownership. The goal is to isolate responsibilities that differ in failure mode, scaling behavior, and security posture, without introducing premature fragmentation.

## `telegram-gateway`

### Responsibility

`telegram-gateway` is the Telegram-facing ingress service of the platform.

### Owns

- Telegram webhook endpoint
- request authentication and validation
- Telegram anti-corruption mapping
- publication of application contracts into RabbitMQ
- initial routing and correlation metadata

### Does Not Own

- finance business rules
- transaction persistence
- reporting logic
- webhook bootstrap or `setWebhook` orchestration
- scheduled jobs

### Key Requirements

- fast webhook acknowledgment
- minimal synchronous logic
- observable request pipeline
- no Telegram-specific contract leakage beyond the service boundary

## `finance-core`

### Responsibility

`finance-core` is the primary business and data ownership service.

### Owns

- users
- transactions
- categories
- global categorization rules
- user-specific categorization overrides
- monthly summaries and reporting queries
- business command handling
- persistence rules and consistency guarantees

### Does Not Own

- Telegram ingress
- long-running background orchestration
- infrastructure provisioning

### Key Requirements

- PostgreSQL as source of truth
- strong transactional behavior where needed
- inbox/outbox patterns for reliable integration boundaries
- explicit domain contracts for commands, events, and queries

## `job-worker`

### Responsibility

`job-worker` executes asynchronous and scheduled processing that should not compete with edge traffic.

### Owns

- reminders
- scheduled summaries
- retry processing
- delayed execution
- outbox delivery processing if delivery is split from core request handling

### Does Not Own

- direct Telegram ingress
- authoritative business data model

### Key Requirements

- safe retries
- dead-letter handling or equivalent failure isolation
- clear job observability
- separate scaling from request-handling services

## Shared Platform Components

These are required platform dependencies but are not treated as domain services in this repository model:

### `PostgreSQL`

- system of record for finance data
- durable storage for transactions and application state that requires strong consistency

### `Redis`

- short-lived conversational state
- request throttling
- short-term caching where justified

### `RabbitMQ`

- asynchronous handoff between gateway, core, and workers
- retry and decoupling boundary
- durable transport for application contracts

## Deployment Topology

The intended topology is:

1. Telegram reaches `telegram-gateway`
2. `telegram-gateway` hands work to RabbitMQ
3. `finance-core` processes application commands
4. `job-worker` executes delayed and scheduled work
5. all services publish logs, metrics, and traces into a shared observability stack

## Future Extraction Policy

Additional services should only be introduced when one of these conditions becomes true:

- a bounded context has independent ownership and release cadence
- scaling characteristics materially diverge
- security isolation requirements justify a separate deployable
- operational complexity is reduced rather than increased

Services such as `notification-service`, `admin-service`, or `reporting-service` are intentionally not created upfront.
