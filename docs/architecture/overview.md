# Architecture Overview

## Goal

Build a Telegram personal finance platform that can deliver the MVP without sacrificing the architectural qualities required for long-term operation:

- secure by default
- horizontally and operationally scalable
- resilient to component failure
- easy to extend with new features and integrations

## Product MVP Scope

The initial MVP is a Telegram bot for personal finance tracking with these core capabilities:

- add expense transactions
- add income transactions
- auto-categorize transactions using rules
- show monthly summary
- show category breakdown
- show recent transactions
- delete or recategorize transactions

The architecture must support this MVP without painting the system into a corner.

## Core Architectural Decisions

- The application codebase lives in a single monorepo.
- The system is split into a small number of services with clear ownership.
- `telegram-gateway` is the Telegram-facing webhook boundary inside the platform.
- Telegram-specific request models are normalized inside `telegram-gateway` and do not cross the async boundary.
- Business processing is asynchronous where it improves resilience and delivery guarantees.
- PostgreSQL is the source of truth for business data.
- RabbitMQ is the durable async transport between the edge and business services.
- Reliability patterns such as idempotency and outbox/inbox are part of the baseline design.

## Service Model

The target application layer consists of three services:

1. `telegram-gateway`
2. `finance-core`
3. `job-worker`

Supporting platform components:

- `PostgreSQL`
- `Redis`
- `RabbitMQ`
- `reverse proxy / ingress`
- `observability stack`
- `secret and configuration management`

## High-Level Request Flow

1. Telegram delivers an update to `telegram-gateway` via webhook.
2. `telegram-gateway` authenticates the request and normalizes supported intents.
3. `telegram-gateway` publishes an application command such as `WorkspaceRequestedCommand`.
4. `finance-core` performs business logic and persists state changes.
5. Outgoing messages are emitted through an outbox-driven delivery flow.
6. `job-worker` processes retries, delayed work, reminders, and scheduled reporting.

## Reliability Baseline

The platform is expected to include these properties from the start:

- retry-safe command processing
- durable persistence for business records
- asynchronous boundaries between edge traffic and business logic
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
  job-worker/
libs/
  contracts/
  building-blocks/
tests/
docs/
  architecture/
  adr/
```

Infrastructure delivery is intentionally expected to live in a separate repository, described in the repository strategy document.
