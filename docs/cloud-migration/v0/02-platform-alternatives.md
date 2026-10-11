# Platform alternatives

Each row is a place the workload can run. The recommendation is the row that removes both the idle floor and the pause, without a new application runtime.

## Backend API and frontend

Both processes are long-running HTTP servers. The API also runs hosted services (database warmup, user activity, TickerQ). The frontend is a Next.js server because Auth0 sessions are handled by `@auth0/nextjs-auth0`. A static host cannot replace that Node server.

| Platform | Fit for this app | Cold start | Idle cost | Verdict |
| --- | --- | --- | --- | --- |
| **Fly.io Machines** | Docker for the API and the existing frontend image. One region close to the database (`lhr` or `ams`). Health checks map to `/alive` and `/health`. | Gone if `min_machines_running = 1`. A stopped machine still starts in seconds, which is the wrong mode for this product. | One shared-CPU machine per process. No separate environment fee and no Redis Enterprise SKU. | **Recommended compute.** |
| **Hetzner Cloud + Kamal** | One small EU instance runs API, Next.js, and Redis under Docker. Kamal deploys from GitHub Actions over SSH. | Gone. The process stays up. | Lowest bill. You operate the host, TLS, and restarts. | **Recommended when Fly is still more than the target bill.** |
| DigitalOcean App Platform or Droplet | App Platform is a managed build-and-run path. A Droplet is the same idea as Hetzner. | App Platform can sleep and then cold-start. A Droplet does not. | Droplet is cheap. App Platform adds a platform margin. | Use a Droplet only if the team already standardises on DigitalOcean. Otherwise Hetzner is the cheaper VPS. |
| Google Cloud Run | Container HTTP, scale to zero or minimum instances. | Minimum instances of 1 avoid the platform cold start. .NET startup remains. A paused Cloud SQL instance brings the same class of failure as Azure SQL. | Minimum instances recreate a floor. | Same tradeoff as Container Apps. |
| AWS App Runner or ECS Fargate | Containers, managed. | App Runner and Fargate are slow to scale from zero. | Fargate has a floor once tasks stay on. | Extra account and IAM surface for the same Docker workload Fly or a VPS already run. |
| AWS Lambda or Azure Functions | The API is a web host with SignalR, background jobs, and a 90 second startup helper. | .NET on Lambda is a worse cold start than Container Apps. | Low when idle. | Does not fit the process model. |
| Cloudflare Workers | Edge isolate. | Fast for Workers-sized code. | Low. | Requires a rewrite off ASP.NET. Out of scope. |
| Stay on Container Apps, replicas ≥ 1, SQL pause off | No migration. | Startup becomes normal .NET startup. | The bill goes up by the two apps plus a SQL database that is not allowed to pause, and Redis Enterprise remains. | Fails the cost goal. Use only as a short-term staging unblock, not as the target. |

Fly and Hetzner both satisfy the constraints. They differ in who patches the host.

- Fly: platform TLS, `flyctl` rollbacks, private networking between API, web, and Redis, no SSH fleet.
- Hetzner: one invoice line, full control, you own kernel updates, disk, and firewall. Kamal keeps deploys repeatable.

Do not split the API and the frontend across two vendors. They already share custom domains, Auth0 callback URLs, and the API base URL.

## Database

Azure SQL is the pause. The schema is ordinary relational data (users, conversations, journals, subscriptions, memory rows). Geo search does not need SQL Server spatial types. That makes Postgres a direct replacement and makes another SQL Server (RDS, a licensed VM) an ongoing licence cost.

| Database | Pause behaviour | Notes | Verdict |
| --- | --- | --- | --- |
| **Neon Postgres** | Can suspend compute. Production must set the minimum compute so it does not suspend. | Branching is useful for a migration dry run. EU region. pgvector is available. Connection pooling is built in (PgBouncer-compatible). | **Recommended with Fly.** |
| Supabase Postgres | The small paid compute sizes stay on. | Bundles Auth, Storage, and vectors. Taking the whole bundle means moving Auth0 and blobs too. | Use only the Postgres service if Neon is unavailable. Leave Supabase Auth and Storage unused. |
| Crunchy, Tembo, or RDS Postgres | Always on at the chosen size. | Heavier account setup. | Fine later if a compliance review wants a specific Postgres vendor. |
| Postgres on the Hetzner disk | Always on. | Backups are your job (daily `pg_dump` or WAL shipping). | **Recommended only in the Hetzner variant**, and only with tested backups. |
| Azure SQL kept awake | No pause if the tier is not serverless, or if auto-pause is disabled. | Keeps `nvarchar` / `datetime2` and the current migrations. | Keeps the SQL Server cost model the plan is leaving. |
| SQL Server on a VM | Always on. | Licence. | Rejected. |

EF Core is the data layer. The provider change is Npgsql. Generate a new initial migration from the current model for Postgres. Copy data once. Do not execute the existing SQL Server migration history against Postgres.

Point the API at Neon with the pooled connection string for the web app, and use the direct connection string for migrations.

## Auth

Auth0 is already outside Azure. The API calls the Management API (create user, patch, roles, token cache), verifies JWTs, and deletes users through an Auth0 webhook that erases search documents, blobs, and rows. The Next.js app uses the Auth0 session SDK.

