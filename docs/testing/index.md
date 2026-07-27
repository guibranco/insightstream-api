---
title: Testing
nav_order: 6
permalink: /testing/
---

# 🧪 Testing

Two test projects, split by what they need to run.

---

## `InsightStream.UnitTests`

No external services required — fast, deterministic.

| Area | What's covered |
| :--- | :-------------- |
| MIME parsing | `MimeKitEmailParser` against `tests/fixtures/sample_medium.eml` — RFC 2047 subject decoding, multipart/alternative selection, base64 body decoding, destination address, date |
| HTML link extraction | `AngleSharpLinkExtractor` — skips unsubscribe/preferences/profile/social/footer links, reads titles from anchor text or heading ancestors, reads bylines from nearby `"By <name>"` text |
| URL normalization | `UrlNormalizer` — strips `utm_*`/`source=`, unwraps Medium redirect links, drops fragments/trailing slashes, produces stable SHA-256 hashes for equivalent URL variants |
| End-to-end parsing | `NewsletterParsingService` against the real fixture — intra-email dedup, author attribution (handle vs. custom domain) |
| Ingest idempotency | `NewsletterIngestProcessor` — re-processing an already-seen `EmailHash` is a no-op; new links get an initial score; existing links just bump `LastSeen` without rescoring |
| Scoring math | `ScoringService` — bounds (`0..10`), monotonicity (liked-author-history > discarded, newer > older, more popular > less popular, matching keyword preference > none) rather than pinned magic numbers |
| Preference clamping | `PreferenceLearningService` — correct deltas per status, half-weight for keywords, clamping at `±1.00`, no-op on reverting to `Awaiting` |

Run:

```bash
dotnet test tests/InsightStream.UnitTests
```

Test doubles (`FakeNewsletterParsingService`, `FakeScoringService`, `FakeScoreCache`, etc.) stand
in for real MimeKit/AngleSharp/Redis dependencies where a test's focus is elsewhere; EF Core's
`InMemory` provider backs `InsightStreamDbContext` for anything that needs a `DbContext` but not
Postgres-specific behavior (check constraints, `numeric` precision, etc.).

---

## `InsightStream.IntegrationTests`

Requires **Docker** (Testcontainers spins up a real `postgres:16-alpine` container per test run).

`IntegrationTestFactory` boots the actual `InsightStream.Api` `Program` via
`WebApplicationFactory<Program>`, points `ConnectionStrings:Postgres` at the ephemeral container,
runs `dotnet ef database update` equivalent (`Database.MigrateAsync()`) against it, and swaps the
Redis-backed `IScoreCache`/`IRateLimiter` for in-memory fakes (and drops the ingest
`IHostedService`) — these tests exercise the **links API against a real relational database**,
not the messaging/caching infrastructure, which unit tests already cover.

Covers: JWT login (including the seeded admin credentials), 401 on missing auth, 422 on invalid
`status`/`sort` query values, link detail retrieval and 404s, status updates (200/400), and
paginated/filtered/searched list queries against real Postgres — catching anything an in-memory
provider might paper over (`ILIKE`, check constraints, actual SQL translation).

Run:

```bash
dotnet test tests/InsightStream.IntegrationTests
```

---

## Everything

```bash
dotnet test
```
