---
title: Deployment
nav_order: 5
has_children: true
permalink: /deployment/
---

# 🚢 Deployment

Production target: a single Ubuntu VPS, root access, Nginx reverse proxy, systemd-managed.

---

## The short version

1. Install the ASP.NET Core **runtime** (not the full SDK), PostgreSQL, Redis, and LavinMQ
2. `dotnet publish` the API and copy the output to `/opt/insightstream`
3. Set every secret via `/opt/insightstream/.env` (systemd `EnvironmentFile`) — never in
   `appsettings.json`
4. Run `dotnet ef database update` against the production connection string
5. Install `deploy/insightstream.service` and `deploy/nginx.conf`, then `certbot --nginx`
6. Point the cPanel mail server's pipe at `deploy/ingest-relay.php`

The full, copy-pasteable walkthrough — including exact `apt` commands, the systemd unit, the
Nginx server block, the admin-password rotation procedure, and a curl smoke-test sequence for
every endpoint — lives in [`deploy/deploy.md`](https://github.com/guibranco/insightstream-api/blob/main/deploy/deploy.md)
at the repository root (kept there, next to the artifacts it references, rather than duplicated
here).

---

## Architecture reminder

```text
cPanel mail server ──(PHP relay pipes raw email)──▶ VPS: Nginx ──▶ InsightStream.Api
                                                                        │
                                                          hosts the ingest consumer too
                                                          (single-deployment simplicity)
```

Nginx terminates TLS (via certbot) and enforces `client_max_body_size 10m` to match the API's own
`Ingest:MaxRequestBodyBytes` / Kestrel `MaxRequestBodySize` limit — keep both in sync if you
change it.

## Splitting the ingest worker out later

The consumer runs today as an `IHostedService` inside the API process. If ingest volume ever
justifies scaling it independently, `deploy/deploy.md` §14 covers extracting it into its own
`InsightStream.Worker` project and systemd unit — no database or queue schema changes required,
since both are already shared, decoupled state.
