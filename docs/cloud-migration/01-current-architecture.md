# Current architecture and cost drivers

Production is defined in `ProjectBrain.AppHost/AppHost.cs` and published with `azure.yaml` (`host: containerapp`) through `azd provision` and `azd deploy`. GitHub Actions in `.github/workflows/azure-deploy.yml` is the release path for staging and production.

## What runs where

| Piece | How it is hosted today | Wired in |
| --- | --- | --- |
| API | Azure Container Apps, external HTTP, 0.25 vCPU, 0.5 Gi, `minReplicas` from config | `PublishAsAzureContainerApp` in `AppHost.cs` |
| Frontend | Next.js Docker image on a second Container App, port 3000 | `AddDockerfile` for `projectbrain.frontend` |
| Migrations | Container App Job | `ProjectBrain.MigrationService`, `PublishAsAzureContainerAppJob` |
| Database | Azure SQL, free-offer exhaustion set to bill over usage | `AddAzureSqlServer` |
| RAG | Azure AI Search, SKU from `AI_SEARCH_SKU`, default `Free` | `AddAzureSearch("ai-search")` |
| Generative AI | Azure OpenAI in `westeurope`: chat `gpt-5-mini`, embeddings `text-embedding-3-small`, Whisper deployment | `AddAzureOpenAI` plus three deployments |
| Speech | Bicep template `ProjectBrain.AppHost/Bicep/azureaispeech.bicep` | Connection string into the API is commented out. Transcription goes through the Whisper deployment. |
| Files | Azure Storage account, public blob access off, blob service `blobs` | `AddAzureStorage` |
| Queue | Azure Storage Queues | `AzureStorageChatPersistenceQueue` behind `IChatPersistenceQueue` |
| Cache | Azure Managed Redis (Redis Enterprise), HA and zones disabled, access keys | `AddAzureManagedRedis` |
| Config | Azure App Configuration, Free SKU, purge protection off | `AddAzureAppConfiguration` |
| Auth | Auth0, including the Management API and a webhook | Not an Azure AD tenant |
| Other SaaS | Stripe, Mailgun, Firebase, LaunchDarkly, Google Maps, Grafana Cloud OTLP | Environment variables in the deploy workflow |
| Local dev | Aspire: SQL Server container, Azurite, Redis container, App Configuration emulator, npm dev server for Next.js | `AppHost.cs` when not in publish mode |

The API is ASP.NET Core on .NET 10. Data access is Entity Framework Core against SQL Server (`Aspire.Microsoft.EntityFrameworkCore.SqlServer`). Coach geo search is an application bounding box over latitude and longitude columns. There is no SQL Server `geography` type.

Retrieval uses 1536-dimension embeddings (`EmbeddingGenerationOptions.Dimensions = 1536`) stored in Azure AI Search. Indexing, chat RAG, user-memory indexing, and user erasure all go through `ISearchIndexService` / `AzureSearchClient`. That interface returns Azure Search SDK types, and callers construct Azure `SearchOptions` themselves, so the provider is not swappable without a new query model.

The two retrieval paths are not equivalent, which matters for any replacement:

- **Chat RAG** (`AzureOpenAI.cs`) is a pure vector nearest-neighbour query filtered by `ownerId`. pgvector reproduces this exactly.
- **User memory** (`UserMemoryRetrievalService`) asks for `QueryType = SearchQueryType.Semantic` with a semantic configuration, i.e. hybrid search plus the hosted semantic ranker, and catches every exception to fall back to a SQL `LIKE` search, logging “Hybrid memory search failed for user {UserId}; falling back to SQL”. The semantic ranker is restricted on the Free tier the AppHost provisions, so **check production logs for that warning**: if it is firing, this path is already degraded and pgvector is an improvement rather than a regression.

Background work is mostly TickerQ (journals, voice notes, goals, coping strategies, agent follow-up). Chat persistence is the exception: it uses an Azure Storage Queue.

## Why startup is slow

Staging is put to sleep on purpose.

- `.github/workflows/sleep-staging.yml` scales `api` and `frontend` to `minReplicas=0` “so Azure SQL can auto-pause”.
- `.github/workflows/scale-staging-container-apps.yml` runs nightly and on deploy, and forces the same zero.
- `wake-staging.yml` is the manual path back to one replica.

The first request after sleep pays for:

1. Container Apps creating a replica and pulling the image.
2. The .NET process starting, building the host, and connecting to Redis, Storage, Search, and OpenAI.
3. **Azure SQL auto-resume, on the first request that touches data.** Microsoft documents this as on the order of a minute, sometimes seconds.
4. Then chat itself, which calls Azure OpenAI and Azure AI Search.

Two things are *not* part of the delay, contrary to how this looks at first glance:

- **Readiness does not wait for the database.** `ProjectBrain.ServiceDefaults/Extensions.cs` maps `/health` with `Predicate = r => !r.Tags.Contains("db")`, with a comment saying this is deliberate so probes do not keep Azure SQL awake. The migrations check is tagged `db` and is served only on `/health/db`.
- **The 90 second retry loop gates nothing.** `DatabaseStartupHostedService` is a `BackgroundService`. It records state and logs; it does not hold up readiness or requests.

