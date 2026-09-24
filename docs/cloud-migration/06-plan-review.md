# Review of the migration plan

This is a critique of the first version of this plan, written after checking the claims against the code and against 2026 list prices. Three load-bearing claims were wrong. The target architecture survives, but the justification and the order of work change, and the plan no longer recommends migrating for cost reasons alone.

Read this document before Phase 2 of [the phased plan](05-phased-migration.md). The other documents have been corrected; this one records what changed and why, so the reasoning can be challenged.

## Verdict

| Part of the plan | Status |
| --- | --- |
| “Cold start is caused by scale-to-zero plus a pausing database” | Correct in substance, wrong in mechanism. The readiness probe deliberately excludes the database. |
| “Keeping it awake on Azure is expensive” | Only true for the database. Keeping Container Apps warm costs single-digit dollars a month. |
| “Azure Managed Redis has a large monthly floor” | Wrong. Balanced B0 is about $11.68/month for one node. |
| “Hetzner is the lowest-cost variant at a few euros” | Unsafe. Hetzner raised prices twice in 2026 and the cost-optimized family was reported unavailable in September 2026. |
| “Neon with suspend disabled” | Correct, but it is not free. Always-on 0.25 CU is about $19.72/month, and disabling scale-to-zero requires a paid plan. |
| “pgvector is a like-for-like replacement for Azure AI Search” | Not for the memory query, which uses the semantic ranker. The replacement needs hybrid search and fusion, or an accepted downgrade. |
| “Swap `ISearchIndexService` and the provider is replaceable” | Understated. Call sites build Azure `SearchOptions`, OData filter strings, and read `SearchDocument`. The index schema is also created in code. |
| Target shape (one always-on host, Postgres with vectors, object storage, Auth0 unchanged) | Sound. |
| Order of work (interfaces, then data, then compute) | Sound, but a cheap in-place fix should come first. |

**The headline change.** Migrating to Fly plus Neon is unlikely to save money against simply fixing the current setup, because today's database is on the Azure SQL free offer and today's search is on the Azure AI Search free tier. The defensible reasons to migrate are the 50 MB search ceiling, consolidation onto one data store, and leaving a platform the team no longer wants — not the monthly bill. The cold start can be fixed in place, and should be, before any migration starts.

## Findings

### 1. The readiness probe does not wait for the database

The plan said a resuming database fails readiness for much of a minute. It does not. `ProjectBrain.ServiceDefaults/Extensions.cs` maps `/health` with `Predicate = r => !r.Tags.Contains("db")` and the comment says this is deliberate, “so probes do not keep Azure SQL serverless awake and block auto-pause”. The migrations health check is tagged `db` and is only exposed on `/health/db`. `DatabaseStartupHostedService` is a `BackgroundService`; its 90 second retry loop does not gate readiness either.

So the real cold-start path is: replica start, image pull, .NET host start, and then **the first request that touches the database** waits for Azure SQL auto-resume. Microsoft documents auto-resume latency as “on the order of one minute”, sometimes seconds.

Two consequences:

- The fix is about the database pausing and the replica count, not about probe tuning. Phase 0's measurement should time the first authenticated request that reads data, not `/health`.
- The current design intentionally reports healthy while the database is unavailable. Any new host will therefore route traffic to an instance that cannot serve data. That is acceptable while the database pauses; it is a liability once it does not. Revisit it when the pause is gone.

### 2. Keeping Container Apps warm is cheap, and the plan implied otherwise

Consumption-plan Container Apps bills replicas at an **idle** rate when they are above zero and not serving requests: about $0.000003 per vCPU-second against $0.000024 active, with memory at about $0.000003 per GiB-second. Each subscription also gets 180,000 vCPU-seconds, 360,000 GiB-seconds, and 2 million requests free per month.

At the configured 0.25 vCPU and 0.5 GiB, one always-running replica allocates roughly 648,000 vCPU-seconds and 1,296,000 GiB-seconds per month. After the free grants, at idle rates, that is roughly $1.40 plus $2.81, so about **$4 per app per month**, plus whatever active time real traffic adds. Two apps is under $10.

