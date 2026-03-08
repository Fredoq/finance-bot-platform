# C4 Context Diagram

This diagram shows the system at the highest level of abstraction: the finance platform, the end user, and the Telegram-facing bot layer through which the user interacts with the platform.

## Context

- The platform is a Telegram personal finance system.
- Internal services such as `bot-gateway`, `finance-core`, and `job-worker` are intentionally hidden at this level.
- The goal of this view is to show the real user interaction chain: user -> Telegram app -> finance bot -> finance platform.

```mermaid
C4Context
    title System Context for Finance Platform

    Person(finance_user, "Finance User", "Uses the product to record transactions, review summaries, and manage personal finances.")

    System(finance_platform, "Finance Platform", "Core personal finance platform where transactions, categorization, summaries, and reporting are processed.")

    System_Ext(telegram_app, "Telegram App", "Client application used by the user to communicate with the bot.")
    System_Ext(finance_bot, "Finance Bot", "Telegram bot interface that receives user commands and exchanges data with the finance platform.")

    Rel(finance_user, telegram_app, "Uses", "Mobile / desktop app")
    Rel(telegram_app, finance_bot, "Sends commands and receives responses", "Telegram messages")
    Rel(finance_bot, finance_platform, "Invokes core finance operations", "HTTPS / webhook / API")
    Rel(finance_platform, finance_bot, "Returns results, reports, and confirmations", "API responses / outgoing messages")
```

## Notes

- `Finance Platform` is the system of interest on this diagram.
- `Finance Bot` is shown separately because it is the user-facing bot layer through which Telegram interactions reach the platform.
- Application services are not shown here because they belong to the next C4 level: Container.
