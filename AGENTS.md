## General
* **Language**: use only english language inside code, C# 14.
* **Stack**: .NET 10, ASP.NET Core 10.

* The `README.md` file must explain the purpose of the repository.
* The `README.md` file must be free of typos, grammar mistakes, and broken English.
* The `README.md` file must be as short as possible and must not duplicate code documentation.

Project Context

This repository contains the application monorepo for a Telegram-based personal finance platform. It hosts `bot-gateway`, `finance-core`, `job-worker`, shared contracts, shared libraries, tests, and architecture documentation.

Language and Tone

Respond in English using .NET 10 / C# 14 technical terminology.
Tone: professional, constructive, and concise.

Response Structure

TL;DR - 1-2 lines.
Details - explanations and links when needed.
Steps - a step-by-step algorithm or a short checklist when it helps solve the task.

Development (.NET 10 / C# 14)

Use idiomatic C# 14 and .NET 10 features when they genuinely simplify the code.
Provide code examples in `csharp` blocks with fully compilable methods, classes, or tests.
Prefer `file-scoped namespaces`, `required`, `collection expressions`, `await foreach`, and `primary constructors` where appropriate.
New code must stay aligned with the architectural decisions and documents in `docs/architecture` and `docs/adr`.

Style Guide C#

Use `file-scoped namespaces`, `required`, and `collection expressions`.

Code Design

Every class or record must have a supplementary docblock preceding it.
A class or record docblock must explain the purpose of the class or record and provide usage examples.
Every method and function must have a supplementary docblock preceding it.
Docblocks must be written in English only, using UTF-8 encoding.

Method bodies may not contain blank lines.
Method and function bodies may not contain comments.
Variable names must be single nouns, never compound or composite.
Method names must be single verbs, never compound or composite.
Error and log messages should not end with a period.
Error and log messages must always be a single sentence, with no periods inside.
Favor the fail fast paradigm over fail safe by throwing exceptions early.

The DDD paradigm must be respected.
Elegant Objects design principles must be respected.

Every class or record may have only one primary constructor; any secondary constructor must delegate to it.

Methods must be declared in interfaces and then implemented in classes or records.
Public methods that do not implement an interface should be avoided.
Methods must never return null.
Null may not be passed as an argument.
Type introspection and type casting are strictly prohibited.
Reflection on object internals is strictly prohibited.

Class or record names may not end with the `-er` or `-or` suffix.
Class or record names should be based on what they are, not what they do.
Examples of bad names: `Manager`, `Controller`, `Helper`, `Handler`, `Writer`, `Reader`, `Converter`, `Validator`, `Router`, `Dispatcher`, `Observer`, `Listener`, `Sorter`, `Encoder`, `Decoder`.
Exceptions include names such as `User` and `Computer`.

Constructor bodies may not contain any code except assignment statements.
Constructors must only create the object without processing the input data in their body.
Data processing must occur on demand by calling object methods.

Encapsulate as few fields as possible in classes or records.
Encapsulate no more than four fields.

Name methods thoughtfully:
Methods that return something in response should be named with nouns.
Methods that manipulate state should be named with verbs and should not return anything in response.

Make classes or records immutable.
Limit classes or records to a maximum of five public methods.

Never return null.
Use one of these options instead:
Throw an exception for fast failure.
Return a collection of objects for empty-result scenarios.
Use the Null Object pattern when it fits the design.

Classes or records must be sealed by default.
Prefer composition over inheritance for classes or records.
Do not use static methods to implement business logic in classes or records.

Errors and Problem Analysis

When analyzing problems, provide a minimal reproducible example, the root cause, and a way to verify the fix.
If tests are proposed, they must validate behavior rather than internal implementation details.

Tests

Tests should be short, focused, and readable.
Every test must assert at least once.
Each test should verify one behavioral scenario of the object under test.
Test files should map clearly to the feature, class, or behavior they cover.
Test names must be full English sentences that describe the observed behavior.
Tests should avoid hidden shared state between cases.
Prefer explicit setup in the test body; shared fixtures are acceptable when they improve clarity and do not hide important context.
Prefer fake objects and stubs over mocks when practical, especially in domain-level tests.
Tests should validate behavior rather than internal implementation details.
Tests should not spend effort on trivial getters and setters unless that behavior is part of a real contract.
Tests must close resources they use, such as file handlers, sockets, and database connections.
Tests should prepare a clean state before execution instead of relying on cleanup after failure.
Use irregular inputs, non-ASCII strings, and randomized values when they improve coverage and remain deterministic enough to diagnose failures.
Tests should store temporary files in temporary directories, not in the codebase directory.
Tests should not print log messages.
The testing framework should disable or minimize logging from the objects under test.
Tests must not wait indefinitely for any event and must always use bounded timeouts.
Tests should assume the absence of an Internet connection unless a test explicitly targets network integration.
Tests must not rely on default configurations of the objects they test when important behavior depends on configuration.
Use ephemeral TCP ports generated with appropriate library functions when network ports are required.
Inline small fixtures when that improves readability.
Create large fixtures at runtime instead of storing them in files when practical.
Supplementary fixture objects are acceptable when they reduce duplication without obscuring intent.
Verify concurrent or multi-threaded behavior when the code under test is expected to be safe under concurrency.
Avoid flaky tests; if retries are used for unstable external conditions, the reason must be explicit and bounded.
Avoid asserting on error message text when a stronger contract such as exception type, structured payload, or error code is available.

DevOps / Infrastructure

Use Docker and Compose v2 for containerization.
For long-running services, expect `healthcheck`, `restart: unless-stopped`, and reasonable resource limits by default.
For .NET configuration, consider `DOTNET_*` environment variables and memory limits when the task involves runtime settings.

External Link Format

Short description + URL.
Do not include long retellings of external articles.

OSS Processes

Default license: MIT, unless stated otherwise.
Versioning: SemVer.
Changelog: Keep a Changelog.
Commits: Conventional Commits.
Branches: `main` is protected; working branches are `feat/*`, `fix/*`, `docs/*`.
PRs: CI, review, and a clear change description are required.

Architecture Artifacts

Use Mermaid for diagrams.
Use C4 style for architecture diagrams when appropriate.
Store diagrams and architecture notes close to the code in `docs/architecture`.

Handling Incomplete Input

Ask no more than 1-2 clarifying questions, and only when proceeding without them would be risky.
If a safe and reasonable assumption can be made, state it explicitly and continue.
