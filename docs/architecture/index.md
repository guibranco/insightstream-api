---
title: Architecture
nav_order: 3
has_children: true
permalink: /architecture/
---

# 🏗️ Architecture

How InsightStream is put together, from mail relay to scored REST API.

---

## In this section

| Page | What you'll learn |
| :--- | :---------------- |
| [Data Model]({{ site.baseurl }}/architecture/data-model/) | Entities, relationships, constraints |
| [Ingest Pipeline]({{ site.baseurl }}/architecture/ingest-pipeline/) | Email → queue → parse → persist, retries and dead-lettering |
| [Scoring]({{ site.baseurl }}/architecture/scoring/) | The 0–10 scoring formula and preference learning |

---

## Solution layout

```text
InsightStream.sln
src/
  InsightStream.Core/            entities, enums, DTOs, options, service interfaces
  InsightStream.Infrastructure/   EF Core + migrations, Redis, LavinMQ, parsers, scoring
  InsightStream.Api/              controllers, middleware, ingest hosted worker, Program.cs
tests/
  InsightStream.UnitTests/
  InsightStream.IntegrationTests/
  fixtures/sample_medium.eml
```

`InsightStream.Core` has no dependency on `Infrastructure` or `Api` — it only defines entities,
DTOs, configuration option classes, and the service interfaces (`IScoringService`,
`ISearchService`, `IEmailParser`, …) that `Infrastructure` implements. `InsightStream.Api` wires
everything together in `Program.cs` and hosts the ingest consumer as a background service
alongside the REST API, for single-deployment simplicity (see
[Splitting the worker out later](/deployment/#14-splitting-the-worker-out-later)).

---

## Why no repository layer, no MediatR

EF Core's `DbContext` **is** the repository/unit-of-work abstraction — wrapping it in another
layer of repository interfaces would just add indirection without adding testability (it's
already swappable via `DbContextOptions`, and integration tests exercise it against a real
Postgres via Testcontainers). Likewise, a single-purpose API with a handful of endpoints doesn't
need a mediator pipeline — controllers call services directly.
