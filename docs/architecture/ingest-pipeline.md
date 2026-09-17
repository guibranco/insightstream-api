---
title: Ingest Pipeline
parent: Architecture
nav_order: 2
---

# 📥 Ingest Pipeline
{: .no_toc }

<details open markdown="block">
  <summary>Table of contents</summary>
  {: .text-delta }
- TOC
{:toc}
</details>

---

## Why asynchronous ingest

Emails never arrive on the API server directly — a cPanel mail server pipes them to a thin PHP
script (`deploy/ingest-relay.php`), which POSTs the raw RFC 822 message to
`POST /api/ingest/email`. Parsing MIME, walking the HTML DOM, and writing several rows
transactionally is too slow to do inline with that request, so the endpoint does the minimum
necessary — auth, dedup, publish — and returns immediately.

## Ingest endpoint

`POST /api/ingest/email`

1. **Auth** — `X-Ingest-Token` header, compared against configuration with
   `CryptographicOperations.FixedTimeEquals` (constant-time; this is machine-to-machine, not JWT)
2. **Rate limit** — 60/hour per token, enforced in Redis (fixed-window counter)
3. **Validate** — `Content-Type` must be `message/rfc822` or `application/octet-stream`; body
   capped at `Ingest:MaxRequestBodyBytes` (default 10 MB), enforced both defensively in the
   controller and via Kestrel's `MaxRequestBodySize`
4. **Dedup** — SHA-256 of the raw bytes; if a `Newsletter` with that hash already exists, respond
   `200 {duplicate:true}` without touching the queue
5. **Publish** — otherwise, publish `{emailHash, rawEmailBase64, retryCount:0}` to the durable
   `newsletter.ingest` queue and respond `202 {duplicate:false}`

## The consumer

`IngestConsumerService` (a `BackgroundService` hosted inside the API process) consumes
`newsletter.ingest` with **prefetch 1** and **manual ack** — one message in flight at a time,
never lost on a crash mid-processing.

For each message:

1. **Parse** (`INewsletterParsingService`, combining three single-purpose pieces):
   - `MimeKitEmailParser` — subject (RFC 2047 decoded automatically), destination address, date,
     and the HTML body (MimeKit picks the right part out of `multipart/alternative`, decoding
     base64/quoted-printable and charset transparently)
   - `AngleSharpLinkExtractor` — walks the DOM for `<a>` tags, skipping unsubscribe/preferences/
     profile/social/footer links (by container class and by URL pattern), reading the title from
     the anchor text or nearest heading ancestor, and the byline from a `"By <name>"` pattern in a
     nearby sibling element
   - `UrlNormalizer` — strips `utm_*`/`source=` query params, unwraps
     `medium.com/m/global-redirect?url=...` links to their real target, drops fragments and
     trailing slashes, then SHA-256-hashes the result
   - Author attribution (Medium `@handle` vs. custom domain) is derived from the **normalized**
     URL, not the raw href — so redirect-wrapped links still attribute correctly
   - Links repeated within the same email (e.g. a "read it again" callout) are de-duplicated by
     normalized-URL hash, keeping the first occurrence's title/author
2. **Persist**, in one transaction:
   - Re-check the email hash (defense against a race between the endpoint's check and this
     consumer run)
   - Insert the `Newsletter` row
   - Upsert each `Link` by `url_hash` — existing links just bump `last_seen`; brand-new links get
     `Status = Awaiting` and an initial score from `IScoringService`
   - Upsert `Author` rows (matched by Medium handle or custom domain) and the `LinkAuthor` /
     `NewsletterLink` join rows, all idempotent
3. **Ack** the delivery

## Retry and dead-lettering

Rather than a multi-queue TTL dead-letter topology, retries are tracked with a plain
`retryCount` field in the message body:

- On failure, if `retryCount < Ingest:MaxRetries` (default 3): republish the same payload to
  `newsletter.ingest` with `retryCount` incremented, then ack the original delivery
- At the retry limit: publish to `newsletter.ingest.dlq` instead, then ack the original

Both queues are declared durable at startup. This keeps the topology to two queues and makes the
retry logic deterministic to unit test — no broker-side TTL timing to reason about.
