---
title: Ingest API
parent: API Reference
nav_order: 2
---

# 📥 Ingest API
{: .no_toc }

<details open markdown="block">
  <summary>Table of contents</summary>
  {: .text-delta }
- TOC
{:toc}
</details>

---

## `POST /api/ingest/email`

Machine-to-machine only — see [Authentication]({{ site.baseurl }}/api-reference/authentication/)
for the `X-Ingest-Token` header. Not JWT-protected.

**Request**

| | |
| :--- | :--- |
| Headers | `X-Ingest-Token: <token>`, `Content-Type: message/rfc822` (or `application/octet-stream`) |
| Body | Raw RFC 822 email bytes, up to `Ingest:MaxRequestBodyBytes` (default 10 MB) |

```bash
curl -X POST http://localhost:5000/api/ingest/email \
  -H "X-Ingest-Token: your-ingest-token" \
  -H "Content-Type: message/rfc822" \
  --data-binary @newsletter.eml
```

**Responses**

| Status | Body | Meaning |
| :----- | :--- | :------ |
| `202 Accepted` | `{"success":true,"data":{"duplicate":false}}` | Published to the ingest queue |
| `200 OK` | `{"success":true,"data":{"duplicate":true}}` | Already processed (matched by SHA-256 of the raw bytes) — not re-queued |
| `401 Unauthorized` | `{"success":false,"message":"..."}` | Missing/invalid `X-Ingest-Token` |
| `415 Unsupported Media Type` | `{"success":false,"message":"..."}` | Wrong `Content-Type` |
| `413 Payload Too Large` | `{"success":false,"message":"..."}` | Body exceeds the size limit |
| `429 Too Many Requests` | `{"success":false,"message":"..."}` | Over 60 requests/hour for this token |

A `202` means the email is queued, **not** that it has been parsed and persisted yet — that
happens asynchronously (see
[Ingest Pipeline]({{ site.baseurl }}/architecture/ingest-pipeline/)). Poll `GET /api/newsletters`
or `GET /api/links` to see it land, usually within a second or two.
