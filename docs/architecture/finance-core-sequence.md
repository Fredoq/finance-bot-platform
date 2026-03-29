# Finance Core And Gateway Delivery Sequence

These sequences show the current v1 workspace paths from Telegram ingress to outbound Telegram delivery.

## Workspace Open Or Restore

```mermaid
sequenceDiagram
    participant T as Telegram Platform
    participant G as telegram-gateway
    participant R as RabbitMQ
    participant C as finance-core
    participant P as PostgreSQL
    participant O as Outbox Loop
    participant D as Gateway Delivery Loop

    T->>G: Deliver /start webhook
    G->>R: Publish workspace.requested
    R->>C: Deliver command
    C->>P: Insert inbox row
    C->>P: Upsert user and workspace
    C->>P: Insert outbox row
    C->>P: Mark inbox row processed
    C->>R: Ack delivery
    O->>P: Read unpublished outbox row
    O->>R: Publish workspace.view.requested to finance.delivery
    R-->>O: Confirm publish
    O->>P: Mark outbox row published
    R->>D: Deliver workspace.view.requested
    D->>T: Send rendered workspace message
    D->>R: Ack delivery
```

## Add Income Transaction

```mermaid
sequenceDiagram
    participant U as Finance User
    participant T as Telegram Platform
    participant G as telegram-gateway
    participant R as RabbitMQ
    participant C as finance-core
    participant P as PostgreSQL
    participant O as Outbox Loop
    participant D as Gateway Delivery Loop

    U->>T: Tap "Add income" and send amount/category
    T->>G: Deliver callback or text update
    G->>R: Publish workspace.input.requested
    R->>C: Deliver command
    C->>P: Insert inbox row
    C->>P: Load workspace, accounts, and category choices
    C->>P: Update workspace state or persist income transaction
    C->>P: Update account current_amount
    C->>P: Insert outbox row
    C->>P: Mark inbox row processed
    C->>R: Ack delivery
    O->>P: Read unpublished outbox row
    O->>R: Publish workspace.view.requested to finance.delivery
    R-->>O: Confirm publish
    O->>P: Mark outbox row published
    R->>D: Deliver workspace.view.requested
    D->>T: Send rendered workspace message
    D->>R: Ack delivery
```

## Notes

- Both `workspace.requested` and `workspace.input.requested` are acknowledged only after the PostgreSQL transaction commits
- The outbox loop is separate from command processing to preserve transactional integrity
- `finance-core` publishes semantic contracts and does not render Telegram payloads
- `telegram-gateway` renders Bot API payloads and treats outbound delivery as at-least-once
- Income and expense flows reuse the same durable command, workspace, and outbox pattern
