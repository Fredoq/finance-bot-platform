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
        Container(bot_gateway, "telegram-gateway", "Application service", "Receives Telegram webhook updates, validates secrets, normalizes supported intents, and publishes application commands.")
        Container(finance_core, "finance-core", "Application service", "Owns business logic, transactions, categories, reporting queries, and persistence consistency.")
        Container(job_worker, "job-worker", "Application service", "Executes retries, delayed jobs, scheduled summaries, reminders, and async delivery work.")

        ContainerDb(postgres, "PostgreSQL", "Relational database", "System of record for users, transactions, categories, rules, and durable application state.")
        Container(redis, "Redis", "In-memory data store", "Stores transient conversational state, throttling state, and short-lived cache data.")
        ContainerQueue(rabbitmq, "RabbitMQ", "Durable async transport", "Buffers application commands, events, retries, and decoupled background work between services.")
    }

    Rel(telegram_platform, bot_gateway, "Delivers updates", "HTTPS / webhook")
    Rel(bot_gateway, rabbitmq, "Publishes application commands", "AMQP 0-9-1")

    Rel(finance_core, postgres, "Reads and writes business data", "SQL")
    Rel(finance_core, redis, "Uses short-lived cache or state where justified", "Redis protocol")
    Rel(finance_core, rabbitmq, "Consumes commands and publishes outbox events", "AMQP 0-9-1")

    Rel(job_worker, rabbitmq, "Consumes delayed and retry work", "AMQP 0-9-1")
    Rel(job_worker, postgres, "Reads and updates durable state when jobs require it", "SQL")
    Rel(job_worker, redis, "Uses ephemeral coordination state when needed", "Redis protocol")
    Rel(job_worker, telegram_platform, "Sends bot messages and delivery work", "Bot API")
```

## Notes

- `telegram-gateway`, `finance-core`, and `job-worker` are the three application containers defined by the architecture
- `PostgreSQL`, `Redis`, and `RabbitMQ` are shown inside the platform boundary as required runtime containers, even though they are infrastructure components rather than domain services
- Telegram is external to the platform, while the Telegram-specific ingress boundary lives inside `telegram-gateway`
