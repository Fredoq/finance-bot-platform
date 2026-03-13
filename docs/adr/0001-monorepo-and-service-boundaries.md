# ADR-0001: Monorepo And Initial Service Boundaries

## Status

Accepted

## Context

The system needs to support an MVP Telegram personal finance bot while also meeting non-functional goals for resilience, extensibility, and security. The architecture must avoid throwaway shortcuts, but also avoid premature fragmentation that would slow delivery.

Two decisions are foundational:

1. how many application services should exist initially
2. how repository boundaries should map to those services

## Decision

The platform will start with three application services:

- `telegram-gateway`
- `finance-core`
- `job-worker`

The application code for these services will live in a single monorepo.

Infrastructure delivery will live in a separate repository.

## Rationale

### Why three services

- `telegram-gateway` isolates external ingress, validation, and request hardening.
- `finance-core` owns business logic and persistence.
- `job-worker` isolates asynchronous and scheduled work from edge request handling.

This split gives meaningful operational separation without over-engineering the system.

### Why not a single deployable

A single deployable would blur edge concerns, business logic, and asynchronous processing. That increases coupling between failure modes and makes it harder to scale or secure the system along natural boundaries.

### Why not repo-per-service

At the current scale, separate repositories would add coordination and release overhead without creating enough independence to justify the cost.

## Consequences

### Positive

- clean separation between ingress, business logic, and background work
- simpler coordination of shared contracts and schema evolution
- architecture decisions remain close to the codebase
- clear path to independent scaling and extraction later

### Negative

- monorepo CI/CD needs discipline as the codebase grows
- service ownership boundaries must be enforced in code review and structure
- some shared code may be overused unless contracts are kept explicit

## Guardrails

The following must be treated as baseline design constraints:

- webhook handling must be idempotent
- business state lives in PostgreSQL
- asynchronous boundaries use durable messaging
- secrets are not stored in source control
- observability is part of the default platform, not a later add-on

## Follow-Up

The next architecture documents should define:

- service APIs and message contracts
- persistence ownership and database boundaries
- deployment topology
- failure handling and retry strategy
