# Technology, stack, and frameworks

The application stack stays. The hosting stack and two libraries change.

## Keep

| Layer | Current | Decision |
| --- | --- | --- |
| API | ASP.NET Core, .NET 10, minimal APIs | Keep. A rewrite to Node or Python would redo every endpoint, auth policy, and test for a cost problem that is in Azure SKUs. |
| UI | Next.js 15, React, Tailwind, `@auth0/nextjs-auth0` | Keep. It already has a production Dockerfile. |
| ORM | Entity Framework Core | Keep. Change the provider from SQL Server to Npgsql. |
| Jobs | TickerQ | Keep. Extend it (or the existing queue interface) so chat persistence does not need Azure Queues. |
| Identity | Auth0 | Keep. |
| Payments, mail, flags, maps, push, traces | Stripe, Mailgun, LaunchDarkly, Google Maps, Firebase, OpenTelemetry to Grafana | Keep. |
| Local orchestration | Aspire AppHost | Keep for local SQL, Redis, Azurite, and the npm dev server. Stop using it as the production provisioner. |
| Tests | xUnit, the API integration factory, Jest | Keep. The integration factory already swaps `ISearchIndexService` and `BlobServiceClient`. New fakes should plug into that factory. |

## Change

| Layer | From | To | Why a framework change is enough |
| --- | --- | --- | --- |
| Database provider | `Aspire.Microsoft.EntityFrameworkCore.SqlServer` | `Npgsql.EntityFrameworkCore.PostgreSQL` plus `Pgvector.EntityFrameworkCore` | The model is portable. Migrations are not. |
| Vector store | `Azure.Search.Documents` inside `AzureSearchClient` | pgvector columns and an HNSW index | Removes the Search service. |
| Model client | `AddAzureOpenAIClient` (Aspire Azure hosting) | `OpenAIClient` pointed at `https://api.openai.com/v1`, or left on the Azure endpoint if residency requires it | The .NET OpenAI SDK supports both. `AzureOpenAI` can stay as a class name until a later rename. |
| Object storage | `Azure.Storage.Blobs` | `AWSSDK.S3` (or the Cloudflare R2-compatible settings of that SDK) behind a small `IObjectStorage` | R2 speaks S3. The rest of the API should stop taking `BlobServiceClient`. |
| Cache | Aspire Redis client against Redis Enterprise | Same StackExchange Redis / Aspire Redis client against a private Redis | No framework change. |
| API packaging | `AddProject` published by Aspire into Container Apps | A Dockerfile for `ProjectBrain.Api` | Fly and Kamal both deploy images. The frontend Dockerfile already exists. |
| Production provisioner | `azd` and the Aspire Azure hosting packages | `flyctl` or Kamal | Aspire’s Azure packages remain only if local emulators still need them. |

## Do not adopt

| Option | Reason |
| --- | --- |
| Rewrite the API on Node, Bun, or Python | The slow path is SQL resume and replica start, which a new language does not remove. |
| Move the frontend to Cloudflare Pages or a static export | Auth0’s Next.js SDK needs a server. |
| Replace EF with Dapper or a second ORM during the move | Provider swap and a data copy are already the risky step. |
| Introduce a separate vector database framework before measuring pgvector | The Free Search SKU is evidence the corpus is small. |
| Replace TickerQ with a new broker (Service Bus, SQS, RabbitMQ) | Another process to run. The queue in this app is a buffer for chat writes, not a multi-service bus. |
| Self-host Aspire dashboard or `azd` on the new host | Aspire remains a developer tool. |

## Database move in practice

1. Add the Npgsql packages to `ProjectBrain.Database` and switch `AddSqlServerDbContext` behind a configuration flag so CI can still run SQL Server tests until Postgres tests exist.
2. From the current `AppDbContext` model, generate one Postgres baseline migration. Archive the SQL Server migration set. Do not try to translate each `datetime2` / `nvarchar` / identity migration by hand.
3. Copy data with a one-off tool (pgloader, or a small EF program that reads SQL Server and writes Postgres). Sequences must be set above the current identity values.
4. Run the existing database integration tests against Postgres. Coach distance search is LINQ over floats and should behave the same.
5. Enable the `vector` extension. Store chunks as `vector(1536)` with `user_id`, `resource_id`, `source`, and the text used for citations.

### Case sensitivity is a correctness change, not a detail

SQL Server databases are case-insensitive by default. Postgres is case-sensitive. `UserRepository` looks users up with `u.Email == email`, while seeding code normalises with `.ToLower()` first, so behaviour is already inconsistent. After the move, a sign-in with `User@example.com` can fail to match a stored `user@example.com`.

Pick one approach and cover it with tests before any data moves:

