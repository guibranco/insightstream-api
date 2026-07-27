---
title: Configuration
parent: Getting Started
nav_order: 2
---

# ⚙️ Configuration
{: .no_toc }

<details open markdown="block">
  <summary>Table of contents</summary>
  {: .text-delta }
- TOC
{:toc}
</details>

---

Configuration is layered the standard ASP.NET Core way: `appsettings.json` →
`appsettings.{Environment}.json` → environment variables. **Secrets never live in
`appsettings.json`** — only safe local defaults ship in `appsettings.Development.json`; everything
else is supplied via environment variables (see `deploy/deploy.md` for the production `.env`).

Double underscore (`__`) binds to nested sections, e.g. `Jwt__Key` → `Jwt:Key`.

---

## Connection strings

| Key | Description |
| :-- | :---------- |
| `ConnectionStrings__Postgres` | Npgsql connection string |
| `ConnectionStrings__Redis` | StackExchange.Redis connection string |

## JWT (`Jwt` section)

| Key | Default | Description |
| :-- | :------ | :---------- |
| `Jwt__Key` | *(none, required)* | HS256 signing key |
| `Jwt__Issuer` | `InsightStream` | Token issuer |
| `Jwt__Audience` | `InsightStream` | Token audience |
| `Jwt__ExpiryMinutes` | `60` | Token lifetime |

## Ingest (`Ingest` section)

| Key | Default | Description |
| :-- | :------ | :---------- |
| `Ingest__Token` | *(none, required)* | Shared secret for `X-Ingest-Token`, compared in constant time |
| `Ingest__MaxRequestBodyBytes` | `10485760` (10 MB) | Max raw email size |
| `Ingest__RateLimitPerHour` | `60` | Ingest requests allowed per hour per token |
| `Ingest__MaxRetries` | `3` | Retries before a message is routed to the dead-letter queue |

## LavinMQ / RabbitMQ (`RabbitMq` section)

| Key | Default | Description |
| :-- | :------ | :---------- |
| `RabbitMq__HostName` | *(none, required)* | Broker host |
| `RabbitMq__Port` | `5672` | Broker port |
| `RabbitMq__UserName` / `Password` | *(none, required)* | Broker credentials |
| `RabbitMq__VirtualHost` | `/` | Virtual host |
| `RabbitMq__IngestQueue` | `newsletter.ingest` | Main durable queue |
| `RabbitMq__IngestDlq` | `newsletter.ingest.dlq` | Dead-letter queue |

## CORS (`Cors` section)

| Key | Description |
| :-- | :---------- |
| `Cors__AllowedOrigin` | The frontend's **exact** origin (scheme + host, no path/trailing slash) |

## Scoring (`Scoring` section)

All optional — sensible defaults are baked into `ScoringOptions` and mirrored in
`appsettings.json` for visibility. See [Scoring]({{ site.baseurl }}/architecture/scoring/) for
what each weight controls.

| Key | Default |
| :-- | :------ |
| `Scoring__AuthorWeight` | `0.35` |
| `Scoring__KeywordWeight` | `0.25` |
| `Scoring__RecencyWeight` | `0.20` |
| `Scoring__PopularityWeight` | `0.20` |
| `Scoring__RecencyDecayDays` | `30` |
| `Scoring__PopularityCap` | `3` |
| `Scoring__MinKeywordLength` | `4` |
| `Scoring__CacheTtl` | `01:00:00` |
| `Scoring__AuthorLikeAdjustment` | `0.2` |
| `Scoring__AuthorReviewDiscardAdjustment` | `-0.1` |
| `Scoring__AuthorDiscardAdjustment` | `-0.2` |
