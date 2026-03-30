# ADR-0007: Recent transactions corrective flow and editable delivery

## Status

Accepted

## Context

The v1 workspace already supports account onboarding, expense creation, and income creation.

Users still need a way to inspect the last recorded transactions, delete incorrect entries, and recategorize them without leaving the Telegram workspace.

The platform also needs a Telegram-friendly way to keep recent transaction navigation inside a single chat panel instead of creating a new bot message for every callback-driven step.

## Decision

The v1 corrective flow adds these workspace states:

- `transaction.recent.list`
- `transaction.recent.detail`
- `transaction.recent.delete.confirm`
- `transaction.recent.category`
- `transaction.recent.recategorize.confirm`

The `home` screen adds a new action code `transaction.recent.show` when the user has at least one account.

Recent transactions are shown as a paged list of the newest entries across both `income` and `expense` kinds.

Recent transaction correction supports:

- hard delete
- recategorize to an existing category
- recategorize from free text with the existing category upsert rules

Delete removes the `transaction_entry` row and compensates the account `current_amount` in the same PostgreSQL transaction.

Recategorize changes only `category_id`.

`WorkspaceViewRequestedCommand` remains the only user-visible outbound contract.

Telegram-specific editable delivery stays inside `telegram-gateway`.

`telegram-gateway` stores a short-lived transport context keyed by the inbound input envelope id and by the current conversation key so that a later `workspace.view.requested` delivery can choose `editMessageText` for recent workspace states.

If the transient context is missing, delivery falls back to `sendMessage`.

## Consequences

### Positive

- users can verify and correct the latest bot actions without leaving the chat
- recent correction stays inside the existing transport-agnostic workspace contract model
- editable Telegram delivery improves chat cleanliness without leaking Telegram ids into `finance-core`

### Negative

- `telegram-gateway` now owns a short-lived transport context in addition to rendering logic
- delete remains destructive in v1 because the baseline schema does not include a soft-delete or audit model
- recent correction adds more workspace states and dynamic action resolution rules
