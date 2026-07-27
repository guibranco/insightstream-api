---
title: 📬 InsightStream
layout: home
nav_order: 1
---

# 📬 InsightStream
{: .fs-9 }

Ingests Medium newsletter emails, extracts and scores article links, tracks authors, and learns
your preferences.
{: .fs-6 .fw-300 }

[Get started now]({{ site.baseurl }}/getting-started/){: .btn .btn-primary .fs-5 .mb-4 .mb-md-0 .mr-2 }
[View on GitHub](https://github.com/guibranco/insightstream-api){: .btn .fs-5 .mb-4 .mb-md-0 }

---

## What is InsightStream?

Medium's daily digest emails are great content, terrible UX — a firehose of links with no way to
prioritize, no memory of what you've already read, and no sense of what you actually like.
**InsightStream** fixes that:

- 📥 **Ingests** raw newsletter emails via a simple, token-authenticated HTTP endpoint
- 🔗 **Extracts** every article link, skipping unsubscribe/social/footer noise
- ✍️ **Tracks authors** across newsletters, by Medium handle or custom publishing domain
- 🎯 **Scores** every link 0–10 from author history, keyword preference, recency, and popularity
- 🧠 **Learns** continuously: every like/discard nudges future scores
- 🔐 **Serves** it all through a JWT-protected REST API for a static frontend

---

## Quick overview

```text
┌────────────────────────────────────────────────────────┐
│  cPanel mail server → thin PHP relay → POST raw email   │
└───────────────────────────┬──────────────────────────────┘
                             │ validated, deduplicated (SHA-256)
                             ▼
                    LavinMQ "newsletter.ingest"
                             │ durable, 3 retries → DLQ
                             ▼
              IngestConsumerService (hosted worker)
        MimeKit → AngleSharp → normalize → score → persist
                             │
                             ▼
                    PostgreSQL (EF Core)
                             │
                             ▼
              REST API (JWT) ── CORS: exact origin only
                             │
                             ▼
                 Static frontend (GitHub Pages)
```

---

## Feature highlights

| 🏷️ Feature | 📝 Description |
| :--------- | :------------- |
| **Idempotent ingest** | SHA-256 of the raw email dedupes at the door — re-delivery is a no-op |
| **Async pipeline** | Ingest returns 202 immediately; LavinMQ decouples parsing from the HTTP request |
| **Robust MIME/HTML parsing** | MimeKit (multipart/alternative, base64/QP, charsets) + AngleSharp |
| **URL normalization** | Strips `utm_*`/`source=`, unwraps Medium redirect links, drops fragments/trailing slashes |
| **Deterministic scoring** | Author history + keyword preference + recency decay + popularity, all configurable |
| **Preference learning** | Every status change nudges author/keyword weights, clamped to [-1, 1] |
| **Retry + dead-letter** | 3 automatic retries, then the poison message lands in `newsletter.ingest.dlq` |
| **Tested** | Unit tests for parsing/scoring/idempotency + Testcontainers integration tests |

---

## Technology stack

| Layer | Technology |
| :---- | :--------- |
| Runtime | .NET 10 / ASP.NET Core 10 (Controllers) |
| Database | PostgreSQL via Entity Framework Core 10 + Npgsql |
| Cache / rate limiting | Redis via StackExchange.Redis |
| Messaging | LavinMQ (AMQP 0-9-1) via RabbitMQ.Client |
| Email parsing | MimeKit |
| HTML parsing | AngleSharp |
| Auth | JWT Bearer (HS256) + BCrypt.Net-Next |
| Logging | Serilog (console + rolling file) |
| Tests | xUnit, FluentAssertions, Testcontainers (PostgreSQL) |
| Docs | Jekyll + Just the Docs (this site) |

---

## Getting started

Head to [Installation]({{ site.baseurl }}/getting-started/installation/) to run InsightStream
locally in a few minutes, or read [Architecture]({{ site.baseurl }}/architecture/) to understand
how the ingest pipeline and scoring engine fit together.
