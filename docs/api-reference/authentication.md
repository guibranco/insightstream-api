---
title: Authentication
parent: API Reference
nav_order: 1
---

# 🔑 Authentication
{: .no_toc }

<details open markdown="block">
  <summary>Table of contents</summary>
  {: .text-delta }
- TOC
{:toc}
</details>

---

InsightStream has two, deliberately separate, authentication mechanisms.

## JWT Bearer (humans / the frontend)

Every endpoint except `/api/auth/login` and `/api/ingest/email` requires:

```
Authorization: Bearer <token>
```

### `POST /api/auth/login`

```bash
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"ChangeMe123!"}'
```

```json
{
  "success": true,
  "data": {
    "token": "eyJhbGciOi...",
    "user": { "id": "...", "username": "admin", "lastLogin": "2026-07-25T18:00:00Z" }
  }
}
```

- HS256, signed with `Jwt:Key`, expires after `Jwt:ExpiryMinutes` (default 60)
- Rate-limited to **5 attempts / 15 minutes per client IP** (Redis-backed), counted regardless of
  whether the attempt succeeds
- Wrong username or password → `401 {"success":false,"message":"Invalid username or password."}`
- No self-service registration or password-change endpoint — single-admin system, seeded via
  migration (see [Data Model]({{ site.baseurl }}/architecture/data-model/))

## Ingest token (machine-to-machine)

```
X-Ingest-Token: <shared secret>
```

Compared against `Ingest:Token` using `CryptographicOperations.FixedTimeEquals` — not a JWT, not
tied to a user, just a shared secret between the cPanel PHP relay and this API. See
[Ingest API]({{ site.baseurl }}/api-reference/ingest-api/).