A side effect worth carrying into any new host: the API reports healthy while the database is unreachable. That is a reasonable trade while the database pauses, and a liability once it does not.

Production uses the same Container Apps and Azure SQL settings, so any `minReplicas` of 0, or any SQL auto-pause, reproduces this.

## What actually costs money

Both of the big services are on free tiers, which is why the app is cheap and cold at the same time. All figures are list prices for EU regions in September 2026; check them against the invoice.

| Resource | Idle behaviour | What it costs |
| --- | --- | --- |
| Azure SQL free offer with `BillOverUsage` | Auto-pauses, which is the cold start | 100,000 vCore-seconds, 32 GB data, and 32 GB backup free per database per month. Always-on at the 0.5 vCore serverless minimum would allocate roughly thirteen times the allowance, billed at General Purpose serverless rates. **Disabling auto-pause on this tier is the expensive move.** Converting to a paid tier is one-way. |
| Azure AI Search, Free SKU | No compute charge | $0, but **50 MB of storage and 3 indexes**. At 1536 dimensions each vector is about 6 KB before content, so the ceiling is near. The semantic ranker is restricted on this tier. This is the real deadline. |
| Azure Managed Redis, Balanced B0, HA disabled | Stays provisioned | About $11.68 per month for a single node. The largest fixed line while the database is free, but a modest one. |
| Container Apps, Consumption | Replicas above zero but idle bill at a reduced rate | Roughly a tenth of the active vCPU rate, plus a monthly grant of 180,000 vCPU-seconds and 360,000 GiB-seconds per subscription. One always-warm 0.25 vCPU / 0.5 GiB replica works out at roughly $4 per month. **Keeping both apps warm is cheap.** |
| Azure OpenAI | GlobalStandard chat and embeddings are per token; Whisper is a Standard deployment | No idle floor. Moving models off Azure saves nothing. |
| Azure AI Speech | Provisioned from Bicep, connection string into the API commented out | A resource nothing uses. Delete it. |
| App Configuration, Free SKU | Free | Not a cost problem. It is still an Azure client on the startup path. |
| Storage account | Small, private | Usually negligible. Queues and blobs are a coupling problem, not a cost one. |

Get the invoice grouped by resource for a full month before choosing a destination. If Log Analytics ingestion, bandwidth, or a paid Search SKU shows up instead, explain that line before changing the architecture.

## Concurrency assumptions baked into the current code

The AppHost sets only `MinReplicas`. Nothing sets `MaxReplicas`, so the Container Apps default maximum applies and production can already run several replicas. Four things assume it does not:

| Assumption | Where | Effect with more than one replica |
| --- | --- | --- |
| SignalR has no backplane | `AddSignalR()` in `Program.cs`, `/hubs/coach-messages`, pushed via `IHubContext<CoachMessageHub>` | A coach message published on one replica never reaches a client connected to another |
| Rate limiting is in-process | `AddRateLimiter` with a partitioned limiter | The 300 per minute limit applies per replica |
| `UserActivityBackgroundService` is a singleton hosted service | `Program.cs` | Each replica runs its own copy |
| TickerQ starts in every instance | `app.UseTickerQ()` | Duplicate job execution unless TickerQ's own locking prevents it. Confirm for version 10.4 before running two instances |

This is a latent issue today, not something the migration introduces. Whatever the destination, the choice has to be made explicitly: one instance and a short gap during deploys, or several instances plus a Redis backplane and verified job locking.

## Coupling that the migration has to break

These are the code boundaries that later phases change. Hosting can move only after the API can be pointed at the new services with configuration.

| Boundary | Current type | Replacement needs |
| --- | --- | --- |
| Search | `ISearchIndexService.SearchAsync` returns `Response<SearchResults<SearchDocument>>`, and callers pass an Azure `SearchOptions` with OData filter strings, a `Select` field list, and `VectorSearchOptions` | A provider-neutral query model, not just a new return type. Three call sites: chat RAG, memory retrieval, erasure. The index schema is also built in code in `background_tasks/AISeeding.cs` and becomes an EF migration. |
| Embeddings and chat | `AzureOpenAI` takes `OpenAIClient` from `AddAzureOpenAIClient` | The official OpenAI .NET client already speaks both Azure and api.openai.com. The change is endpoint, key, and deployment-name versus model-name. |
| Blobs | `BlobServiceClient` and `Storage` | An `IObjectStorage` with put, get, delete, and delete-by-prefix, implemented for Azure Blob now and R2 (S3) next |
| Chat queue | `IChatPersistenceQueue` already exists | A Postgres or TickerQ implementation beside `AzureStorageChatPersistenceQueue` |
| SQL | `UseSqlServer` / Aspire SQL Server integration | Npgsql. Existing migrations are SQL Server snapshots and should not be replayed on Postgres. |
| Feature flags config | Azure App Configuration in `FeatureFlags.cs` | LaunchDarkly is already the flag system. App Configuration can be skipped when the provider is absent. |
| Identity | Auth0 Management API, roles, webhook erasure | No change in the first migration |

User erasure already walks search documents, blobs, and the database (`UserErasure` DTOs, Auth0 webhook). Any new store has to be added to that path before production data moves.
