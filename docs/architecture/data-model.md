---
title: Data Model
parent: Architecture
nav_order: 1
---

# 🗄️ Data Model
{: .no_toc }

<details open markdown="block">
  <summary>Table of contents</summary>
  {: .text-delta }
- TOC
{:toc}
</details>

---

PostgreSQL via EF Core 10, snake_case columns/tables (`EFCore.NamingConventions`), UUID v7
primary keys (`Guid.CreateVersion7()` — time-ordered, index-friendly).

## Entities

| Table | Purpose |
| :---- | :------ |
| `newsletters` | One row per ingested email; `email_hash` (SHA-256, unique) drives idempotency |
| `links` | One row per distinct article URL; `url_hash` (SHA-256 of the *normalized* URL, unique) drives dedup |
| `authors` | One row per attributed author; unique on `medium_handle` and `custom_domain` (both nullable) |
| `newsletter_links` | Join: which newsletters a link appeared in — "sent in N different newsletters" |
| `link_authors` | Join: which authors are attributed to a link |
| `users` | Single-admin auth; bcrypt `password_hash` |
| `user_preferences` | Learned author/keyword weights, clamped to `[-1.00, 1.00]` via a check constraint |

## Key constraints

- `links.status` — string-mapped enum: `Awaiting`, `Liked`, `Discarded`, `DiscardedAfterReview`
- `links.priority_score` — `numeric(5,2)`, default `5.00`
- Indexes on `links.status` and `links.priority_score DESC` (the "prioritized" query's exact
  access pattern)
- `user_preferences.weight` — `numeric(3,2)`, `CHECK (weight >= -1.00 AND weight <= 1.00)`
- `UNIQUE(user_id, preference_type, preference_value)` on `user_preferences` — one weight per
  (user, type, value) tuple, upserted in place
- `newsletter_links` / `link_authors` — composite primary keys (no surrogate id), cascade delete
  from either side

## Seed data

The initial migration seeds one admin user: `admin` / `ChangeMe123!`. There is no self-service
password-change endpoint by design — see
[Deployment §10]({{ site.baseurl }}/deployment/#10-rotating-the-seeded-admin-password) for the
rotation procedure.
