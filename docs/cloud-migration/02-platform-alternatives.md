# Platform alternatives

All prices are list prices for EU regions checked in September 2026, excluding model tokens, bandwidth, and VAT. They are here to size a decision, not to quote a bill. Verify each in the vendor calculator, and verify the current Azure spend in Cost Management grouped by resource.

Read [the review](06-plan-review.md) alongside this document. The first version of this comparison overstated Azure's idle cost and understated the cost of leaving.

## Backend API and frontend

Both processes are long-running HTTP servers. The API also runs hosted services (database warmup, user activity, TickerQ) and a SignalR hub. The frontend is a Next.js server because Auth0 sessions are handled by `@auth0/nextjs-auth0`, so a static host cannot replace it.

| Platform | Fit | Cold start | Cost | Verdict |
| --- | --- | --- | --- | --- |
| **Container Apps, kept warm** | No migration. `minReplicas=1` and stop the database pausing. | Gone, once the database does not pause. | Idle replicas bill at roughly a tenth of the active vCPU rate, and 180,000 vCPU-seconds plus 360,000 GiB-seconds per subscription per month are free. One warm 0.25 vCPU / 0.5 GiB replica is roughly **$4 per month**, so about **$8 for both apps**. | **Do this first.** It is the cheapest fix for the stated problem, and it keeps rolling revisions. |
| **Fly.io Machines** | Docker for the API and the existing frontend image, EU region next to the database. Health checks map to `/alive` and `/health`. WebSockets for the SignalR hub are supported. | Gone with `min_machines_running = 1`. | `shared-cpu-1x` with 1 GB in Amsterdam is about **$5.92**; 512 MB about **$3.32**; 256 MB about **$2.02**. API plus web plus Redis is roughly **$11**. Prices rose on 1 October 2026 (additional RAM $5 → $6 per GB). | **Default if the team leaves Azure.** Managed TLS, `flyctl` rollbacks, private networking. |
| **One VPS with Kamal** | One EU instance runs API, Next.js, and Redis under Docker; Kamal deploys over SSH from GitHub Actions. | Gone. | Hetzner raised prices twice in 2026. The cost-optimized CX23 class is about **€5.99** but was reported unavailable in September 2026; the regular CPX22 equivalent is about **€19.49**, plus €0.50 for IPv4. **Price this on the day you buy it.** | Viable, and the only route to a materially lower bill (when the database is on the same box). You patch the host, own TLS renewal, and have no redundancy. |
| DigitalOcean, Vultr, Linode droplet | Same shape as the VPS option. | Gone. | Around $5–6 for 1 GB, more per GB than Hetzner. | Use if the team already standardises there. |
| Google Cloud Run | Container HTTP with minimum instances. | Minimum instances remove the platform cold start; a paused Cloud SQL brings back the same class of problem. | Comparable to Container Apps. | No advantage over fixing Container Apps. |
| AWS App Runner, ECS Fargate | Containers, managed. | Slow from zero; a floor once tasks stay up. | Higher than Fly for this size. | Extra account and IAM surface for the same Docker workload. |
| AWS Lambda, Azure Functions | The API is a web host with SignalR, hosted services, and a background warmup. | Worse .NET cold start. | Low when idle. | Does not fit the process model. |
| Cloudflare Workers | Edge isolate. | Fast. | Low. | Requires a rewrite off ASP.NET. Out of scope. |

### Availability is part of this choice

