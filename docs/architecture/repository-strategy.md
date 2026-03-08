# Repository Strategy

## Decision

The platform uses:

- one application monorepo
- one separate infrastructure repository

## Application Monorepo

This repository is the application monorepo. It is expected to contain:

- application services
- shared contracts
- shared libraries
- tests
- architecture documentation and ADRs

## Why Monorepo

The services in the current system are tightly related and expected to evolve together. A monorepo is the most practical default because it:

- reduces cross-repository coordination for contract changes
- keeps shared domain and messaging contracts versioned together
- simplifies CI/CD setup at the current scale
- lowers operational overhead while service boundaries are still settling

## Separate Infrastructure Repository

A separate infrastructure repository is expected for delivery concerns. A planned name can be:

- `finance-bot-infra`

That repository should contain:

- infrastructure as code
- environment definitions
- deployment manifests
- network and ingress configuration
- runtime configuration templates
- backup, restore, and operational runbooks

## Why Infra Is Separate

Separating infrastructure from application code helps when:

- deployment concerns must be versioned independently
- environment topology changes without corresponding app changes
- operational access should be limited more tightly than app code access
- platform automation grows faster than product code

## What Does Not Need Its Own Repository

These should stay in the monorepo until there is a strong reason to split them:

- `bot-gateway`
- `finance-core`
- `job-worker`
- shared contracts and shared libraries
- architecture decision records

## Architectural Documentation Placement

Architecture documentation belongs with the application code, not in a standalone architecture repository.

Reason:

- design decisions are most useful when maintained next to the code they govern
- ADRs should evolve with implementation
- a separate architecture-only repository tends to drift

Therefore:

- architecture docs live in `docs/architecture`
- ADRs live in `docs/adr`
- infra topology and delivery mechanics live in the separate infra repository
