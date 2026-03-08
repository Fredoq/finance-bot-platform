# Service Boundaries

## Design Principle

The system should have a small number of services with explicit ownership. The goal is to isolate responsibilities that differ in failure mode, scaling behavior, and security posture, without introducing premature fragmentation.

## `bot-gateway`

### Responsibility

`bot-gateway` is the external edge component for Telegram traffic.

### Owns

- Telegram webhook endpoint
- request authentication and validation
- deduplication of incoming updates
- initial routing metadata
- rate limiting and abuse protection at the bot boundary

### Does Not Own

- finance business rules
- transaction persistence
- reporting logic
- scheduled jobs

### Key Requirements

- fast webhook acknowledgment
- safe handling of duplicate updates
- minimal synchronous logic
- observable request pipeline

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

### `message broker`

- asynchronous handoff between gateway, core, and workers
- retry and decoupling boundary

The exact broker can be chosen later, but the architecture assumes a durable async transport exists.

## Deployment Topology

The intended topology is:

1. external ingress reaches `bot-gateway`
2. `bot-gateway` hands work to the async boundary
3. `finance-core` processes business operations
4. `job-worker` executes delayed and scheduled work
5. all services publish logs, metrics, and traces into a shared observability stack

## Future Extraction Policy

Additional services should only be introduced when one of these conditions becomes true:

- a bounded context has independent ownership and release cadence
- scaling characteristics materially diverge
- security isolation requirements justify a separate deployable
- operational complexity is reduced rather than increased

Services such as `notification-service`, `admin-service`, or `reporting-service` are intentionally not created upfront.