The plan treated “two always-on apps” as the expensive configuration. It is not. Setting `minReplicas=1` is close to free and removes the replica-start half of the cold start immediately.

### 3. Azure Managed Redis is a small floor, not a large one

Balanced B0 (1 GB) is about **$11.68/month** for one node PAYG, $23.36 for a two-node HA pair. The AppHost already disables high availability, so the single-node price applies.

The plan described Redis Enterprise as a cluster-priced service that made it “a poor cache for an app that is allowed to sleep”. At $12 it is a modest line, and it is not the reason the bill feels high. It is still the largest fixed line if the database is free, so it is worth revisiting — but as an optimisation, not a motivation.

Redis is genuinely used: `builder.AddRedisDistributedCache("azurecache")`, consumed by `RedisCacheService`, `UserActivityService`, `UserActivitySyncService`, and `DistributedWebhookIdempotencyService`. Webhook idempotency must survive restarts, so “just use in-memory cache” is not safe for Stripe and Auth0 webhooks.

### 4. The database is probably free today, which inverts the cost argument

`AppHost.cs` sets `database.FreeLimitExhaustionBehavior = FreeLimitExhaustionBehavior.BillOverUsage`, which means the free offer is enabled. That offer gives, per database per month: 100,000 vCore-seconds of serverless compute, 32 GB data, 32 GB backup, with overage billed at General Purpose serverless rates.

An always-on database at the serverless minimum of 0.5 vCore allocates about 1,296,000 vCore-seconds a month — roughly thirteen times the free allowance. Priced at General Purpose serverless rates that is a three-figure monthly bill. This is why the team ended up sleeping staging: the free offer is only free if the database is idle most of the time.

That has two implications the plan missed:

- Disabling auto-pause on the current database is the one change that would genuinely cost a lot. It should never be the recommendation.
- Azure has a cheap always-on option the plan never priced: the **DTU purchasing model** (Basic or S0), which does not auto-pause. Confirm current list prices in the calculator for the target region before relying on it. Caveats: Basic has a 2 GB size cap, S0 is 10 DTU and may be too slow under chat load, and converting a free-offer database to a paid tier is **one-way** — you cannot go back to the free offer.

So the comparison is not “cheap and cold on Azure” against “cheap and warm elsewhere”. It is “free and cold” against “about $15–20 a month and warm”, wherever the warm database lives.

### 5. Corrected cost comparison

List prices, September 2026, EU regions, excluding model tokens, bandwidth, and VAT. Verify each in the vendor calculator before committing; these are order-of-magnitude figures for a decision, not a quote.

| Option | Monthly | What you get | What you take on |
| --- | --- | --- | --- |
| **Today** | Near zero for SQL and Search; about $12 Redis; Container Apps mostly inside the free grant; storage and App Config negligible | Cheapest possible | Cold start on the first query after a pause; a 50 MB search ceiling |
| **A. Stay on Azure and fix** | Container Apps warm about $8; Azure SQL on a DTU tier about $5–15; Redis B0 $11.68; storage about $1 → **roughly $25–35** | No data migration. Keeps AI Search Free and Azure OpenAI. Days of work, not a project. | Still on Azure. Still bounded by the 50 MB Free search tier. One-way exit from the SQL free offer. |
| **B. Fly plus Neon** | API 1 GB about $5.92, web 512 MB about $3.32, Redis 256 MB about $2.02 → about $11; Neon always-on 0.25 CU about $19.72 plus $0.35/GB storage; R2 about $1 → **roughly $33** | Managed TLS, rollbacks, managed Postgres with point-in-time restore, pgvector, no search ceiling | Full data migration. Fly raised prices effective 1 October 2026 (additional RAM $5 → $6 per GB). |
| **C. One VPS, app plus Redis, managed Postgres** | Hetzner CX23 class €5.99 if available, otherwise CPX22 about €19.49, plus €0.50 IPv4; Neon about $19.72; R2 about $1 → **roughly $27–43** | Fewest vendors for compute | You patch the host. Hetzner raised prices twice in 2026 and the cost-optimized family was reported unavailable in September 2026, so price this on the day. |
| **D. One VPS with everything on it** | One server plus R2 → **roughly $8–24** | Cheapest always-on by a wide margin | You own Postgres backups, restore drills, and point-in-time recovery for health-adjacent personal data, on one machine with no redundancy. |

