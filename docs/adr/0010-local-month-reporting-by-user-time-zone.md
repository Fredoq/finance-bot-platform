# ADR-0010: Local month reporting by user time zone

## Status

Accepted

## Context

`summary.month` and `category.month` already expose monthly finance read models in the Telegram workspace.

The first implementation grouped transactions by UTC calendar month.

This makes month boundaries incorrect for users outside UTC because transactions around midnight UTC can appear in the wrong local month.

## Decision

`finance-core` stores a user time zone in `finance.user_account.time_zone`.

The default value for new and existing users is `Etc/UTC`.

Monthly summary and category breakdown resolve the selected month in the user time zone and translate the local month boundaries to UTC before querying PostgreSQL.

The workspace state data for report screens includes the active time zone label so Telegram rendering can show which local calendar is being used.

`WorkspaceProfile` remains unchanged because time zone is persisted as finance domain data rather than transport profile data.

## Consequences

### Positive

- month boundaries match the user's local calendar
- report navigation uses the same local calendar semantics as aggregation
- existing users keep stable behavior until they choose a different time zone

### Negative

- reporting logic now depends on valid time zone handling
- invalid stored time zones must be normalized to the UTC default
