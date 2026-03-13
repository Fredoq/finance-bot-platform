# finance-bot-platform

Monorepo for the Telegram personal finance platform: telegram gateway, finance core, background workers, shared contracts, and platform documentation.

## Purpose

This repository is the application monorepo for a secure, scalable Telegram personal finance system. It is intended to host the user-facing bot entrypoint, core domain services, background workers, shared libraries, tests, and architecture decision records.

## Architecture docs

- [Architecture overview](docs/architecture/overview.md)
- [C4 context diagram](docs/architecture/context-diagram.md)
- [C4 container diagram](docs/architecture/container-diagram.md)
- [Service boundaries](docs/architecture/services.md)
- [Repository strategy](docs/architecture/repository-strategy.md)
- [ADR-0001: Monorepo and service boundaries](docs/adr/0001-monorepo-and-service-boundaries.md)
- [ADR-0002: Telegram gateway boundary and entry contracts](docs/adr/0002-telegram-gateway-telegram-boundary-and-entry-contracts.md)