| Provider | What changes | Verdict |
| --- | --- | --- |
| **Auth0, current tenant** | Callback URLs, audience, and webhook URL when the API domain moves. | **Keep.** |
| Clerk | New session model in Next.js, new Management-API equivalent, new role mapping, re-issued users or a password reset for every account. | Only if Auth0’s bill is larger than the engineering cost. It is a separate project after hosting is stable. |
| Supabase Auth | Same class of rewrite as Clerk, plus a second identity store beside the app’s `User` table. | Rejected for this migration. |
| Azure AD B2C or Entra External ID | Moves identity onto the platform being left, and still needs a user migration. | Rejected. |
| Self-hosted Keycloak | Another always-on service to patch and back up. | Rejected at this team size. |

## RAG index

The corpus is per-user documents (journals, voice-note transcripts, onboarding markdown, uploads) embedded at 1536 dimensions and filtered by user. Azure AI Search on the Free SKU is already at its limit; the next SKU is a monthly floor.

| Index | Cost shape | Application change | Verdict |
| --- | --- | --- | --- |
| **pgvector in the primary Postgres** | No second service. Storage sits with the rows that are already erased per user. | New implementation of search/index/delete. Query filters by `userId`. Re-embed or copy vectors. HNSW index. | **Recommended.** Matches the data size implied by a Free Search SKU. |
| Qdrant Cloud or Turbopuffer | Usage-based, still a second system and a second erasure path. | Smaller code change if the team wants a dedicated vector API. | Revisit only if pgvector latency or recall is measured as insufficient. |
| Pinecone | Monthly floor on paid plans, separate erasure. | Same as Qdrant. | Rejected while the corpus fits the Free Search tier. |
| Stay on Azure AI Search Free | $0 until the limits bind, then a SKU jump. | None. | Leaves an Azure dependency and a cliff. |
| OpenSearch or Elasticsearch | Cluster floor. | Large. | Rejected. |

User memory retrieval (`UserMemoryRetrievalService`, `UserMemoryIndexService`) uses the same embedding size and should use the same pgvector tables, with a source column to tell memory rows from document chunks.

## Generative AI

Chat, strategy suggestions, summaries, journal agents, goal suggestions, and embeddings all go through `AzureOpenAI` / `AgentOpenAIService`. Transcription uses the Whisper deployment on the same Cognitive Services account. The Speech resource is provisioned and unused by the API.

| Provider | Idle cost | Region | Verdict |
| --- | --- | --- | --- |
| **OpenAI API** | None beyond tokens. Same model names the app already requests (`gpt-5-mini`, `text-embedding-3-small`). Audio transcriptions replace the Whisper deployment. | Processing is not tied to West Europe. | **Recommended** when prompts may be processed by OpenAI. |
| Azure OpenAI kept in West Europe | Token charges, plus the Cognitive Services account. | Stays in the current region. | **Recommended only for a hard EU processing requirement.** The rest of Azure still goes. |
| Anthropic or another chat model | Token charges. | Depends on the vendor. | A product change (prompt and tool-call behaviour), not a hosting change. Do it after the provider switch is stable, if at all. |
| OpenRouter or a gateway | Extra hop and markup. | Varies by upstream. | Useful later for fallback models. Unnecessary for the first cutover. |
| Self-hosted weights on a GPU | The GPU is the floor, whether or not anyone is chatting. | You choose the region. | Rejected at the current app size. |

Embeddings must stay 1536 dimensions or every chunk must be re-embedded. Staying on `text-embedding-3-small` avoids a re-embed during the provider cutover. A later model change is a backfill job, not part of the hosting move.

## Files, cache, config, speech

| Concern | Recommendation | Why |
| --- | --- | --- |
| Blobs | Cloudflare R2, EU jurisdiction, S3 API | Private objects, no public access (same policy as `AllowBlobPublicAccess = false`). Egress to the API in the EU is the common case. Add the bucket to user erasure. |
| Queues | Stop using Azure Storage Queues | `IChatPersistenceQueue` gets a Postgres implementation. TickerQ already covers the other jobs. One less Azure client at startup. |
| Redis | Fly Machine or a container on the Hetzner host, private network only | Replaces Redis Enterprise. The app uses it as a cache (Auth0 role cache, webhook idempotency, Aspire Redis client), which does not need an enterprise cluster. |
| App Configuration | Fly secrets and GitHub environment secrets | LaunchDarkly remains the flag service. The Free App Configuration store is not worth a client on the request path. |
| Speech | Remove the Bicep resource after transcription is confirmed on the OpenAI audio API | The API does not read `ConnectionStrings__speech`. |
| Observability | Keep Grafana Cloud OTLP | The API already exports OTLP when the endpoint is set. Fly or the VPS only need the same environment variables. |

## Target picture

```text
GitHub Actions
  -> docker build (API, frontend)
  -> flyctl deploy
       Fly app: projectbrain-api     (shared CPU, min 1, /health, /alive)
       Fly app: projectbrain-web      (existing Next.js image, min 1)
       Fly app: projectbrain-redis    (private)
  -> Neon Postgres (EU, pgvector, no suspend in production)
  -> Cloudflare R2 (EU)
  -> OpenAI API (chat, embeddings, transcription)
  -> Auth0, Stripe, Mailgun, Firebase, LaunchDarkly, Google Maps, Grafana
```

Hetzner variant: replace the three Fly apps with one Compose/Kamal host. Neon can stay, or Postgres can move onto that host if a managed database is declined in the decision log.
