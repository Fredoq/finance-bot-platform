# C4 Container Diagram

This diagram decomposes the `Finance Platform` into its main deployable containers and supporting data and integration components.

## Scope

- The system of interest is `Finance Platform`
- `telegram-gateway` is the Telegram-facing ingress boundary inside the platform
- Internal containers are based on the service model and shared platform components described in the architecture documents

```mermaid
C4Container
    title Container Diagram for Finance Platform

    Person(finance_user, "Finance User", "Uses the bot to record transactions and review personal finance data.")
    System_Ext(telegram_app, "Telegram App", "Client application used by the user to interact with the bot.")
    System_Ext(telegram_platform, "Telegram Platform", "Telegram cloud that exchanges webhook updates and bot messages through the Bot API.")

    Rel(finance_user, telegram_app, "Uses", "Mobile / desktop app")
    Rel(telegram_app, telegram_platform, "Exchanges bot messages", "Telegram protocol")

    System_Boundary(finance_platform, "Finance Platform") {
        Container(bot_gateway, "telegram-gateway", "Application service", "Receives Telegram webhook updates, validates secrets, normalizes supported intents, consumes delivery contracts, and sends Bot API responses.")
        Container(finance_core, "finance-core", "Application service", "Owns business logic, transactions, categories, reporting queries, and persistence consistency.")
        Container(job_worker, "job-worker", "Future application service", "Reserved extraction target for scheduled summaries, reminders, batch workloads, and other future background processing.")

        ContainerDb(postgres, "PostgreSQL", "Relational database", "System of record for users, transactions, categories, rules, and durable application state.")
        Container(redis, "Redis", "In-memory data store", "Stores transient conversational state, throttling state, and short-lived cache data.")
        ContainerQueue(rabbitmq, "RabbitMQ", "Durable async transport", "Buffers ingress commands, semantic delivery contracts, retries, and future background work between services.")
    }

    Rel(telegram_platform, bot_gateway, "Delivers updates", "HTTPS / webhook")
    Rel(bot_gateway, rabbitmq, "Publishes application commands and consumes delivery contracts", "AMQP 0-9-1")
    Rel(bot_gateway, telegram_platform, "Sends bot messages", "Bot API")

    Rel(finance_core, postgres, "Reads and writes business data", "SQL")
    Rel(finance_core, redis, "Uses short-lived cache or state where justified", "Redis protocol")
    Rel(finance_core, rabbitmq, "Consumes commands and publishes semantic delivery intents", "AMQP 0-9-1")

    Rel(job_worker, rabbitmq, "Consumes future delayed and scheduled work", "AMQP 0-9-1")
    Rel(job_worker, postgres, "Reads and updates durable state when jobs require it", "SQL")
    Rel(job_worker, redis, "Uses ephemeral coordination state when needed", "Redis protocol")
```

## Notes

- `telegram-gateway` and `finance-core` are the active runtime containers in v1
- `job-worker` remains in the model as a future extraction target instead of a current delivery participant
- `PostgreSQL`, `Redis`, and `RabbitMQ` are shown inside the platform boundary as required runtime containers, even though they are infrastructure components rather than domain services
- Telegram is external to the platform, while the Telegram-specific ingress boundary lives inside `telegram-gateway`
