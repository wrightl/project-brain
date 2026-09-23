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

Retrieval uses 1536-dimension embeddings (`EmbeddingGenerationOptions.Dimensions = 1536`) stored in Azure AI Search. Indexing, chat RAG, user-memory indexing, and user erasure all go through `ISearchIndexService` / `AzureSearchClient`. That interface currently returns Azure Search SDK types (`SearchResults<SearchDocument>`), so the provider is not actually swappable without a small contract change.

Background work is mostly TickerQ (journals, voice notes, goals, coping strategies, agent follow-up). Chat persistence is the exception: it uses an Azure Storage Queue.

## Why startup is slow

Staging is put to sleep on purpose.

- `.github/workflows/sleep-staging.yml` scales `api` and `frontend` to `minReplicas=0` “so Azure SQL can auto-pause”.
- `.github/workflows/scale-staging-container-apps.yml` runs nightly and on deploy, and forces the same zero.
- `wake-staging.yml` is the manual path back to one replica.

The first request after sleep pays for all of the following:

1. Container Apps creates a replica and pulls the image.
2. The .NET process starts, builds the host, and connects to Redis, Storage, Search, and OpenAI.
3. Azure SQL serverless may answer with a resume error. `DatabaseStartupHostedService` treats that as retryable and will keep trying for 90 seconds.
4. Readiness is `GET /health` with a 5 second timeout and 6 failures. Liveness is `GET /alive`. A database that is still resuming fails readiness for a meaningful part of a minute.
5. Only after that does the app serve chat, which then calls Azure OpenAI and Azure AI Search.

Production uses the same Container Apps and Azure SQL settings. Any `minReplicas` of 0, or any SQL auto-pause, reproduces this path.

## Why the bill stays high while the app is idle

Scale-to-zero removes compute for the two apps. It does not remove the resources that have a monthly floor or that exist so the apps can wake:

| Resource | Idle behaviour | Why it shows up on the bill |
| --- | --- | --- |
| Azure Managed Redis | Stays provisioned. HA is already disabled. | Redis Enterprise is priced as a cluster, not as a request. It is a poor cache for an app that is allowed to sleep. |
| Azure SQL free offer with `BillOverUsage` | Pauses when idle, which causes the 40613 warmup. | Crossing the free limit bills. Leaving it running so it does not pause also bills. |
| Container Apps environment | Present even when replica count is 0 | Environment and log ingestion can cost when replicas are awake. Two always-on apps at 0.25 vCPU are the “fast” configuration and the more expensive one. |
| Azure AI Search | Default SKU is Free (capacity and index limits). | Moving off Free to a paid SKU is a step-change in cost. The Free tier is also a reason to leave Search rather than upgrade it. |
| Azure OpenAI | GlobalStandard for chat and embeddings is pay per token. Whisper is a Standard deployment. | Idle token cost should be near zero. The account still sits on the critical path for every cold start and for `azd provision`. |
| Azure AI Speech | Provisioned from Bicep. The API connection string is commented out. | A resource that the running API does not use. |
| App Configuration | Free SKU | Unlikely to be a cost problem. It is still an Azure client the API must configure at startup. |
| Storage account | Small, private | Usually cheap. Queues and blobs are the coupling, not the price. |

Confirm the split in Azure Cost Management grouped by resource for one full month before choosing Fly versus Hetzner. The plan assumes Redis, SQL, and the Container Apps environment dominate the idle bill, and that model tokens dominate the variable bill. If the invoice shows something else (Log Analytics, bandwidth, or a paid Search SKU), adjust Phase 0 and do not change the target shape until that line is explained.

## Coupling that the migration has to break

These are the code boundaries that later phases change. Hosting can move only after the API can be pointed at the new services with configuration.

| Boundary | Current type | Replacement needs |
| --- | --- | --- |
| Search | `ISearchIndexService.SearchAsync` returns `Response<SearchResults<SearchDocument>>` | A project-owned result type so pgvector can implement the same interface |
| Embeddings and chat | `AzureOpenAI` takes `OpenAIClient` from `AddAzureOpenAIClient` | The official OpenAI .NET client already speaks both Azure and api.openai.com. The change is endpoint, key, and deployment-name versus model-name. |
| Blobs | `BlobServiceClient` and `Storage` | An `IObjectStorage` with put, get, delete, and delete-by-prefix, implemented for Azure Blob now and R2 (S3) next |
| Chat queue | `IChatPersistenceQueue` already exists | A Postgres or TickerQ implementation beside `AzureStorageChatPersistenceQueue` |
| SQL | `UseSqlServer` / Aspire SQL Server integration | Npgsql. Existing migrations are SQL Server snapshots and should not be replayed on Postgres. |
| Feature flags config | Azure App Configuration in `FeatureFlags.cs` | LaunchDarkly is already the flag system. App Configuration can be skipped when the provider is absent. |
| Identity | Auth0 Management API, roles, webhook erasure | No change in the first migration |

User erasure already walks search documents, blobs, and the database (`UserErasure` DTOs, Auth0 webhook). Any new store has to be added to that path before production data moves.