Reading of this table: **A is the right first move regardless of the destination.** It removes the symptom for the price of a takeaway. On pure cost, only D clearly beats A, and D transfers real durability risk to a small team. B and C are not cost plays; they are “leave Azure and consolidate the data stores” plays. That is a legitimate goal, but it should be stated as the goal.

### 6. Azure AI Search Free is a capability cliff, and it is the strongest technical reason to move

The Free tier allows **50 MB of storage and 3 indexes**. Every chunk carries a 1536-dimension float vector, which is about 6 KB before content. A few thousand chunks of journals, transcripts, and uploads will exhaust it, and the next step is the Basic tier as a new monthly line.

Worse, the semantic ranker is a premium feature. Microsoft's pricing page states it is not available on the Dedicated Free tier, while the how-to describes a free plan with a monthly allowance of 1,000 requests on all tiers. These two statements are hard to reconcile, so treat it as unresolved and check the running system.

That matters because `UserMemoryRetrievalService` issues `QueryType = SearchQueryType.Semantic` with a `SemanticConfigurationName` of `default`, and wraps the whole call in a `try/catch` that logs “Hybrid memory search failed for user {UserId}; falling back to SQL” and then runs a `LIKE`-style repository search. **Check the logs for that warning before designing for parity.** If it is firing in production, memory retrieval is already degraded to SQL matching, pgvector would be an upgrade, and the plan should say so. If it is not firing, pgvector needs hybrid retrieval (a `tsvector` BM25 index alongside the vector index, fused with reciprocal rank fusion) to avoid a visible regression, and even then there is no equivalent of the hosted reranker without adding a reranking service.

Chat RAG in `AzureOpenAI.cs` is a different story: pure vector KNN with a filter on `ownerId`, which pgvector reproduces exactly.

### 7. The search abstraction is a bigger job than “change the return type”

The plan said `ISearchIndexService` returns Azure SDK types and needs a project-owned result type. True, but incomplete. The interface is:

```349:356:ProjectBrain.Api/ai/AzureSearchClient.cs
public interface ISearchIndexService
{
    Task<Response<SearchResults<SearchDocument>>> SearchAsync(string query, SearchOptions searchOptions);
    Task DeleteDocumentsFromIndexAsync(string filename, string location);
    Task DeleteAllDocumentsFromIndexAsync(string? userId);
    Task<int> DeleteAllDocumentsForUserAsync(string? userId);
    Task ExtractEmbedAndIndexFromStreamAsync(Stream stream, string filename, string? userId, string blobPath, string resourceId, bool removeExistingDocuments = false);
}
```

Callers pass an Azure `SearchOptions` they construct themselves, including OData filter strings such as `ownerId eq '{userId}' or ownerId eq '' or ownerId eq null`, a `Select` list of field names, and `VectorSearchOptions`. They then read results with `SearchDocument.GetString("memoryType")`. So the work is a provider-neutral query model (filters, top-k, fields, vector, optional keyword) plus three call sites: chat RAG, memory retrieval, and erasure.

Also, the index schema is created in application code (`background_tasks/AISeeding.cs` builds `SearchIndex`, HNSW config, semantic config, and a 1536-dimension vector field). For Postgres that becomes an EF migration plus an index strategy, which is better, but it is a rewrite of that file rather than a swap.

One simplification worth noting: `DeleteAllDocumentsForUserAsync` currently pages through documents 1,000 at a time to delete them. In Postgres that is one `DELETE ... WHERE user_id = @id`, which makes erasure both simpler and more reliable.

### 8. Concurrency: the plan assumed a single instance without saying so

`Program.cs` calls `AddSignalR()` with **no backplane** and maps `/hubs/coach-messages`. `CoachMessages.cs` pushes to clients through `IHubContext<CoachMessageHub>`. The AppHost sets only `MinReplicas`; nothing sets `MaxReplicas`, so the Container Apps default maximum applies and production can already scale to several replicas. With more than one replica and no backplane, a coach message published on replica A never reaches a client connected to replica B.

