# ADR-0006: First account onboarding and account model

## Status

Accepted

## Context

The current workspace baseline opens a generic `home` screen with placeholder actions.

This is a poor fit for a new user because the first useful action is to add an account and its current balance.

The platform also needs an initial account model that is simple enough for v1 while remaining compatible with future transaction flows.

## Decision

The first-run workspace experience is a guided account onboarding flow.

The v1 workspace states are:

- `home`
- `account.name`
- `account.currency`
- `account.balance`
- `account.confirm`

The v1 account model is:

- one user can own many accounts
- each account has a name
- each account has a required ISO-style currency code
- each account stores both `opening_amount` and `current_amount`
- `current_amount` may become negative
- account type is intentionally omitted in v1

The current balance is collected during onboarding and saved directly on the account row.

Expense and income flows project directly into `current_amount`.

The v1 balance model allows negative balances after transaction application.

Insufficient funds validation and overdraft protection are intentionally out of scope for now.

The initial `home` screen shows only `account.add` when the user has no accounts.

Workspace user input is carried through a new application contract named `WorkspaceInputRequestedCommand` with `Kind` values `action` and `text`.

`WorkspaceViewRequestedCommand` includes serialized `StateData` so that `telegram-gateway` can render state-specific screens without reading business persistence.

## Consequences

### Positive

- new users reach the first meaningful outcome immediately
- the account model is explicit in PostgreSQL instead of being hidden in workspace state
- the bot can render multi-step onboarding while keeping Telegram concerns inside `telegram-gateway`
- future transaction flows can apply balance mutations without adding account-type-specific rules in v1

### Negative

- `/start` now resets the current workspace to the account-aware `home` screen instead of preserving the previous state
- the delivery contract becomes larger because it now carries `StateData`
- negative balances can exist until explicit overdraft rules are introduced
