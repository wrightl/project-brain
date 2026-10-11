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

`ISearchIndexService` should return a project type (id, content, user id, resource id, score, blob path) instead of `SearchResults<SearchDocument>`. Call sites in chat retrieval and erasure then stay stable when the implementation changes.

## AI client in practice

`AddAzureOpenAI` registers search, the OpenAI client, blob storage, embedders, and erasure. Split that extension:

- `AddObjectStorage` — Azure or R2 from configuration.
- `AddSearchIndex` — Azure Search or pgvector from configuration.
- `AddGenerativeModel` — Azure OpenAI or OpenAI API from configuration.

Chat deployment names (`openai-chat-deployment`) are Azure concepts. The OpenAI API takes a model name (`gpt-5-mini`). Map them in configuration so call sites keep a single setting.

Transcription (`TranscribeAudio`) should call the audio transcriptions endpoint with the same file types the chat endpoint accepts today. Delete the Whisper Container Apps deployment and the Speech Bicep once staging has transcribed a voice note on the new path.

## Local development

Aspire stays the way developers start the stack:

- SQL Server container can remain until the Postgres flag is the default, then the AppHost should start Postgres with pgvector instead of SQL Server.
- Azurite can remain until `IObjectStorage` has a local filesystem or MinIO implementation.
- Redis container stays.
- The App Configuration emulator is optional once production does not require the store. LaunchDarkly already covers flags.

Developers should be able to run the API against Neon and R2 only when they are testing those integrations. The default local path stays on containers.
