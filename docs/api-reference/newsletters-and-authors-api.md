---
title: Newsletters & Authors API
parent: API Reference
nav_order: 4
---

# 📰 Newsletters & Authors API
{: .no_toc }

<details open markdown="block">
  <summary>Table of contents</summary>
  {: .text-delta }
- TOC
{:toc}
</details>

All endpoints below require `Authorization: Bearer <token>`.

---

## `GET /api/stats`

```json
{
  "success": true,
  "data": {
    "statusCounts": { "Awaiting": 12, "Liked": 8, "Discarded": 20, "DiscardedAfterReview": 3 },
    "totalLinks": 43,
    "totalNewsletters": 15,
    "totalAuthors": 22,
    "recentNewsletters": [{ "id": "...", "title": "...", "receivedDate": "...", "linkCount": 3 }]
  }
}
```

## `GET /api/newsletters`

List, newest first, with each newsletter's link count.

## `GET /api/newsletters/{id}`

Detail, including every link that appeared in it. `404` if missing.

## `GET /api/authors`

List, alphabetical, with each author's link count.

## `GET /api/authors/{id}`

Detail: every link attributed to the author, plus a per-status interaction breakdown
(`{"Awaiting": 1, "Liked": 4, "Discarded": 2, "DiscardedAfterReview": 0}`) — the same counts that
feed the [author scoring factor]({{ site.baseurl }}/architecture/scoring/#the-four-factors).
`404` if missing.
