# ADR-0008: Monthly summary read model and Telegram navigation

## Status

Accepted

## Context

The v1 workspace already supports account onboarding, expense and income recording, and recent transaction correction.

Users still need a read-only monthly summary that can be opened from `home` and navigated inside the same Telegram panel without introducing a new delivery contract or a reporting service.

The current persistence model stores transactions in `finance.transaction_entry` with `occurred_utc` timestamps and account currencies, but it does not store user time zones, exchange rates, or a base reporting currency.

## Decision

The v1 monthly summary is implemented as a new workspace state named `summary.month`.

The `home` screen adds the new action code `summary.month.show` when the user has at least one account.

The summary state supports these action codes:

- `summary.month.prev`
- `summary.month.next`
- `summary.month.back`

`summary.month.show` opens the current UTC month derived from the input occurrence time.

The summary period is always a UTC calendar month:

- inclusive lower bound: first day of month at `00:00:00+00`
- exclusive upper bound: first day of next month at `00:00:00+00`

The serialized workspace state adds a new additive section:

- `SummaryData { year, month, currencies }`

The monthly summary aggregates transactions by:

1. currency
2. account inside that currency

Each currency group contains:

- `currency`
- `income`
- `expense`
- `net`
- `accounts`

Each account item contains:

- `id`
- `name`
- `income`
- `expense`
- `net`

The summary does not compute a cross-currency grand total.

Currency groups are sorted by currency code ascending.

Accounts inside a currency group are sorted by account name ascending.

The summary remains read-only in v1.

`WorkspaceViewRequestedCommand` remains the only downstream user-visible contract.

Telegram editable delivery is extended to include `summary.month` in the same best-effort transport-context model already used for `transaction.recent.*`.

## Rationale

- A workspace state keeps the feature inside the current `finance-core -> semantic view -> telegram-gateway` model
- UTC month boundaries are the only deterministic option in v1 because the model lacks user time zones
- Currency grouping avoids incorrect totals because the platform does not have FX conversion or a base reporting currency
- Reusing `WorkspaceViewRequestedCommand` avoids premature contract expansion for a read-only report
- Editable Telegram delivery keeps month navigation inside one chat panel without leaking Telegram details into `finance-core`

## Consequences

### Positive

- users can inspect monthly performance without leaving the current workspace
- no schema migration is required because the summary is computed from existing transactions
- the delivery contract stays additive and transport-agnostic
- the future `category breakdown` feature can reuse the same period-selection model

### Negative

- monthly reporting is UTC-based until a user time zone model exists
- users with multiple currencies see separate totals instead of one combined result
- editable delivery still depends on transient gateway transport context and may fall back to `sendMessage`

## Follow-Up

Next reporting increments may define:

- category breakdown for the selected month
- user-configured time zones
- scheduled monthly summaries in `job-worker`
