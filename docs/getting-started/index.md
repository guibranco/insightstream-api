---
title: Getting Started
nav_order: 2
has_children: true
permalink: /getting-started/
---

# 🚀 Getting Started

Everything you need to go from zero to a running InsightStream instance.

---

## In this section

| Page | What you'll learn |
| :--- | :---------------- |
| [Installation]({{ site.baseurl }}/getting-started/installation/) | Cloning, restoring, and running locally |
| [Configuration]({{ site.baseurl }}/getting-started/configuration/) | All available settings and environment variables |
| [Quick Start]({{ site.baseurl }}/getting-started/quick-start/) | Ingest the sample fixture and query the API in 5 minutes |

---

## Prerequisites

| Requirement | Minimum version | Notes |
| :---------- | :-------------- | :---- |
| .NET SDK | 10.x (pinned via `global.json`) | Building, running migrations, `dotnet run` |
| PostgreSQL | 14+ | Primary datastore |
| Redis | any recent | Score cache + rate limiting |
| LavinMQ (or RabbitMQ) | any recent | AMQP 0-9-1 ingest queue |
| Docker Desktop | any recent | Only needed for the Testcontainers integration tests |

---

## Choose your path

### 💻 I want to run it locally right now

Follow [Installation]({{ site.baseurl }}/getting-started/installation/) — clone, configure,
migrate, `dotnet run`.

### 🏭 I want to deploy it to production

Skip to [Deployment]({{ site.baseurl }}/deployment/) for the full Ubuntu + Nginx + systemd
walkthrough.

### 🔌 I want to understand the ingest pipeline first

Read [Architecture]({{ site.baseurl }}/architecture/) before touching any code.
