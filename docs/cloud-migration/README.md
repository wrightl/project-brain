# Cloud migration plan

ProjectBrain is hosted on Azure through .NET Aspire and `azd`: Container Apps for the API and Next.js frontend, Azure SQL, Azure AI Search, Azure OpenAI, Azure Managed Redis, Blob Storage and Queues, App Configuration, and an Azure AI Speech template. Staging is deliberately scaled to zero so Azure SQL can auto-pause. That is what makes the first request slow.

This plan keeps the ASP.NET Core API and the Next.js frontend. It fixes the cold start in place first, then moves the always-on footprint onto a small always-on host and replaces Azure SQL and Azure AI Search with one Postgres database that also holds the vectors. Auth0, Stripe, Mailgun, Firebase, LaunchDarkly, Google Maps, and Grafana Cloud stay where they are.

> This plan was reviewed and corrected. Three of the first version's cost claims did not survive checking. See [the review](06-plan-review.md) for what changed; the short version is below.

## Start here: the cost story is not what it looked like

Two free tiers are doing the work today, and they are the reason the app is both cheap and cold.

- **The database is on the Azure SQL free offer** (`FreeLimitExhaustionBehavior` is set in `AppHost.cs`): 100,000 vCore-seconds per month. Always-on at the 0.5 vCore serverless minimum would allocate around thirteen times that, at General Purpose serverless overage rates. This is the one change that would genuinely cost a lot, so it is never the recommendation.
- **Search is on the Azure AI Search Free tier**: 50 MB of storage and 3 indexes, with the semantic ranker restricted. Every chunk carries a 1536-dimension vector, so this ceiling is close.

Meanwhile the things the first version of this plan blamed are small. Container Apps bills a warm-but-idle replica at roughly a tenth of the active rate and grants 180,000 vCPU-seconds a month per subscription, so keeping both apps warm is under about $10 a month. Azure Managed Redis Balanced B0 is about $11.68 a month, not a large floor.

So: **migrating does not save much against simply fixing the current setup.** Fix it in place first. Then migrate for the reasons that actually hold — the 50 MB search ceiling, consolidating two data stores into one, and leaving a platform the team no longer wants.

| Path | Roughly per month | Verdict |
| --- | --- | --- |
| Today | Near zero for SQL and Search, about $12 Redis, Container Apps mostly inside the free grant | Cold on the first query after a pause; nearly out of search headroom |
| **A. Stay on Azure, remove the pause** | about $25–35 | **Do this first.** No data migration. Days of work. |
| B. Fly plus Neon | about $33 | Managed, consolidates the stores, removes the search ceiling |
| C. One VPS plus managed Postgres | about $27–43 | Fewest compute vendors; price Hetzner on the day |
| D. One VPS running everything | about $8–24 | Cheapest by far, and you own Postgres backups and restore drills |

List prices, September 2026, EU regions, before model tokens and VAT. Verify against your own invoice and the vendor calculators; see [the review](06-plan-review.md) for the workings.

## Recommendation

