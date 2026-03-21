# finance-bot-platform

Monorepo for the Telegram personal finance platform: `telegram-gateway`, `finance-core`, shared contracts, tests, architecture documentation, and Aspire-based local orchestration.

## Purpose

This repository contains the application code for a Telegram-based personal finance system. The current runtime model uses `telegram-gateway` for Telegram ingress and delivery, `finance-core` for business processing and persistence, and shared contracts for asynchronous integration. `job-worker` remains a future extraction target for scheduled and batch workloads.

## Local development

Set the AppHost secrets once:

```bash
dotnet user-secrets --project aspire/FinanceBot.AppHost set "Parameters:telegram-bot-token" "<bot-token>"
dotnet user-secrets --project aspire/FinanceBot.AppHost set "Parameters:telegram-webhook-secret" "<webhook-secret>"
dotnet user-secrets --project aspire/FinanceBot.AppHost set "Parameters:telegram-key-secret" "<opaque-key-secret>"
```

Run the local stack:

```bash
dotnet run --project aspire/FinanceBot.AppHost
```

Expose `telegram-gateway` through a tunnel and point Telegram to `https://<public-url>/telegram/webhook`.

## Architecture docs

- [Architecture overview](docs/architecture/overview.md)
- [C4 context diagram](docs/architecture/context-diagram.md)
- [C4 container diagram](docs/architecture/container-diagram.md)
- [Service boundaries](docs/architecture/services.md)
- [Delivery ADR](docs/adr/0005-telegram-gateway-outbound-delivery-and-reversible-opaque-keys.md)
- [Repository strategy](docs/architecture/repository-strategy.md)
- [ADR-0001: Monorepo and service boundaries](docs/adr/0001-monorepo-and-service-boundaries.md)
- [ADR-0002: Telegram gateway boundary and entry contracts](docs/adr/0002-telegram-gateway-telegram-boundary-and-entry-contracts.md)
