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
- publication of application commands into RabbitMQ
- consumption of semantic delivery contracts from RabbitMQ
- rendering of Telegram responses and Bot API delivery
- initial routing and correlation metadata

### Does Not Own

- finance business rules
- transaction persistence
- reporting logic
- webhook bootstrap or `setWebhook` orchestration
- scheduled jobs outside Telegram delivery

### Key Requirements

- fast webhook acknowledgment
- minimal synchronous logic
- observable request pipeline
- safe outbound retry and dead-letter handling
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
- semantic outbound contracts for user-visible state and actions

### Does Not Own

- Telegram ingress
- Telegram rendering and Bot API payload construction
- long-running background orchestration
- infrastructure provisioning

### Key Requirements

- PostgreSQL as source of truth
- strong transactional behavior where needed
- inbox/outbox patterns for reliable integration boundaries
- explicit domain contracts for commands, events, and queries

## `job-worker`

### Responsibility

`job-worker` is a future extraction target for asynchronous and scheduled processing that should not compete with edge traffic.

### Owns

- reminders
- scheduled summaries
- delayed execution
- batch exports and reporting workloads
- future delivery or retry flows only if they outgrow the gateway runtime

### Does Not Own

- direct Telegram ingress
- Telegram delivery in the current runtime model
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
2. `telegram-gateway` hands ingress commands to RabbitMQ
3. `finance-core` processes application commands and publishes semantic delivery intents
4. `telegram-gateway` consumes delivery intents and calls the Telegram Bot API
5. `job-worker` remains available for future delayed and scheduled work
6. all services publish logs, metrics, and traces into a shared observability stack

## Contract Evolution

- Shared contracts describe application meaning, not Telegram payload shape
- Additive fields may extend an existing contract without renaming it
- Breaking changes require a new contract name or an explicit version suffix
- `telegram-gateway` must support version overlap during contract migration
- Telegram DTO, parse mode, keyboard schema, and Bot API method names must stay inside `telegram-gateway`

## Future Extraction Policy

Additional services should only be introduced when one of these conditions becomes true:

- a bounded context has independent ownership and release cadence
- scaling characteristics materially diverge
- security isolation requirements justify a separate deployable
- operational complexity is reduced rather than increased

Services such as `notification-service`, `admin-service`, or `reporting-service` are intentionally not created upfront.