Three more things are per-instance today:

- `AddRateLimiter` uses an in-process partitioned limiter, so the 300 requests per minute limit is per replica.
- `UserActivityBackgroundService` is registered as a singleton hosted service, so each replica runs its own copy.
- TickerQ is started with `app.UseTickerQ()` in every instance. Confirm what distributed locking TickerQ 10.4 provides before running two instances, or jobs may run twice.

This is a latent issue on Azure today, and the migration must make a deliberate choice rather than inherit it:

- Pin the API to exactly one instance and accept a short gap during deploys and host maintenance, or
- Run two or more instances and add the Redis backplane (`AddStackExchangeRedis` on SignalR), confirm TickerQ locking, and accept that the rate limit becomes per instance.

The first version of the plan recommended a single always-on machine without mentioning that a single machine means no redundancy during deploys or host events. Container Apps gives rolling revisions by default; a one-machine Fly app or one VPS does not. State the availability target explicitly.

### 9. Postgres portability details the plan waved at

- **Collation.** SQL Server databases are case-insensitive by default; Postgres is case-sensitive. `UserRepository` looks users up with `u.Email == email`, and other code paths normalise with `.ToLower()` first. After the move, a user who signs in with `User@example.com` may not match a stored `user@example.com`. Decide on one approach — `citext`, a `lower(email)` unique index with normalisation at the boundary, or a nondeterministic ICU collation (which then breaks `LIKE` patterns) — and cover it with tests. Also audit `Contains` filters such as the coach country search.
- **Migrations are not tested today.** `DatabaseIntegrationTests` starts a SQL Server Testcontainer and calls `EnsureCreatedAsync()`, so the existing migration set is never exercised in CI. A Postgres baseline therefore needs its own verification: apply migrations to an empty database, then compare against the model. Switch the container to a pgvector-enabled Postgres image so vector tests run in CI too.
- **Sequences.** Identity columns become sequences that must be set past the copied maximum, as the plan said. Worth keeping.

### 10. Running outside Aspire needs the connection names reproduced exactly

Aspire injects configuration the app reads by name. Outside Aspire, every one of these must be set by hand: `ConnectionStrings__projectbraindb`, `ConnectionStrings__azurecache`, `ConnectionStrings__blobs`, `ConnectionStrings__queues`, the `ai-search` and `openai` client connection strings, plus `Auth0__*`, `Mailgun__*`, `Firebase__CredentialsJson`, `LaunchDarkly__SdkKey`, `GoogleMaps__GeocodingApiKey`, `Stripe__WebhookSecret`, and the `OTEL_*` variables. `AddServiceDefaults` also registers service discovery and standard resilience for all `HttpClient`s; that keeps working without Aspire, but any code relying on Aspire-style `services__*` names would not. Audit for that before the first deploy rather than during it.

Feature flags read Azure App Configuration in `FeatureFlags.cs`. Confirm the code tolerates the provider being absent before removing the store, since LaunchDarkly is the actual flag system.

### 11. The frontend image is environment-specific

`projectbrain.frontend/Dockerfile` takes a `DEPLOY_ENV` build argument and copies `.env.staging` or `.env.production` before `next build`. `NEXT_PUBLIC_*` values, including `NEXT_PUBLIC_API_SERVER_URL` used by the browser SignalR client in `coach-messages-hub-client.ts`, are baked at build time.

So the same web image cannot be promoted from staging to production. The pipeline must build one image per environment, and the API hostname must be known at build time. The deployment document has been corrected; the first version implied a single image with runtime environment variables.

The browser also talks to the API hub directly, so the new host must allow WebSocket upgrades on the API domain and CORS must keep `AllowCredentials` with the exact frontend origin.

### 12. Compliance was underweighted

The data includes journals, mood, coping strategies, voice notes, and coach conversations. In the UK and EU that is likely to touch special category data, and the app already has a user-erasure orchestrator, which suggests erasure obligations are being taken seriously.