| Concern | Stay | Move to |
| --- | --- | --- |
| API and frontend | ASP.NET Core on .NET 10, Next.js 15 | One always-on host. [Fly.io](https://fly.io) Machines by default; a single VPS if the team prefers one invoice and will patch a server |
| Database | Entity Framework Core | Postgres. Neon in the EU with scale-to-zero disabled, unless the team accepts owning backups |
| RAG index | `ISearchIndexService` | `pgvector` in that same Postgres database |
| Generative AI | Chat, embeddings, transcription behind the existing AI services | **Optional.** Azure OpenAI already costs nothing when idle. Move to the OpenAI API only to close the Azure account, and never for cost |
| Auth | Auth0 (Management API, roles, webhooks, Next.js SDK) | Auth0 |
| Cache | `IDistributedCache` for activity, idempotency, and app caching | Redis on the new host, private network only |
| Files | Blob paths and erasure | Cloudflare R2 (S3-compatible, EU jurisdiction) |
| Background work | TickerQ plus one Azure Storage Queue | TickerQ and the existing `IChatPersistenceQueue` on Postgres |
| Config | Environment variables already used in deploy | Host secrets. Drop Azure App Configuration in production |
| Deploy | GitHub Actions | GitHub Actions building Docker images and running `flyctl deploy` (or Kamal) |
| Local dev | Aspire AppHost | Aspire AppHost, with Azure publish packages no longer driving production |

## Why the app is slow to start

Not for the reason the first version of this plan gave. `/health` deliberately **excludes** database checks (`ProjectBrain.ServiceDefaults/Extensions.cs`), so readiness does not wait for the database, and the 90 second retry in `DatabaseStartupHostedService` is a background service that gates nothing.

The real sequence is:

1. Staging workflows set Container Apps `minReplicas` to 0, so there is no replica to serve the request.
2. Container Apps creates a replica and pulls the image; the .NET host starts.
3. Azure SQL has auto-paused. **The first request that touches data** waits for auto-resume, which Microsoft documents as on the order of a minute.
4. Only then does chat run, and it then calls Azure OpenAI and Azure AI Search.

Steps 1 and 3 are both fixable without leaving Azure, and step 1 is nearly free to fix. That is Phase 1.

## Documents

1. [Current architecture and cost drivers](01-current-architecture.md)
2. [Platform alternatives](02-platform-alternatives.md)
3. [Technology, stack, and frameworks](03-technology-and-stack.md)
4. [Deployment](04-deployment.md)
5. [Phased migration](05-phased-migration.md)
6. [Review of this plan](06-plan-review.md)

## Assumptions

These stand in for the clarifying questions that would normally gate the plan. Change the recommendation if an assumption is wrong.

1. The goals are a first request that does not wait on a paused database, and a bill that does not grow when the app is kept warm.
2. Production traffic fits the current size: 0.25 vCPU and 0.5 Gi per app.
3. The team will keep C# for the API and TypeScript/Next.js for the client.
4. A full Auth0 migration is out of scope unless Auth0 itself is a top line on the bill.
5. EU hosting is the default. A hard “no data leaves the EU or UK, including model prompts” rule is not confirmed.
6. Stripe, Mailgun, Firebase, LaunchDarkly, Google Maps, and Grafana Cloud stay.
7. There is no procurement rule that the workload must remain on Azure.
8. The data is health-adjacent (journals, mood, coping strategies, coach conversations), so new vendors need data processing agreements before production data moves.

## Decisions still needed

Answer 1 to 4 before Phase 2 (the migration decision gate). Phase 1 needs none of them.

| # | Decision | Default if unanswered |
| --- | --- | --- |
| 1 | After Phase 1, is the bill acceptable? If yes, is there still a reason to migrate? | Yes: the 50 MB Azure AI Search Free ceiling and the wish to run one data store instead of three |
| 2 | Availability target during deploys and host maintenance | One API instance, accepting a short gap. Two instances require a SignalR backplane and TickerQ locking first |
| 3 | Will the team own Postgres backups and restore drills? | No, so use managed Postgres (Neon) and accept the higher floor |
| 4 | Fly.io or a single VPS | Fly.io, for managed TLS, rollbacks, and private networking |
| 5 | Hard data-residency rule for prompts, embeddings, files, and the database | EU region for compute, Postgres, and R2. Keep Azure OpenAI in West Europe unless there is a reason to move models |
| 6 | Acceptable cutover window and rollback length | Staging soak, then production DNS cutover with Azure left intact for one release cycle |

## What this plan does not do

- It does not rewrite the API in another language.
- It does not replace Auth0 in the first migration.
- It does not self-host a model. A GPU large enough for the current chat model costs more than pay-per-token inference at this traffic level.
- It does not claim a large saving. The saving against a fixed-in-place Azure setup is small; the gains are the search ceiling, one data store, and leaving the platform.
- It does not keep scale-to-zero as a cost strategy. Scale-to-zero plus a pausing database is how the slow start is produced, and warm replicas are cheap.
