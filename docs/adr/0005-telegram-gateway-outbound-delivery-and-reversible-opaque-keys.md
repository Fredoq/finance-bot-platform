# ADR-0005: Telegram gateway outbound delivery and reversible opaque keys

## Status

Accepted

## Context

`finance-core` already publishes semantic outbound contracts through its transactional outbox.

The platform now needs a concrete delivery runtime that can consume those contracts, render Telegram responses, and call the Bot API without introducing a separate service too early.

The existing opaque actor and conversation keys are one-way hashes, which means `telegram-gateway` cannot recover the Telegram chat identifier from `WorkspaceIdentity` for outbound delivery.

## Decision

Outbound Telegram delivery stays inside `telegram-gateway` in v1 as a dedicated background service.

`finance-core` continues to publish only semantic outbound contracts such as `WorkspaceViewRequestedCommand`.

The RabbitMQ topology is split into:

- `finance.command` for ingress commands
- `finance.delivery` for semantic outbound delivery contracts

`telegram-gateway` consumes delivery contracts with manual acknowledgements, retry queues, and a dead queue.

Unsupported contracts, invalid payloads, and permanent Telegram failures move to the dead queue.

Transient transport failures and Telegram throttling move to the retry queue until the delivery attempt budget is exhausted.

Opaque actor and conversation keys become reversible opaque tokens so that `telegram-gateway` can recover Telegram identifiers without a separate durable mapping store.

Shared contracts remain semantic and transport-agnostic.

Telegram DTO, Bot API payload shape, keyboard layout, and render logic stay inside `telegram-gateway`.

## Rationale

- Delivery belongs to the Telegram boundary, not to the business core
- Keeping delivery in `telegram-gateway` avoids a premature third runtime service
- A separate delivery exchange keeps ingress and outbound concerns isolated
- Reversible opaque keys remove the need for a gateway database only to map conversation identifiers
- Semantic contracts keep future channel expansion and renderer evolution open

## Consequences

### Positive

- `finance-core` remains free of Telegram-specific rendering logic
- `telegram-gateway` becomes the single place that understands Telegram ingress and delivery
- Delivery reliability is explicit through retry and dead-letter queues
- Future outbound contracts can be added as new semantic slices without changing `finance-core` boundaries

### Negative

- `telegram-gateway` now owns two RabbitMQ roles instead of only ingress publication
- User-visible delivery remains at-least-once, so duplicate Telegram messages are still possible in failure scenarios
- Opaque token format must remain stable after rollout because persisted workspace identities depend on it

## Contract Evolution

- Shared outbound contracts describe user-visible meaning, not Telegram formatting
- Additive fields may extend an existing contract without renaming it
- Breaking changes require a new contract name or an explicit version suffix
- `telegram-gateway` must support version overlap during contract migration

## Follow-Up

Next ADRs should define:

- callback query contracts and delivery behavior
- edit and delete delivery operations
- when scheduled or batch workloads justify extracting `job-worker`
