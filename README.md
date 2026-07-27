# 📬 InsightStream

**InsightStream** ingests your Medium daily-digest newsletters straight from your inbox, extracts
every article link, tracks the authors behind them, learns what you actually like, and serves it
all up through a clean REST API — so you can stop drowning in digest emails and start reading a
prioritized, personalized feed instead.

[![.NET](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16-4169E1?logo=postgresql&logoColor=white)](https://www.postgresql.org/)
[![Redis](https://img.shields.io/badge/Redis-cache-DC382D?logo=redis&logoColor=white)](https://redis.io/)
[![LavinMQ](https://img.shields.io/badge/LavinMQ-AMQP%200--9--1-FF6600)](https://lavinmq.com/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

[![Build & Deploy](https://github.com/guibranco/insightstream-api/actions/workflows/deploy.yml/badge.svg)](https://github.com/guibranco/insightstream-api/actions/workflows/deploy.yml)
[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=guibranco_insightstream-api&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=guibranco_insightstream-api)
[![Coverage](https://sonarcloud.io/api/project_badges/measure?project=guibranco_insightstream-api&metric=coverage)](https://sonarcloud.io/summary/new_code?id=guibranco_insightstream-api)

📖 **Full documentation:** see [`/docs`](docs/) or the published site once GitHub Pages is
enabled for this repo.

---

## ✨ What it does

- 📥 **Ingests** raw Medium digest emails via a simple HTTP endpoint (fed by a cPanel → PHP relay)
- 🔁 **Queues** everything through LavinMQ so parsing never blocks the ingest response
- 🔗 **Extracts** article links from the newsletter HTML, skipping unsubscribe/social/footer noise
- ✍️ **Tracks authors** by Medium `@handle` or custom domain, with per-author interaction history
- 🎯 **Scores** every link 0–10 from author history, keyword preference, recency, and popularity
- 🧠 **Learns** your preferences automatically every time you like/discard a link
- 🔐 **Serves** a JWT-protected REST API, CORS-locked to your frontend's exact origin

---

## 🏗️ Architecture

```text
 cPanel mail server                 InsightStream VPS (Nginx + systemd)
┌───────────────────┐   raw email   ┌─────────────────────────────────────────┐
│  Thin PHP relay    │ ──POST───────▶│  POST /api/ingest/email                 │
│  (pipes STDIN)     │               │   • validates token, dedups by SHA-256  │
└───────────────────┘               │   • publishes to LavinMQ, returns 202   │
                                     │                                          │
                                     │  newsletter.ingest (durable queue) ──┐  │
                                     │                                       │  │
                                     │  IngestConsumerService (hosted)  ◀────┘  │
                                     │   • MimeKit parses MIME/charset/QP      │
                                     │   • AngleSharp extracts links + authors │
                                     │   • normalizes URLs, dedups, scores     │
                                     │   • persists via EF Core (PostgreSQL)   │
                                     │                                          │
                                     │  REST API (JWT-protected)               │
                                     │   /api/links /api/newsletters /api/...  │
                                     └───────────────────┬──────────────────────┘
                                                          │ CORS: exact origin only
                                                          ▼
                                          Static frontend on GitHub Pages
```

3 retries then dead-letter: failed ingest messages are retried up to
`Ingest:MaxRetries` (default 3) before landing in `newsletter.ingest.dlq` for manual inspection.

---

## 🧱 Tech stack

| Layer | Technology |
| :---- | :--------- |
| Runtime | .NET 10 / ASP.NET Core 10 (Controllers) |
| Database | PostgreSQL via EF Core 10 + Npgsql, snake_case naming |
| Cache / rate limiting | Redis via StackExchange.Redis |
| Messaging | LavinMQ (AMQP 0-9-1) via RabbitMQ.Client |
| Email parsing | MimeKit |
| HTML parsing | AngleSharp |
| Auth | JWT Bearer (HS256) + BCrypt.Net-Next |
| Logging | Serilog (console + rolling file) |
| Tests | xUnit, FluentAssertions, Testcontainers (PostgreSQL) |

---

## 📂 Project structure

```text
InsightStream.sln
src/
  InsightStream.Core/            entities, enums, DTOs, options, service interfaces
  InsightStream.Infrastructure/   EF Core + migrations, Redis, LavinMQ, MimeKit/AngleSharp parsers, scoring
  InsightStream.Api/              controllers, middleware, ingest hosted worker, Program.cs
tests/
  InsightStream.UnitTests/
  InsightStream.IntegrationTests/  (Testcontainers PostgreSQL + WebApplicationFactory)
  fixtures/sample_medium.eml
deploy/                           nginx.conf, systemd unit, ingest-relay.php, deploy.md
docs/                             Just-the-Docs site (published via GitHub Actions)
```

---

## 🚀 Getting started (local development)

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download) (see `global.json` — pinned to `10.0.302`)
- PostgreSQL, Redis, and LavinMQ (or plain RabbitMQ, which is wire-compatible) running locally
- Docker Desktop, if you want to run the integration test suite (Testcontainers)

### 1. Clone and restore

```bash
git clone git@github.com:guibranco/insightstream-api.git
cd insightstream-api
dotnet restore
```

### 2. Configure

Edit `src/InsightStream.Api/appsettings.Development.json` (already pre-filled with sane
`localhost` defaults) or override via environment variables — see
[Configuration](#-configuration) below.

### 3. Apply database migrations

```bash
dotnet ef database update \
  --project src/InsightStream.Infrastructure \
  --startup-project src/InsightStream.Api
```

This seeds one admin user: `admin` / `ChangeMe123!` — **change it** (see `deploy/deploy.md`
§10 for the rotation procedure; there's no self-service endpoint by design).

### 4. Run

```bash
dotnet run --project src/InsightStream.Api
```

The API starts on `http://localhost:5000` (or whatever `ASPNETCORE_URLS` you set) and the ingest
consumer starts alongside it as a hosted background service — no separate process needed.

### 5. Try it

```bash
curl http://localhost:5000/health

curl -X POST http://localhost:5000/api/ingest/email \
  -H "X-Ingest-Token: dev-only-ingest-token-change-me" \
  -H "Content-Type: message/rfc822" \
  --data-binary @tests/fixtures/sample_medium.eml
```

---

## 🔐 Configuration

All secrets are supplied via **environment variables** in production (never committed);
`appsettings.Development.json` only holds safe local defaults. Double-underscore (`__`) binds to
nested config sections.

| Variable | Purpose |
| :------- | :------ |
| `ConnectionStrings__Postgres` | Npgsql connection string |
| `ConnectionStrings__Redis` | StackExchange.Redis connection string |
| `Jwt__Key` / `Jwt__Issuer` / `Jwt__Audience` / `Jwt__ExpiryMinutes` | JWT signing (HS256) |
| `Ingest__Token` | Shared secret for `X-Ingest-Token` (constant-time compared) |
| `Ingest__MaxRequestBodyBytes` / `RateLimitPerHour` / `MaxRetries` | Ingest tuning |
| `RabbitMq__HostName/Port/UserName/Password/VirtualHost` | LavinMQ connection |
| `RabbitMq__IngestQueue` / `IngestDlq` | Queue names |
| `Cors__AllowedOrigin` | Exact frontend origin (scheme + host, no path) allowed by CORS |

See `deploy/deploy.md` §7 for a full production `.env` example and secret-generation commands.

---

## 🧪 Testing

```bash
# Fast unit tests: MIME/HTML parsing, URL normalization, scoring math, preference clamping,
# ingest idempotency — no external services required.
dotnet test tests/InsightStream.UnitTests

# Integration tests: real Postgres via Testcontainers + WebApplicationFactory against the
# actual API pipeline. Requires Docker Desktop (or another Docker engine) running.
dotnet test tests/InsightStream.IntegrationTests

# Everything
dotnet test
```

---

## 🔄 CI/CD

| Workflow | Trigger | Does |
| :------- | :------ | :--- |
| [`build.yml`](.github/workflows/build.yml) | Every pull request | Build, run the full test suite with coverage, submit results to SonarCloud |
| [`deploy.yml`](.github/workflows/deploy.yml) | Push to `main` (or manual dispatch) | Build, test + SonarCloud, compute the next version with GitVersion, publish a GitHub Release |
| [`pages.yml`](.github/workflows/pages.yml) | Changes under `docs/` on `main` | Build and publish the Jekyll docs site to GitHub Pages |

`deploy.yml` doesn't yet ship anything to the production VM — see the placeholder comment at the
bottom of that file for what a future publish step will look like; `deploy/deploy.md` documents
the manual procedure it will eventually automate.

Requires a `SONAR_TOKEN` repository secret (SonarCloud → Account → Security → generate token).

---

## 📡 API overview

All responses share one JSON envelope: `{ success, data?, message?, pagination? }`.
JWT Bearer auth is required everywhere except `/api/auth/login` and `/api/ingest/email`
(machine-to-machine, token header instead).

| Method & path | Purpose |
| :------------ | :------ |
| `POST /api/ingest/email` | Ingest a raw RFC 822 email (machine-to-machine, `X-Ingest-Token`) |
| `POST /api/auth/login` | Log in, get a JWT |
| `GET /api/stats` | Status counts, totals, recent newsletters |
| `GET /api/links` | Paginated, filterable, searchable, sortable link list |
| `GET /api/links/prioritized` | Awaiting links, priority-ranked, lazily rescored |
| `GET /api/links/{id}` | Link detail — author(s) + newsletters it appeared in |
| `PUT /api/links/{id}/status` | Update status (`Awaiting`/`Liked`/`Discarded`/`DiscardedAfterReview`) — triggers preference learning |
| `GET /api/newsletters` / `/{id}` | Newsletter list / detail with its links |
| `GET /api/authors` / `/{id}` | Author list / detail with links + per-status interaction counts |
| `GET /health` | PostgreSQL + Redis + LavinMQ connectivity |

Full request/response shapes, status codes, and validation rules: see
[`docs/api-reference/`](docs/api-reference/).

---

## 📦 Deployment

The target environment is a single Ubuntu VPS behind Nginx, managed by systemd — see
[`deploy/deploy.md`](deploy/deploy.md) for the complete walkthrough: installing the .NET runtime,
PostgreSQL, Redis, and LavinMQ; systemd unit and Nginx config (`deploy/insightstream.service`,
`deploy/nginx.conf`); the cPanel → PHP ingest relay (`deploy/ingest-relay.php`); Let's Encrypt via
certbot; and a full curl smoke-test sequence.

---

## 📖 Docs site

Deeper architecture notes, the data model, the scoring formula, and the full API reference live
under [`/docs`](docs/) and publish to GitHub Pages via
[`.github/workflows/pages.yml`](.github/workflows/pages.yml) on every push to `main`.

---

## 📄 License

[MIT](LICENSE) © Guilherme Branco Stracini
