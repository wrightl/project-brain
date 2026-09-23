# Cloud migration plan

ProjectBrain is hosted on Azure through .NET Aspire and `azd`: Container Apps for the API and Next.js frontend, Azure SQL, Azure AI Search, Azure OpenAI, Azure Managed Redis, Blob Storage and Queues, App Configuration, and an Azure AI Speech template. Staging is deliberately scaled to zero so Azure SQL can auto-pause. That combination is what makes hosting both expensive to keep warm and slow to wake up.

This plan keeps the ASP.NET Core API and the Next.js frontend. It moves the always-on footprint onto a small machine, replaces Azure SQL and Azure AI Search with one Postgres database (including vectors), and leaves Auth0, Stripe, Mailgun, Firebase, LaunchDarkly, Google Maps, and Grafana Cloud where they are.

## Recommendation

| Concern | Stay | Move to |
| --- | --- | --- |
| API and frontend | ASP.NET Core on .NET 10, Next.js 15 | [Fly.io](https://fly.io) Machines, one shared CPU each, minimum one machine running |
| Database | Entity Framework Core | Neon Postgres in the EU, scale-to-zero off in production |
| RAG index | `ISearchIndexService` | `pgvector` in that same Postgres database |
| Generative AI | Chat, embeddings, transcription behind the existing AI services | OpenAI API for `gpt-5-mini`, `text-embedding-3-small` (1536 dimensions), and audio transcription |
| Auth | Auth0 (Management API, roles, webhooks, Next.js SDK) | Auth0 |
| Cache | In-process and distributed cache usage | Redis on a small Fly Machine next to the API |
| Files | Blob paths and erasure | Cloudflare R2 (S3-compatible, EU jurisdiction) |
| Background work | TickerQ plus one Azure Storage Queue | TickerQ and the existing `IChatPersistenceQueue` on Postgres |
| Config | Environment variables already used in deploy | Fly secrets. Drop Azure App Configuration in production |
| Deploy | GitHub Actions | GitHub Actions building Docker images and running `flyctl deploy` |
| Local dev | Aspire AppHost | Aspire AppHost, with Azure publish packages no longer driving production |

The lower-cost variant is the same data layer on a single small Hetzner Cloud instance in the EU, deployed with Kamal. Choose that variant when a platform fee is still too high after the Azure bill is gone. Choose Fly when the team wants managed TLS, rollbacks, and less machine administration.

## Why this shape

Cold start today is a chain, not a single slow process:

1. Staging workflows set Container Apps `minReplicas` to 0 so the apps sleep.
2. Azure SQL is configured to pause. `DatabaseStartupHostedService` retries for up to 90 seconds on resume errors such as SQL 40613.
3. The API readiness probe allows six failures at five seconds each, so a paused database fails the app while it is waking.
4. Azure Managed Redis has its own monthly floor and does not get cheaper when the apps are scaled to zero.

Keeping Container Apps and Azure SQL awake removes the pause and raises the bill. A small always-on machine plus a Postgres instance that is not allowed to suspend removes the pause without a platform floor for Redis Enterprise, AI Search, a Container Apps environment, and a Speech resource.

Token spend for the models stays usage-based either on Azure OpenAI or on the OpenAI API. Moving models does not, by itself, cut a large idle bill. It removes a Cognitive Services account from the startup and provisioning path.

## Documents

1. [Current architecture and cost drivers](01-current-architecture.md)
2. [Platform alternatives](02-platform-alternatives.md)
3. [Technology, stack, and frameworks](03-technology-and-stack.md)
4. [Deployment](04-deployment.md)
5. [Phased migration](05-phased-migration.md)

## Assumptions

These stand in for the clarifying questions that would normally gate the plan. Change the recommendation if an assumption is wrong.

1. The joint goal is a lower monthly hosting bill and a first request that does not wait on a paused database or a scaled-to-zero replica.
2. Production traffic fits the current size: 0.25 vCPU and 0.5 Gi per app, with staging slept overnight.
3. The team will keep C# for the API and TypeScript/Next.js for the client.
4. A full Auth0 migration is out of scope unless Auth0 itself is a top line on the bill.
5. EU hosting is the default because Speech and Azure OpenAI are pinned to `westeurope`. A hard “no data leaves the EU / UK, including model prompts” requirement is not confirmed.
6. Stripe, Mailgun, Firebase, LaunchDarkly, Google Maps, and Grafana Cloud stay.
7. There is no procurement rule that the workload must remain on Azure.

## Decisions still needed

Answer these before Phase 5 (compute cutover). Earlier phases can start without them.

| # | Decision | Default if unanswered |
| --- | --- | --- |
| 1 | Hard data-residency rule for prompts, embeddings, files, and the primary database | EU region for compute, Postgres, and R2. OpenAI API for models. If prompts must stay inside a Microsoft EU boundary, keep Azure OpenAI in West Europe and move everything else. |
| 2 | Fly.io or Hetzner/Kamal for compute | Fly.io |
| 3 | Neon or Postgres on the same machine as the API | Neon for Fly. Postgres on the VPS only if Hetzner is chosen and a managed database fee is rejected. |
| 4 | Budget ceiling that would make even one always-on API machine unacceptable | One shared-CPU API machine and one shared-CPU web machine stay running |
| 5 | Whether staging must sleep | Staging stays warm on the smallest size. Sleeping staging is what reintroduces cold start. |
| 6 | Acceptable cutover window and rollback length | Staging soak, then production DNS cutover with Azure left intact for one release cycle |

## What this plan does not do

- It does not rewrite the API in another language.
- It does not replace Auth0 in the first migration.
- It does not self-host a model. A GPU large enough for the current chat model costs more than pay-per-token inference at this traffic level.
- It does not keep scale-to-zero as a cost strategy. The product already showed that scale-to-zero is how the slow start is produced.
