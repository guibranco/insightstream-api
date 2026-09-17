---
title: API Reference
nav_order: 4
has_children: true
permalink: /api-reference/
---

# 📡 API Reference

Complete reference for the InsightStream REST API.

---

## In this section

| Page | What you'll learn |
| :--- | :---------------- |
| [Authentication]({{ site.baseurl }}/api-reference/authentication/) | JWT login and the ingest token |
| [Ingest API]({{ site.baseurl }}/api-reference/ingest-api/) | The machine-to-machine email ingest endpoint |
| [Links API]({{ site.baseurl }}/api-reference/links-api/) | Query, prioritize, inspect, and update links |
| [Newsletters & Authors API]({{ site.baseurl }}/api-reference/newsletters-and-authors-api/) | Stats, newsletters, and authors |

---

## Base URL

Local development default:

```
http://localhost:5000
```

## Response envelope

Every response — success or failure — uses the same JSON shape:

```json
{
  "success": true,
  "data": { "...": "..." },
  "message": null,
  "pagination": null
}
```

`data` and `pagination` are omitted (`null`) where not applicable; `message` carries a
human-readable explanation on failure. Enum values (link status, etc.) are serialized as strings.

## Status codes

| Code | Meaning |
| :--- | :------ |
| `200 OK` | Successful read, update, or a duplicate-ingest no-op |
| `202 Accepted` | Email accepted and queued for processing |
| `400 Bad Request` | Malformed request body (e.g. an invalid enum value) |
| `401 Unauthorized` | Missing/invalid JWT, ingest token, or login credentials |
| `403 Forbidden` | Authenticated but not permitted |
| `404 Not Found` | Resource does not exist |
| `413 Payload Too Large` | Email exceeds the configured size limit |
| `422 Unprocessable Entity` | Invalid query parameter (status/sort whitelist, pagination bounds) |
| `429 Too Many Requests` | Rate limit exceeded (login or ingest) |
| `500 Internal Server Error` | Unexpected error — never leaks exception details |

## CORS

Locked to a single, exact frontend origin (`Cors:AllowedOrigin` — scheme + host, no path),
methods `GET, POST, PUT, DELETE, OPTIONS`, headers `Content-Type, Authorization`. No credentials
mode — the token travels as an `Authorization` header, not a cookie. Preflight `OPTIONS` requests
succeed on every route, including error paths, since CORS middleware runs ahead of authentication
in the pipeline.
