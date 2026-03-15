# Finance Core And Gateway Delivery Sequence

This sequence shows the v1 `workspace.requested` path from Telegram ingress to outbound Telegram delivery.

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

## Notes

- The command is acknowledged only after the PostgreSQL transaction commits
- The outbox loop is separate from command processing to preserve transactional integrity
- `finance-core` publishes semantic contracts and does not render Telegram payloads
- `telegram-gateway` renders Bot API payloads and treats outbound delivery as at-least-once