Container Apps gives rolling revisions by default. One Fly machine or one VPS does not: deploys and host maintenance are a visible gap. Because `AddSignalR()` has no backplane and TickerQ starts in every instance, running two instances is not free either — see [the concurrency section](01-current-architecture.md#concurrency-assumptions-baked-into-the-current-code). Decide one of:

- **One instance.** Simplest and cheapest. Accept seconds-to-a-minute of downtime on deploy. Set the maximum instance count to 1 so nothing scales out by surprise.
- **Two or more instances.** Add `AddStackExchangeRedis` to SignalR, confirm TickerQ's distributed locking, and accept that the in-process rate limiter becomes per instance.

## Database

Azure SQL's auto-pause is the cold start, but the database is also almost certainly free today: the free offer gives 100,000 vCore-seconds, 32 GB data, and 32 GB backup per database per month, and `AppHost.cs` enables it. Always-on at the 0.5 vCore serverless minimum would allocate roughly thirteen times that allowance at General Purpose serverless rates, so **never simply disable auto-pause on the current tier**.

The schema is ordinary relational data. Coach geo search is an application-side bounding box over latitude and longitude, with no SQL Server spatial types, so Postgres is a straight swap at the model level.

| Option | Pause | Cost | Verdict |
| --- | --- | --- | --- |
| **Azure SQL on a DTU tier (Basic or S0)** | Never pauses | Small fixed monthly price; confirm current list price for the region. Basic caps at 2 GB; S0 is 10 DTU and may be too slow under chat load. Converting off the free offer is **one-way**. | **The quick fix.** Removes the cold start without touching code. |
| **Neon Postgres** | Suspends after 5 minutes by default; can be disabled on a paid plan | Launch is usage-based with no minimum: **$0.106 per CU-hour** and **$0.35 per GB-month**. Always-on at 0.25 CU is about **$19.72 per month**. | **Recommended managed Postgres** if the team leaves Azure and does not want to own backups. pgvector available, EU regions, built-in pooling, branches for migration rehearsal. |
| **Postgres on the same VPS** | Never pauses | Effectively free once the server is paid for | The only genuinely cheaper option. Requires continuous archiving (for example WAL-G to R2), a rehearsed restore, and acceptance that one machine holds health-adjacent data. |
| Supabase Postgres | Paid compute stays on | Comparable to Neon | Use the Postgres service only. Taking Auth and Storage too would mean moving Auth0 and blobs in the same project. |
| Crunchy, Tembo, RDS Postgres | Always on | Higher floor | Fine if a compliance review wants a named vendor. |
| Azure SQL serverless with auto-pause disabled | Never pauses | Three figures per month at the 0.5 vCore minimum | Rejected. This is the trap. |
| SQL Server on a VM | Never pauses | Licence | Rejected. |

EF Core is the data layer, so the provider change is Npgsql plus a new baseline migration. Use Neon's pooled connection string for the app and the direct one for migrations.

Two portability items the first version glossed over, both covered in [technology](03-technology-and-stack.md): **case sensitivity** (SQL Server is case-insensitive by default, Postgres is not, and `UserRepository` compares `u.Email == email`) and the fact that the current integration tests call `EnsureCreatedAsync()` rather than applying migrations, so the migration set is not verified by CI today.

## Auth

Auth0 is already outside Azure. The API calls the Management API (create, patch, roles, token cache), validates JWTs, and erases users through a webhook that clears search documents, blobs, and rows. The frontend uses the Auth0 session SDK.

| Provider | What changes | Verdict |
| --- | --- | --- |
| **Auth0, current tenant** | Callback URLs, audience, and webhook URL when the API hostname moves. | **Keep.** |
| Clerk | New session handling in Next.js, a new Management API equivalent, new role mapping, and a user migration with password resets. | Only if Auth0's bill exceeds the engineering cost. Separate project. |
| Supabase Auth | Same scale of rewrite, plus a second identity store beside the app's `User` table. | Rejected here. |
| Entra External ID | Moves identity onto the platform being left, and still needs a user migration. | Rejected. |
| Self-hosted Keycloak | Another always-on service to patch and back up. | Rejected at this team size. |

## RAG index

This is the strongest technical reason to move, independent of hosting. Azure AI Search Free allows **50 MB of storage and 3 indexes**. Each chunk carries a 1536-dimension vector at roughly 6 KB before content, so a few thousand chunks exhaust it, and the next tier is a new monthly line. The semantic ranker that `UserMemoryRetrievalService` requests is also restricted on Free.

| Index | Cost | Application change | Verdict |
| --- | --- | --- | --- |
| **pgvector in the primary Postgres** | No second service; storage sits with the rows that erasure already deletes | New implementation of search, index, and delete; an HNSW index; a provider-neutral query model. Chat RAG maps directly. Memory retrieval needs `tsvector` plus reciprocal rank fusion to approximate hybrid search, and there is no hosted reranker equivalent. | **Recommended.** Removes the ceiling and one whole service. Confirm first whether the semantic path is already falling back to SQL. |
| Azure AI Search Basic or higher | A new fixed monthly line | None | The do-nothing option when the Free tier fills. Keeps parity, including the reranker. |
| Qdrant Cloud, Turbopuffer | Usage-based, second system, second erasure path | Smaller change than pgvector if a dedicated vector API is wanted | Revisit only if pgvector recall or latency is measured as insufficient. |
| Pinecone | Monthly floor on paid plans | Same as Qdrant | Rejected while the corpus fits a Free tier. |
| OpenSearch, Elasticsearch | Cluster floor | Large | Rejected. |

Add a reranking service (for example a hosted rerank API) only if measurement shows hybrid plus fusion is not good enough. Do not add it speculatively.

User memory indexing and retrieval use the same embedding size and should share the pgvector tables, with a source column distinguishing memory rows from document chunks.

## Generative AI

Chat, summaries, journal agents, goal suggestions, strategy suggestions, and embeddings all go through `AzureOpenAI` / `AgentOpenAIService`. Transcription uses the Whisper deployment on the same account. The Speech resource is provisioned and unused.

| Provider | Idle cost | Verdict |
| --- | --- | --- |
| **Azure OpenAI, West Europe (current)** | None beyond tokens. GlobalStandard is pay per token. | **Keep unless the goal is to close the Azure account.** There is no idle saving to win here, and the processing boundary is already documented. |
| OpenAI API | None beyond tokens. Same model families (`gpt-5-mini`, `text-embedding-3-small`), audio transcriptions instead of a Whisper deployment. | Optional. Do it to consolidate vendors, and only after confirming model identifiers, the 25 MB transcription upload limit against the app's voice-note limits, and zero-retention terms in writing. |
| Anthropic or another chat model | Tokens | A product change in prompt and tool-call behaviour, not a hosting change. Separate exercise. |
| Gateway such as OpenRouter | Extra hop and margin | Useful later for fallback models. Unnecessary now. |
| Self-hosted weights on a GPU | The GPU is the floor | Rejected at this size. |

Embeddings must stay at 1536 dimensions or every chunk needs re-embedding. Assert the dimension in the indexing code so a provider default cannot silently break retrieval.

## Files, cache, config, speech

| Concern | Recommendation | Why |
| --- | --- | --- |
| Blobs | Cloudflare R2, EU jurisdiction, S3 API, private objects | Matches the current `AllowBlobPublicAccess = false` policy, and there is no egress charge. Add the bucket to user erasure before production data moves. |
| Queues | Stop using Azure Storage Queues | `IChatPersistenceQueue` gets a Postgres implementation; TickerQ already covers other jobs. One less client on the startup path. |
| Redis | Keep Redis, move it next to the app on the private network | It is genuinely used for the distributed cache, user activity, and **webhook idempotency**, which must survive restarts — so plain in-memory caching is not a safe substitute for Stripe and Auth0 webhooks. Azure Managed Redis B0 is about $11.68 per month; a 256 MB Fly machine or a container on the VPS replaces it for a couple of dollars. |
| App Configuration | Host secrets plus GitHub environment secrets | LaunchDarkly is the flag service. Confirm `FeatureFlags.cs` tolerates the provider being absent before removing the store. |
| Speech | Delete the Bicep resource | The API never reads `ConnectionStrings__speech`. Free win. |
| Observability | Keep Grafana Cloud OTLP | `AddServiceDefaults` exports OTLP when the endpoint is set; the new host only needs the same variables. |

## Target picture

```text
GitHub Actions
  -> docker build (API, and one web image per environment)
  -> flyctl deploy (or kamal deploy)
       API      (always on, min 1, max 1 unless a SignalR backplane is added)
       Web      (always on, image built per environment)
       Redis    (private network only)
  -> Postgres with pgvector (Neon in the EU, suspend disabled; or on the VPS with WAL archiving)
  -> Cloudflare R2 (EU)
  -> Azure OpenAI in West Europe, or the OpenAI API
  -> Auth0, Stripe, Mailgun, Firebase, LaunchDarkly, Google Maps, Grafana
```
