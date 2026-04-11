# finance-bot-platform

Monorepo for the Telegram personal finance platform: `telegram-gateway`, `finance-core`, shared contracts, tests, architecture documentation, and Aspire-based local orchestration.

## Purpose

This repository contains the application code for a Telegram-based personal finance system. The current runtime model uses `telegram-gateway` for Telegram ingress and delivery, `finance-core` for business processing and persistence, and shared contracts for asynchronous integration. The workspace flow covers account onboarding, income and expense tracking, recent transaction corrections, monthly reports, and user-configurable time zone settings for local-month reporting.

## Dev runtime

This workflow is for local development only. It is not a production or staging deployment model.

Set the AppHost secrets once:

```bash
dotnet user-secrets --project aspire/FinanceBot.AppHost set "Parameters:telegram-bot-token" "<bot-token>"
dotnet user-secrets --project aspire/FinanceBot.AppHost set "Parameters:telegram-webhook-secret" "<webhook-secret>"
dotnet user-secrets --project aspire/FinanceBot.AppHost set "Parameters:telegram-key-secret" "<opaque-key-secret>"
```

Start the local Aspire stack:

Ensure the Aspire CLI is installed and available in your `PATH`.

```bash
aspire run --project aspire/FinanceBot.AppHost --non-interactive
```

The AppHost starts `PostgreSQL`, `RabbitMQ`, `Redis`, `finance-core`, and `telegram-gateway`. `telegram-gateway` is exposed locally on `http://127.0.0.1:8082`.

Expose the webhook endpoint through a temporary HTTPS tunnel for development:

```bash
npx --yes localtunnel --port 8082
```

Use the returned public URL to configure the Telegram webhook:

```bash
BOT=$(dotnet user-secrets --project aspire/FinanceBot.AppHost list | awk -F' = ' '/Parameters:telegram-bot-token/ {print $2}')
HOOK=$(dotnet user-secrets --project aspire/FinanceBot.AppHost list | awk -F' = ' '/Parameters:telegram-webhook-secret/ {print $2}')
curl -X POST "https://api.telegram.org/bot${BOT}/setWebhook" \
  -d "url=https://<public-url>/telegram/webhook" \
  -d "secret_token=${HOOK}" \
  -d "drop_pending_updates=true"
```

Check the webhook state:

```bash
curl "https://api.telegram.org/bot${BOT}/getWebhookInfo"
```

Delete the webhook when the dev session ends:

```bash
curl -X POST "https://api.telegram.org/bot${BOT}/deleteWebhook" \
  -d "drop_pending_updates=true"
```

## Architecture docs

- [Architecture overview](docs/architecture/overview.md)
- [C4 context diagram](docs/architecture/context-diagram.md)
- [C4 container diagram](docs/architecture/container-diagram.md)
- [Service boundaries](docs/architecture/services.md)
- [Delivery ADR](docs/adr/0005-telegram-gateway-outbound-delivery-and-reversible-opaque-keys.md)
- [Recent transactions ADR](docs/adr/0007-recent-transactions-corrective-flow-and-editable-delivery.md)
- [Local month reporting ADR](docs/adr/0010-local-month-reporting-by-user-time-zone.md)
- [Account transfer ADR](docs/adr/0011-account-transfer-workflow.md)
- [Shared transport context cache ADR](docs/adr/0009-telegram-gateway-shared-transport-context-cache.md)
- [Repository strategy](docs/architecture/repository-strategy.md)
- [ADR-0001: Monorepo and service boundaries](docs/adr/0001-monorepo-and-service-boundaries.md)
- [ADR-0002: Telegram gateway boundary and entry contracts](docs/adr/0002-telegram-gateway-telegram-boundary-and-entry-contracts.md)
