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
- Telegram webhook handling is separated from core business processing.
- Business processing is asynchronous where it improves resilience and delivery guarantees.
- PostgreSQL is the source of truth for business data.
- Redis and a broker are used for transient state, rate limiting, and asynchronous communication.
- Reliability patterns such as idempotency and outbox/inbox are part of the baseline design.

## Service Model

The target application layer consists of three services:

1. `bot-gateway`
2. `finance-core`
3. `job-worker`

Supporting platform components:

- `PostgreSQL`
- `Redis`
- `message broker`
- `reverse proxy / ingress`
- `observability stack`
- `secret and configuration management`

## High-Level Request Flow

1. Telegram sends an update to `bot-gateway` via webhook.
2. `bot-gateway` authenticates and validates the request.
3. `bot-gateway` deduplicates the incoming update using Telegram update metadata.
4. `bot-gateway` publishes a command or event for downstream processing.
5. `finance-core` performs business logic and persists state changes.
6. Outgoing messages are emitted through an outbox-driven delivery flow.
7. `job-worker` processes background work such as retries, reminders, and scheduled reporting.

## Reliability Baseline

The platform is expected to include these properties from the start:

- idempotent update handling
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
  bot-gateway/
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
