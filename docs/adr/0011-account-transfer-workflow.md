# ADR-0011: Account Transfer Workflow

## Status

Accepted

## Context

The workspace already supports multiple accounts, income transactions, expense transactions, recent transaction correction, monthly summary, category breakdown, and user time zones.

Users need to move money between their own accounts without recording false income or expense.

The platform does not yet have an exchange-rate model, a base reporting currency, or correction behavior for non-transaction ledger entries.

## Decision

The v1 transfer workflow supports transfers only between accounts with the same currency.

The workflow is implemented as workspace states:

- `transfer.source.account`
- `transfer.target.account`
- `transfer.amount`
- `transfer.confirm`

The home screen exposes `transfer.add` only when a user has at least two accounts.

The target account choices include only accounts with the same currency as the source account and exclude the source account itself.

The transfer has no free-text description in v1.

Transfers are stored in a dedicated `finance.account_transfer` table instead of `finance.transaction_entry`.

Saving a transfer inserts the transfer row, subtracts the amount from the source account, and adds the amount to the target account in one PostgreSQL transaction.

Transfers do not appear in recent transactions, monthly income and expense summary, or category breakdown in v1.

Transfer deletion and editing are out of scope for v1.

## Consequences

### Positive

- Users can move money between accounts without distorting income, expense, or category reports.
- The v1 model avoids premature foreign exchange semantics.
- The workflow stays inside the existing semantic workspace view contract.

### Negative

- Cross-currency transfers require a later explicit exchange-rate decision.
- Recent transaction history does not show transfers yet.
- Incorrect transfers require manual database correction until a transfer correction flow exists.
