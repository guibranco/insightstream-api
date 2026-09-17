---
title: Links API
parent: API Reference
nav_order: 3
---

# 🔗 Links API
{: .no_toc }

<details open markdown="block">
  <summary>Table of contents</summary>
  {: .text-delta }
- TOC
{:toc}
</details>

All endpoints below require `Authorization: Bearer <token>`.

---

## `GET /api/links`

Paginated, filterable, searchable, sortable.

| Query param | Default | Notes |
| :---------- | :------ | :---- |
| `status` | *(none — all statuses)* | One of `Awaiting`, `Liked`, `Discarded`, `DiscardedAfterReview`; `422` if invalid |
| `page` | `1` | `422` if `< 1` |
| `per_page` | `20` | `422` if outside `1..100` |
| `sort` | `priority` | One of `priority` (score desc), `newest`/`oldest` (first-seen), `title`; `422` if invalid |
| `search` | *(none)* | ILIKE match against title and attributed author names |

```bash
curl "http://localhost:5000/api/links?status=Awaiting&sort=priority&page=1&per_page=20" \
  -H "Authorization: Bearer $TOKEN"
```

```json
{
  "success": true,
  "data": [
    {
      "id": "...",
      "url": "https://medium.com/@janedoe/...",
      "title": "Understanding Rust Ownership in 10 Minutes",
      "status": "Awaiting",
      "priorityScore": 7.25,
      "firstSeen": "2026-07-20T09:00:00Z",
      "lastSeen": "2026-07-22T09:00:00Z",
      "authorNames": ["Jane Doe"],
      "newsletterAppearanceCount": 2
    }
  ],
  "pagination": { "page": 1, "perPage": 20, "totalItems": 34, "totalPages": 2 }
}
```

## `GET /api/links/prioritized`

`Awaiting` links only, ranked by `priorityScore` descending. Scores are recomputed lazily — see
[Scoring → Caching]({{ site.baseurl }}/architecture/scoring/#caching).

## `GET /api/links/{id}`

Full detail: attributed authors and every newsletter the link appeared in.

```json
{
  "success": true,
  "data": {
    "id": "...",
    "url": "...",
    "title": "...",
    "status": "Awaiting",
    "priorityScore": 7.25,
    "firstSeen": "...",
    "lastSeen": "...",
    "authors": [{ "id": "...", "name": "Jane Doe" }],
    "newsletters": [{ "id": "...", "title": "...", "receivedDate": "..." }]
  }
}
```

`404` if the link doesn't exist.

## `PUT /api/links/{id}/status`

```bash
curl -X PUT "http://localhost:5000/api/links/$LINK_ID/status" \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{"status":"Liked"}'
```

Valid values: `Awaiting`, `Liked`, `Discarded`, `DiscardedAfterReview` (moving back to `Awaiting`
is allowed — it just carries no preference-learning signal). Triggers
[preference learning]({{ site.baseurl }}/architecture/scoring/#preference-learning) and
invalidates the link's cached score. `400` for an invalid status value, `404` if the link doesn't
exist.
