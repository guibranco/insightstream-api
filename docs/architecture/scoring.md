---
title: Scoring
parent: Architecture
nav_order: 3
---

# 🎯 Scoring
{: .no_toc }

<details open markdown="block">
  <summary>Table of contents</summary>
  {: .text-delta }
- TOC
{:toc}
</details>

---

Every link gets a deterministic score from 0–10, computed from four independent factors and
combined with configurable weights (`Scoring:*`, see
[Configuration]({{ site.baseurl }}/getting-started/configuration/)).

## The four factors

**Author factor** `[-1, 1]` — for each author attributed to the link:

```
(liked − discarded − 0.5 × discardedAfterReview) / totalReviewedInteractions
```

computed live from that author's other links' current statuses (`Awaiting` links don't count as
"reviewed"). Zero if the author has no reviewed history yet, or the link has no attributed
author. Averaged across authors if a link has more than one.

**Keyword factor** `[-1, 1]` — the link's title is tokenized (lowercased, non-alphanumeric split,
stop words removed, `Scoring:MinKeywordLength` minimum length), then averaged against any
matching `UserPreference` keyword weights. Zero if no tokens match a stored preference.

**Recency factor** `[0, 1]` — linear decay from 1.0 (just seen) to 0.0 over
`Scoring:RecencyDecayDays` (default 30), based on `FirstSeen`.

**Popularity factor** `[0, 1]` — how many distinct newsletters the link has appeared in, capped
at `Scoring:PopularityCap` (default 3) and normalized.

## Combining them

```
score = clamp(
  5.0
  + 5 × Scoring:AuthorWeight     × authorFactor
  + 5 × Scoring:KeywordWeight    × keywordFactor
  + 5 × Scoring:RecencyWeight    × recencyFactor
  + 5 × Scoring:PopularityWeight × popularityFactor,
  0, 10
)
```

Author/keyword preference can pull the score up *or* down from the neutral baseline of 5;
recency/popularity can only add to it — there's no such thing as "negative recency," so an old,
unpopular link simply doesn't get a freshness bonus rather than being actively penalized for its
age.

{: .note }
The default weights (`0.35 / 0.25 / 0.20 / 0.20`) sum to 1.0, so a maximally-liked, keyword-
matched, brand-new, maximally-popular link scores a clean 10.0. The test suite asserts bounds and
monotonicity (newer > older, more liked-history > more discarded-history, etc.) rather than
pinning exact numbers to these constants.

## Caching

Computed scores are cached in Redis at `link:score:{id}` with a TTL of `Scoring:CacheTtl`
(default 1 hour). `GET /api/links/prioritized` calls `IScoringService.GetScoreAsync`, which
returns the cached value if present or computes and caches it otherwise — "lazy recompute of
stale scores," not an eager background job. The cache entry is explicitly invalidated whenever a
link's status changes via `PUT /api/links/{id}/status`.

## Preference learning

Every status change (`PUT /api/links/{id}/status`) nudges stored preference weights:

| New status | Author adjustment | Keyword adjustment |
| :--------- | :----------------- | :------------------ |
| `Liked` | `+0.2` | `+0.1` (half) |
| `DiscardedAfterReview` | `−0.1` | `−0.05` (half) |
| `Discarded` | `−0.2` | `−0.1` (half) |
| `Awaiting` (reverting) | *(no-op — no learning signal)* | *(no-op)* |

Applied to every author attributed to the link (identified by Medium handle, custom domain, or
name, in that preference order) and every keyword token in its title, clamped to `[-1.00, 1.00]`
and upserted into `user_preferences`.
