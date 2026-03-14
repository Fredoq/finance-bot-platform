# C4 Context Diagram

This diagram shows the finance platform at the highest level of abstraction together with the end user and the Telegram systems through which the user interacts with the platform.

## Context

- The platform is a Telegram personal finance system
- Internal services such as `telegram-gateway`, `finance-core`, and `job-worker` are intentionally hidden at this level
- The Telegram-facing ingress boundary is part of the finance platform, not an external service

```mermaid
C4Context
    title System Context for Finance Platform

    Person(finance_user, "Finance User", "Uses the product to record transactions, review summaries, and manage personal finances.")

    System(finance_platform, "Finance Platform", "Core personal finance platform where Telegram updates are normalized, business commands are processed, and reports are produced.")

    System_Ext(telegram_app, "Telegram App", "Client application used by the user to communicate with the bot.")
    System_Ext(telegram_platform, "Telegram Platform", "Telegram cloud that exchanges bot messages and webhook updates via the Bot API.")

    Rel(finance_user, telegram_app, "Uses", "Mobile / desktop app")
    Rel(telegram_app, telegram_platform, "Exchanges bot messages", "Telegram protocol")
    Rel(telegram_platform, finance_platform, "Delivers webhook updates and bot responses", "HTTPS / Bot API")
```

## Notes

- `Finance Platform` is the system of interest on this diagram
- Telegram is shown as the external system through which users and the platform exchange bot traffic
- Application services are not shown here because they belong to the next C4 level: Container
