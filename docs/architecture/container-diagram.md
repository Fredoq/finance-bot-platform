# C4 Container Diagram

This diagram decomposes the `Finance Platform` into its main deployable containers and supporting data/integration components.

## Scope

- The system of interest is `Finance Platform`.
- `Finance Bot` stays outside the platform boundary and acts as the Telegram-facing client of the platform.
- Internal containers are based on the service model and shared platform components described in the architecture documents.

```mermaid
C4Container
    title Container Diagram for Finance Platform

    Person(finance_user, "Finance User", "Uses the bot to record transactions and review personal finance data.")
    System_Ext(telegram_app, "Telegram App", "Client application used by the user to communicate with the bot.")
    System_Ext(finance_bot, "Finance Bot", "Telegram bot that interacts with the finance platform on behalf of the user.")

    Rel(finance_user, telegram_app, "Uses", "Mobile / desktop app")
    Rel(telegram_app, finance_bot, "Sends commands and receives responses", "Telegram messages")

    System_Boundary(finance_platform, "Finance Platform") {
        Container(bot_gateway, "bot-gateway", "Application service", "Receives bot requests, validates them, deduplicates updates, applies edge rate limits, and publishes downstream commands/events.")
        Container(finance_core, "finance-core", "Application service", "Owns business logic, transactions, categories, reporting queries, and persistence consistency.")
        Container(job_worker, "job-worker", "Application service", "Executes retries, delayed jobs, scheduled summaries, reminders, and async delivery work.")

        ContainerDb(postgres, "PostgreSQL", "Relational database", "System of record for users, transactions, categories, rules, and durable application state.")
        Container(redis, "Redis", "In-memory data store", "Stores transient conversational state, throttling state, and short-lived cache data.")
        ContainerQueue(message_broker, "Message Broker", "Durable async transport", "Buffers commands, events, retries, and decoupled background work between services.")
    }

    Rel(finance_bot, bot_gateway, "Invokes platform entrypoint", "HTTPS / webhook / API")
    Rel(bot_gateway, redis, "Reads and writes transient state", "Rate limits / dedup hints")
    Rel(bot_gateway, message_broker, "Publishes commands and events", "Async messages")

    Rel(finance_core, postgres, "Reads and writes business data", "SQL")
    Rel(finance_core, redis, "Uses short-lived cache/state where justified", "Redis protocol")
    Rel(finance_core, message_broker, "Consumes commands/events and publishes outbox events", "Async messages")

    Rel(job_worker, message_broker, "Consumes scheduled and retry work", "Async messages")
    Rel(job_worker, postgres, "Reads and updates durable state when jobs require it", "SQL")
    Rel(job_worker, redis, "Uses ephemeral coordination state when needed", "Redis protocol")
    Rel(job_worker, finance_bot, "Sends delivery results and scheduled outputs via bot integration", "Outgoing API calls")
```

## Notes

- `bot-gateway`, `finance-core`, and `job-worker` are the three application containers defined by the architecture.
- `PostgreSQL`, `Redis`, and `Message Broker` are shown inside the platform boundary as required runtime containers, even though they are infrastructure components rather than domain services.
- `Finance Bot` is kept outside the platform boundary because it is the Telegram-facing interaction layer, not part of the core platform itself.
