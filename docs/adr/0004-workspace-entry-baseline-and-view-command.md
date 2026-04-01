# ADR-0004: Workspace entry baseline and view command

## Status

Accepted

## Context

The first business contract handled by `finance-core` is `WorkspaceRequestedCommand`.

The contract does not describe Telegram or `/start`. It only means that a workspace should be opened or restored for a given actor and conversation.

The service also needs a downstream message that allows another service to display the semantic workspace view to the user.

## Decision

`WorkspaceRequestedCommand` is interpreted as an application request to open or restore a workspace.

The initial workspace state machine root is `home`.

Repeated workspace requests do not reset an existing active state in v1.

`finance-core` publishes a new public contract named `WorkspaceViewRequestedCommand` with:

- `WorkspaceIdentity`
- `WorkspaceProfile`
- `State`
- `Actions`
- `IsNewUser`
- `IsNewWorkspace`
- `OccurredUtc`

The outbound routing key is `workspace.view.requested`.

The current v1 home action codes are:

- `account.add`
- `transaction.expense.add`
- `transaction.income.add`

Reporting and history actions such as `summary.month.show`, `category.breakdown.show`, and `transaction.recent.show` remain planned follow-up capabilities rather than current home actions.

## Rationale

- The inbound contract stays transport-agnostic and does not leak Telegram semantics
- The downstream contract stays semantic and does not carry rendered text
- The `home` state is enough to bootstrap the state model without over-designing future flows
- Keeping the current state on repeated requests preserves future draft or long-lived workspace scenarios
- Separate `IsNewUser` and `IsNewWorkspace` flags preserve meaning for future multi-conversation actors

## Consequences

### Positive

- The first workflow supports both onboarding and workspace restore
- Future UI or delivery services can render the same semantic contract differently by channel
- New workspace states can be added without changing the meaning of `WorkspaceRequestedCommand`

### Negative

- The current action set remains intentionally narrower than the full MVP reporting scope
- Additional home actions will require additive contract and rendering updates as reporting features arrive

## Workflow Rules

- New actor and new conversation create both user and workspace
- Existing actor and new conversation create only a new workspace
- Existing actor and existing conversation update profile and entry payload metadata while preserving the current state
- Outbound idempotency for the view command is derived from the inbound idempotency key with the `:workspace-view` suffix

## Follow-Up

Next ADRs should define:

- financial command contracts
- state transitions beyond `home`
- the delivery service that consumes `WorkspaceViewRequestedCommand`
