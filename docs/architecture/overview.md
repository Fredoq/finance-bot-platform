# Architecture Overview

## Goal

Build a Telegram personal finance platform that can deliver the MVP without sacrificing the architectural qualities required for long-term operation:

- secure by default
- horizontally and operationally scalable
- resilient to component failure
- easy to extend with new features and integrations

## Product MVP Scope

The initial MVP is a Telegram bot for personal finance tracking with these core capabilities:

- guide a new user through adding the first account and its current balance
- add expense transactions
- add income transactions
- auto-categorize transactions using rules
- show monthly summary
- show category breakdown
- show recent transactions
- delete or recategorize transactions
- configure the user time zone for local-month reporting

The architecture must support this MVP without painting the system into a corner.

## Core Architectural Decisions

- The application codebase lives in a single monorepo.
- The system is split into a small number of services with clear ownership.
- `telegram-gateway` is the Telegram-facing boundary inside the platform for both webhook ingress and outbound Bot API delivery.
- Telegram-specific request models are normalized inside `telegram-gateway` and do not cross the async boundary.
- `finance-core` publishes semantic presentation intents and does not render Telegram payloads.
- Business processing is asynchronous where it improves resilience and delivery guarantees.
- PostgreSQL is the source of truth for business data.
- RabbitMQ is the durable async transport between the edge and business services.
- Redis remains available for short-lived conversational state, throttling data, and cache entries where low-latency ephemeral storage is justified.
- Reliability patterns such as idempotency and outbox/inbox are part of the baseline design.

## Service Model

The current runtime model uses two active services:

1. `telegram-gateway`
2. `finance-core`

Supporting platform components:

- `PostgreSQL`
- `Redis`
- `RabbitMQ`
- `reverse proxy / ingress`
- `observability stack`
- `secret and configuration management`

## High-Level Request Flow

1. Telegram delivers an update to `telegram-gateway` via webhook.
2. `telegram-gateway` authenticates the request, normalizes supported intents, and publishes an application command such as `WorkspaceRequestedCommand`.
3. `finance-core` performs business logic, persists state changes, and stores semantic outbound intents in its outbox.
4. `finance-core` publishes user-visible contracts such as `WorkspaceViewRequestedCommand` to the delivery exchange.
5. `telegram-gateway` consumes delivery contracts, renders the Telegram response, and sends it through the Bot API.

## Reliability Baseline

The platform is expected to include these properties from the start:

- retry-safe command processing
- durable persistence for business records
- asynchronous boundaries between edge traffic and business logic
- at-least-once user-visible delivery through RabbitMQ retry and dead-letter queues
- health and readiness checks
- structured logs and traceable correlation ids
- backup and restore procedures for PostgreSQL

## Security Baseline

The platform is expected to include these controls from the start:

- webhook secret validation
- least-privilege service credentials
- secrets delivered through environment or a secret manager, never committed
- encrypted transport for external ingress
- auditability for user-visible operations
- validation and sanitization of user input

## Planned Repository Layout

Target structure inside this monorepo:

```text
services/
  telegram-gateway/
  finance-core/
libs/
  contracts/
  building-blocks/
tests/
docs/
  architecture/
  adr/
```

Infrastructure delivery is intentionally expected to live in a separate repository, described in the repository strategy document.
