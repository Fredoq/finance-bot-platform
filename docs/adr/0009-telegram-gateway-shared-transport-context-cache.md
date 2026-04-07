# ADR-0009: Telegram gateway shared transport context cache

## Status

Accepted

## Context

`telegram-gateway` already supports editable delivery for callback-driven workspace states such as `transaction.recent.*`, `summary.month`, and `category.month`.

The first implementation stores transport context in gateway process memory.

This makes editable delivery fragile across process restarts and unusable across multiple gateway replicas because the message edit metadata is node-local.

## Decision

`telegram-gateway` stores transport context in Redis instead of process memory.

The Redis cache keeps the existing logical keys:

- `envelope:{id}`
- `conversation:{room}`

The existing transport context interface remains unchanged.

The cache keeps the existing fifteen-minute retention window.

Redis failures do not fail the webhook or delivery pipeline:

- write failures are logged and ignored
- read failures are treated as cache misses
- delivery falls back to `sendMessage` when no editable context is available

Redis is treated as a best-effort optimization for editable delivery rather than a readiness dependency.

## Consequences

### Positive

- editable delivery survives gateway restarts
- editable delivery works across multiple gateway replicas
- business and contract boundaries remain unchanged

### Negative

- gateway runtime now depends on Redis connectivity for the edit optimization path
- Redis payload compatibility becomes part of gateway rollout safety
