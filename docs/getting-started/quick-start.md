---
title: Quick Start
parent: Getting Started
nav_order: 3
---

# ⚡ Quick Start
{: .no_toc }

<details open markdown="block">
  <summary>Table of contents</summary>
  {: .text-delta }
- TOC
{:toc}
</details>

Five minutes from a running instance to a scored, prioritized link list — using the bundled
sample fixture, no real Medium email required.

---

## 1. Ingest the sample newsletter

```bash
curl -i -X POST http://localhost:5000/api/ingest/email \
  -H "X-Ingest-Token: dev-only-ingest-token-change-me" \
  -H "Content-Type: message/rfc822" \
  --data-binary @tests/fixtures/sample_medium.eml
```

Expect `202 Accepted` with `{"success":true,"data":{"duplicate":false}}` — the email is now
queued in LavinMQ. Give the hosted consumer a second to parse and persist it (check the
console/log output for `Ingested newsletter ... with 3 link(s)`).

Re-run the exact same command and you'll get `200 OK` with `{"duplicate":true}` — the SHA-256
idempotency check kicked in.

## 2. Log in

```bash
TOKEN=$(curl -s -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"ChangeMe123!"}' | jq -r '.data.token')
```

## 3. See what came in

```bash
curl -s http://localhost:5000/api/stats -H "Authorization: Bearer $TOKEN" | jq
```

```bash
curl -s "http://localhost:5000/api/links?status=Awaiting&sort=priority" \
  -H "Authorization: Bearer $TOKEN" | jq
```

You should see three links extracted from the fixture — one by a Medium `@handle` author, one by
a custom-domain publication, and one that was reached through a Medium redirect link and resolved
to its real target.

## 4. Prioritized queue

```bash
curl -s http://localhost:5000/api/links/prioritized -H "Authorization: Bearer $TOKEN" | jq
```

Awaiting links only, ranked by score — freshly (re)computed if their cached score has expired.

## 5. Like or discard something

```bash
LINK_ID=$(curl -s "http://localhost:5000/api/links?per_page=1" \
  -H "Authorization: Bearer $TOKEN" | jq -r '.data[0].id')

curl -s -X PUT "http://localhost:5000/api/links/$LINK_ID/status" \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{"status":"Liked"}' | jq
```

This nudges the author's and the title's keyword preference weights up
(see [Scoring]({{ site.baseurl }}/architecture/scoring/)) — ingest a few more newsletters from the
same author and watch their links start scoring higher.

---

Next: [API Reference]({{ site.baseurl }}/api-reference/) for the full request/response
contracts, or [Architecture]({{ site.baseurl }}/architecture/) for how the pieces fit together.