Adding Fly or Hetzner, Neon, Cloudflare, and possibly OpenAI means new sub-processors. Before production data moves, the plan needs: signed data processing agreements with each new vendor, an updated sub-processor list and privacy notice, a DPIA review of the change, a decision on model-provider data retention (OpenAI's zero-retention and no-training terms need to be requested and recorded, not assumed), and confirmation of where backups live. This is a gate on Phase 7, not paperwork to do afterwards, and it is a real argument for keeping Azure OpenAI in West Europe where the processing boundary is already documented.

### 13. Backups and restore were only specified for objects

The first version asked for an R2 restore test but said nothing about database recovery. Set an explicit recovery point and recovery time objective, then test against it:

- The free-offer database has 7-day point-in-time restore today (and no SLA, as Microsoft notes it is intended for development and proof-of-concept use — worth flagging on its own).
- Neon Launch offers roughly a 7-day restore window; the Scale plan extends it.
- Self-managed Postgres on a VPS has whatever you build: nightly `pg_dump` is a day of data loss, continuous archiving with WAL-G to R2 is closer to minutes. Option D above is only acceptable with the latter, plus a rehearsed restore.

### 14. Smaller corrections

- **Moving models saves nothing.** Azure OpenAI GlobalStandard is pay-per-token with no idle floor, so Phase 6 is not a cost measure. It is only worth doing to close the Azure account or to consolidate vendors, and it carries prompt-behaviour risk. It has been demoted to optional.
- **Transcription limits.** The OpenAI audio transcription endpoint caps upload size (25 MB at time of writing). Check the voice-note limits enforced in `Chat.cs` and `VoiceNotes.cs` against that before switching, and confirm the same audio formats are accepted.
- **Model naming.** `gpt-5-mini` and `text-embedding-3-small` are Azure deployment names here (`openai-chat-deployment`, `openai-embed-deployment`). Confirm the equivalent OpenAI API model identifiers exist under the account before the switch, and keep the embedding at 1536 dimensions or plan a full re-embed.
- **The Speech resource really is unused.** The Bicep template is provisioned and the connection string into the API is commented out. Deleting it is a free win in Phase 1, not something to wait for.
- **Migration job seeding.** `ProjectBrain.MigrationService` seeds admin and test users from `AdminUser__Password` and `TestUsers__Password`. Whatever replaces the Container App Job must not seed test users into production. Assert on `deploy-env` in the release command.
- **The frontend waits on Redis.** `AppHost.cs` has the frontend `WaitFor(cache)`, but the Next.js app has no Redis dependency. Probably vestigial; drop it rather than reproducing it on the new host.

## What this changes in the plan

1. A new Phase 1 fixes the cold start in place on Azure: one warm replica per app, a database that does not pause, and deletion of the unused Speech resource. It is cheap and it is reversible.
2. Phase 2 is an explicit decision gate. Migration proceeds on the strength of the search ceiling, store consolidation, and platform preference, with the measured bill from Phase 1 in hand — not on an assumption that Azure is expensive.
3. The compute recommendation is stated as a tradeoff between cost and operational burden, with an availability target, rather than as a single answer. Fly remains the default for a small team that wants managed TLS and rollbacks with a managed database.
4. Phase 6 (models) is optional and explicitly not a cost saving.
5. New required work: collation strategy, SignalR and job concurrency decision, per-environment frontend images, connection-name audit, data processing agreements and DPIA, and a database restore drill.

## Open questions for the team

1. Is the “Hybrid memory search failed … falling back to SQL” warning present in production logs? This decides whether pgvector must implement hybrid retrieval and reranking, or is an improvement on what users get today.
2. How close is the Azure AI Search index to the 50 MB Free limit? That number sets the deadline for the whole migration.
3. What is the actual invoice, grouped by resource, for a full month? Every cost statement here is a list price, not your bill.
4. What is the availability target during deploys? One instance is simplest and cheapest, and it means a visible gap.
5. Is the team willing to own Postgres backups and restore drills? A yes makes the cheapest option viable; a no means a managed database and a higher floor.
6. Does the free-offer database ever exceed 100,000 vCore-seconds in a month today? If it does, Azure SQL is already billing and the comparison shifts toward migrating.