- Normalise email to lower case at every boundary and add a `lower(email)` unique index. Most explicit, and it makes the existing inconsistency visible.
- Use `citext` for email columns. Least code change; adds an extension dependency.
- Use a nondeterministic ICU collation on the column. Avoid: it breaks `LIKE` and pattern matching on that column.

Audit other string comparisons at the same time, including the coach `Country.Contains(country)` filter and tag lookups.

### Migrations are not covered by CI today

`DatabaseIntegrationTests` starts a SQL Server Testcontainer and calls `EnsureCreatedAsync()`, so the existing migration set is never applied in CI. A Postgres baseline therefore needs its own verification: apply migrations to an empty database and compare the result against the model. Switch the test container to a pgvector-enabled Postgres image so vector behaviour is covered too.

### The search abstraction needs a query model, not a new return type

The interface returns Azure types *and* callers build Azure `SearchOptions`, OData filter strings such as `ownerId eq '{userId}' or ownerId eq '' or ownerId eq null`, a `Select` field list, and `VectorSearchOptions`, then read results with `SearchDocument.GetString(...)`. Replacing the provider means introducing a project-owned query (filters, top-k, fields, query vector, optional keyword text) and result type, then updating three call sites: chat RAG, memory retrieval, and erasure.

The index schema is built in application code in `background_tasks/AISeeding.cs`, including the HNSW configuration, the semantic configuration, and the 1536-dimension vector field. On Postgres that becomes an EF migration plus an index strategy, so this file is rewritten rather than swapped.

Retrieval parity differs by path:

- **Chat RAG** is vector nearest-neighbour with a filter. pgvector matches it.
- **User memory** requests the hosted semantic ranker and silently falls back to a SQL `LIKE` search on any failure. Check whether that fallback is already firing in production. If it is, pgvector is an upgrade. If it is not, matching it needs a `tsvector` index alongside the vector index with reciprocal rank fusion, and there is still no hosted reranker equivalent.

Erasure gets simpler: the current implementation pages through documents 1,000 at a time, while Postgres needs one `DELETE ... WHERE user_id = @id`.

## AI client in practice

`AddAzureOpenAI` registers search, the OpenAI client, blob storage, embedders, and erasure. Split that extension:

- `AddObjectStorage` — Azure or R2 from configuration.
- `AddSearchIndex` — Azure Search or pgvector from configuration.
- `AddGenerativeModel` — Azure OpenAI or OpenAI API from configuration.

Chat deployment names (`openai-chat-deployment`) are Azure concepts. The OpenAI API takes a model name (`gpt-5-mini`). Map them in configuration so call sites keep a single setting.

Transcription (`TranscribeAudio`) should call the audio transcriptions endpoint with the same file types the chat endpoint accepts today. Check the provider's upload limit (25 MB on the OpenAI audio endpoint at the time of writing) against the voice-note limits enforced in `Chat.cs` and `VoiceNotes.cs` before switching. Delete the Whisper deployment once staging has transcribed a voice note on the new path. The Speech Bicep resource can go immediately: nothing reads `ConnectionStrings__speech`.

## Concurrency has to be decided, not inherited

`AddSignalR()` has no backplane, the rate limiter is in-process, `UserActivityBackgroundService` is a singleton hosted service, and `app.UseTickerQ()` starts the job processor in every instance. Nothing sets a maximum replica count today, so production can already run more than one instance with all four assumptions broken.

Choose explicitly as part of the move:

- **One instance.** Set the maximum to 1. Nothing else changes. Deploys and host events are a visible gap.
- **More than one.** Add `AddStackExchangeRedis` to SignalR, verify TickerQ 10.4's distributed locking so jobs do not run twice, and accept a per-instance rate limit (or move it to a shared store).

## Running outside Aspire

Aspire injects configuration by connection name. Outside Aspire every one of these must be provided explicitly: `ConnectionStrings__projectbraindb`, `ConnectionStrings__azurecache`, `ConnectionStrings__blobs`, `ConnectionStrings__queues`, the `ai-search` and `openai` client connection strings, plus `Auth0__*`, `Mailgun__*`, `Firebase__CredentialsJson`, `LaunchDarkly__SdkKey`, `GoogleMaps__GeocodingApiKey`, `Stripe__WebhookSecret`, and the `OTEL_*` variables. `AddServiceDefaults` also registers service discovery and standard resilience for `HttpClient`s; that keeps working, but audit for any code that depends on Aspire-injected `services__*` names before the first deploy.

## Local development

Aspire stays the way developers start the stack:

- SQL Server container can remain until the Postgres flag is the default, then the AppHost should start Postgres with pgvector instead of SQL Server.
- Azurite can remain until `IObjectStorage` has a local filesystem or MinIO implementation.
- Redis container stays.
- The App Configuration emulator is optional once production does not require the store. LaunchDarkly already covers flags.

Developers should be able to run the API against Neon and R2 only when they are testing those integrations. The default local path stays on containers.
