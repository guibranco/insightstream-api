---
title: Installation
parent: Getting Started
nav_order: 1
---

# 📦 Installation
{: .no_toc }

<details open markdown="block">
  <summary>Table of contents</summary>
  {: .text-delta }
- TOC
{:toc}
</details>

---

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download) — pinned to `10.0.302` via `global.json`
- PostgreSQL 14+, Redis, and LavinMQ (or plain RabbitMQ — wire-compatible) reachable from your
  machine
- Docker Desktop, only if you want to run the Testcontainers integration test suite

---

## 1. Clone the repository

```bash
git clone git@github.com:guibranco/insightstream-api.git
cd insightstream-api
```

## 2. Restore dependencies

```bash
dotnet restore
```

## 3. Start Postgres, Redis, and LavinMQ locally

Any install method works — native packages, Homebrew, or containers. For a quick throwaway
stack:

```bash
docker run -d --name insightstream-postgres -p 5432:5432 \
  -e POSTGRES_DB=insightstream -e POSTGRES_USER=postgres -e POSTGRES_PASSWORD=postgres \
  postgres:16-alpine

docker run -d --name insightstream-redis -p 6379:6379 redis:7-alpine

docker run -d --name insightstream-lavinmq -p 5672:5672 -p 15672:15672 cloudamqp/lavinmq:latest
```

`appsettings.Development.json` already points at `localhost` for all three with matching
credentials, so no further configuration is needed for local development.

## 4. Apply database migrations

```bash
dotnet ef database update \
  --project src/InsightStream.Infrastructure \
  --startup-project src/InsightStream.Api
```

{: .warning }
This seeds a single admin user: `admin` / `ChangeMe123!`. There is no self-service
"change password" endpoint by design — see the
[Deployment guide]({{ site.baseurl }}/deployment/) for the rotation procedure before you expose
this anywhere public.

## 5. Run

```bash
dotnet run --project src/InsightStream.Api
```

The API starts on `http://localhost:5000` by default (or whatever `ASPNETCORE_URLS` you set).
The ingest consumer runs alongside it automatically as a hosted background service — no separate
process needed for local development.

## 6. Verify

```bash
curl http://localhost:5000/health
```

```
GET /health  →  200 OK  {"success":true,"data":{"status":"Healthy", ...}}
```

---

## Building from source (Release)

```bash
dotnet restore
dotnet build --configuration Release
dotnet publish src/InsightStream.Api -c Release -o ./publish
```

See [Deployment]({{ site.baseurl }}/deployment/) for shipping the published output to a
production VPS.
