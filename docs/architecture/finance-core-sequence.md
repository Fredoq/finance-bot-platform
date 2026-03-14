# Finance Core Command Sequence

This sequence shows the v1 command path for `workspace.requested`.

```mermaid
sequenceDiagram
    participant G as telegram-gateway
    participant R as RabbitMQ
    participant C as finance-core
    participant P as PostgreSQL
    participant O as Outbox Loop

    G->>R: Publish workspace.requested
    R->>C: Deliver command
    C->>P: Insert inbox row
    C->>P: Upsert user and workspace
    C->>P: Insert outbox row
    C->>P: Mark inbox row processed
    C->>R: Ack delivery
    O->>P: Read unpublished outbox row
    O->>R: Publish workspace.view.requested
    R-->>O: Confirm publish
    O->>P: Mark outbox row published
```

## Notes

- The command is acknowledged only after the PostgreSQL transaction commits
- The outbox loop is separate from command processing to preserve transactional integrity
- Downstream consumers must treat outbound messages as at-least-once deliveries
