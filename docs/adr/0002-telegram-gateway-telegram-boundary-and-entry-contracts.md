# ADR-0002: Telegram gateway boundary and entry contracts

## Status

Accepted

## Context

The first concrete implementation step is `telegram-gateway`. Two questions must be fixed before implementation:

1. whether Telegram-specific request models are allowed to cross the async boundary
2. what the first downstream command should represent for `/start`

## Decision

`telegram-gateway` is the Telegram-facing ingress boundary inside the platform.

Telegram request models stay inside `telegram-gateway` and are normalized before publication.

The first downstream command is `WorkspaceRequestedCommand`.

The command is application-oriented and does not mention Telegram.

`WorkspaceRequestedCommand` is emitted for both first-time and repeated `/start` interactions.

`telegram-gateway` generates opaque actor and conversation keys for the command payload.

The message envelope carries a deterministic idempotency key derived from the inbound update id.

`telegram-gateway` accepts any Telegram `Update` but only maps supported intents in local slices.

Webhook bootstrap and `allowed_updates` orchestration stay outside the service for now.

## Rationale

- The async boundary should carry application meaning, not transport-specific payloads
- `finance-core` should not depend on Telegram concepts
- `/start` means open or restore the user workspace regardless of whether the user is new or returning
- opaque keys keep downstream contracts stable while preserving deterministic identity mapping
- local slice growth inside `telegram-gateway` allows support for new Telegram update types without changing the boundary model

## Consequences

### Positive

- Telegram-specific code is isolated to one service
- downstream services stay transport-agnostic
- the first workflow is compatible with both onboarding and re-entry
- idempotency and duplicate handling can be implemented downstream without losing ingress context

### Negative

- downstream services store opaque edge keys instead of familiar transport ids
- debugging raw ingress sometimes requires checking `telegram-gateway` logs rather than only message payloads
- webhook bootstrap still requires separate operational handling until a dedicated provisioning decision is made
