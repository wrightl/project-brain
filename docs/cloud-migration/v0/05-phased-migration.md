# Phased migration

Each phase leaves the product shippable on Azure. Production DNS moves once, in Phase 5. Do not start a phase until the exit criteria of the previous one are true.

Work is sequenced so the database and the index move while the API is still on Container Apps. That separates “Postgres is wrong” from “Fly is wrong”.

## Phase 0 — Measure and lock the decisions

No application code.

1. In Azure Cost Management, group last month by resource. Record Redis, SQL, Container Apps, the Container Apps environment, Log Analytics, Search, Cognitive Services, Storage, and bandwidth as separate lines.
2. On staging, after a sleep cycle, measure three timestamps: replica started, `/health` returned 200, first authenticated chat token streamed. The 90 second database warmup log line is the number that should collapse.
3. Close the decision table in the [readme](README.md): residency, Fly versus Hetzner, Neon versus on-box Postgres, staging always-on.
4. Create the new accounts (Fly or Hetzner, Neon, Cloudflare R2, OpenAI project) in the EU. Do not point production at them.

**Exit.** A one-page note in the decision log states the chosen compute and whether prompts stay on Azure OpenAI. Cost lines are attached.

## Phase 1 — Make providers replaceable, still on Azure

Code only. Production resources stay.

1. Change `ISearchIndexService` so it does not return Azure Search SDK types. Adapt `AzureSearchClient` and the test fake.
2. Add `IObjectStorage` and implement it with the current `BlobServiceClient`. Route `Storage`, upload endpoints, and blob erasure through it.
3. Add a Postgres implementation of `IChatPersistenceQueue` behind a config switch. Leave the Azure queue as the default.
4. Split `AddAzureOpenAI` into storage, search, and model registration as described in [technology](03-technology-and-stack.md).
5. Add `ProjectBrain.Api/Dockerfile`. The image should run the API the same way Container Apps does (URLs, `/health`, `/alive`).
6. Add an Npgsql dependency and a configuration switch. Default remains SQL Server.

**Exit.** Existing test suites pass. Staging on Azure still uses Blob, Search, SQL Server, and the Azure queue. A local run can boot the new API image.

## Phase 2 — Postgres and pgvector on staging

The staging API can stay on Container Apps for this phase so the only new variable is the database.

1. Provision Neon in the EU. Disable suspend for the staging branch used by the always-on test, or accept suspend only for a throwaway branch.
2. Generate the Postgres baseline migration from `AppDbContext`. Apply it with `ProjectBrain.MigrationService`.
3. Copy a staging snapshot from Azure SQL. Reset sequences. Diff row counts per table.
4. Run database integration tests against Neon.
5. Add `PgvectorSearchIndexService`. Create the extension and the HNSW index.
6. Re-embed staging documents with `text-embedding-3-small` at 1536 dimensions into pgvector. Do not try to export Azure AI Search’s internal vector format.
7. Point staging search reads and writes at pgvector. Keep Azure AI Search until a staging chat returns citations from Postgres.
8. Point staging `AppDbContext` at Neon. Watch the warmup log. It should succeed on the first attempt.

**Exit.** Staging chat, journal create, voice-note index, coach search, Stripe webhook idempotency, and user erasure succeed against Postgres. Azure SQL for staging is unused for a week.

## Phase 3 — Objects and the chat queue

1. Create the R2 bucket, EU jurisdiction, public access off.
2. Copy staging blobs. Preserve the path strings already stored in the database so rows do not need a rewrite. If paths must change, write them in the database in the same job as the copy.
3. Switch staging `IObjectStorage` to R2. Upload a coach file, download it, delete a user, confirm the prefix is gone.
4. Switch staging `IChatPersistenceQueue` to Postgres (or TickerQ). Confirm a chat turn still persists if the request ends before the write finishes.
5. Repeat the copy for production blobs only after the staging path has run. Production traffic still reads Azure Blob until Phase 5, so this phase’s production step is a verified copy, not the cutover.

**Exit.** Staging no longer calls Azure Storage. A restore test reads one object back from R2.

## Phase 4 — Models

1. Create an OpenAI project with a monthly budget alert. If the decision log says prompts stay in West Europe, skip this phase and keep `AddAzureOpenAIClient` pointed at the existing account.
2. On staging, set the chat model, the embedding model, and transcription to the OpenAI API. Keep 1536 dimensions.
3. Run a voice note through transcription, a journal summary, a coach chat with citations, and a tool-calling agent turn.
4. Compare a handful of stored embeddings: new query embeddings must hit the Phase 2 index. If the provider silently changes dimensions, retrieval will miss. Assert the dimension in the indexing code.

**Exit.** Staging generative calls succeed with the Azure OpenAI connection string removed from that environment. Token spend appears on the OpenAI project, not on the Cognitive Services account.

## Phase 5 — Compute cutover

Do staging first. Production is a DNS change after staging has been the only staging host for at least a few days.

1. Deploy Redis, API, and frontend with the mechanism in [deployment](04-deployment.md). `min` running instances is 1. Confirm a machine reboot comes back without a 90 second database retry.
2. Run migrations as a release command against Neon.
3. Attach staging domains. Update Auth0, Stripe, and webhook URLs for staging.
4. Exercise: sign-in, onboard, chat with a citation, upload a file, journal, voice note, coach geo search, Stripe test webhook, account deletion (erasure of rows, vectors, and R2 objects).
5. For production: lower DNS TTL, take a final Azure SQL backup, run the data copy again (or replicate the delta), deploy production apps, shift DNS, watch `/health` and the first real chat.
6. Leave the Azure Container Apps and Azure SQL in place, scaled down, for one release cycle. Rollback is DNS back to Azure plus the pre-cutover database backup if the new database was written to.

**Exit.** Production hostnames resolve to Fly or the Hetzner proxy. A fresh deploy does not call `azd`. The first request after an API restart is a normal process start, measured in the same way as Phase 0.

## Phase 6 — Remove Azure

After the rollback window:

1. Delete Container Apps, the environment, Azure SQL, AI Search, the storage account, Managed Redis, App Configuration, the Speech resource, and the Azure OpenAI account if Phase 4 moved models.
2. Remove `azure.yaml`, the Azure deploy workflow, and the sleep / wake / scale workflows.
3. Remove `Aspire.Hosting.Azure.*` from the AppHost. Keep Aspire for local containers.
4. Shorten `DatabaseStartupHostedService` once logs show no retry on the new database.
5. Delete the Azure federated credential from the GitHub organisation.

**Exit.** The monthly invoice for this product no longer contains those Azure resources. Local `dotnet run --project ProjectBrain.AppHost` still starts the API and the frontend.

## Phase 7 — After the move

Only if the new bill or the product needs it.

| Follow-up | When |
| --- | --- |
| Move off Auth0 | Auth0 is a larger line than compute and database combined, and the team accepts a user-visible re-login. |
| Dedicated vector service | Measured recall or latency on pgvector is not good enough after the HNSW index is tuned. |
| Second region | Users are far from `lhr` / `ams` and chat latency is dominated by geography rather than the model. |
| Hetzner if Fly was chosen, or the reverse | The invoice after Phase 6 misses the budget in decision 4. |
| Re-enable a non-production sleep | Never for the API that serves real users. A second preview app can stay off; that is a separate Fly app, not `minReplicas=0` on staging. |

## Risk register

| Risk | Mitigation |
| --- | --- |
| Postgres baseline misses a SQL Server default, collation, or cascade | Row-count diff and the database integration tests on Neon before staging traffic moves. |
| Identity sequences collide after the copy | Set each sequence to `max(id) + 1` as part of the copy job. |
| Search quality drops | Same embedding model and dimensions. Compare citation ids for a fixed set of staging questions before switching reads. |
| User erasure misses R2 or pgvector | Extend the existing erasure orchestrator in Phase 1–3 and run it in staging before production. |
| Next.js image still expects Azure-only env names | The frontend already takes `API_SERVER_URL`, Auth0, and LaunchDarkly from the environment. Map those in `fly.toml` or Kamal env. |
| .NET on a 512 MB machine restarts under load | Size the API machine at 1 GB, which matches the current 0.5 Gi request plus headroom. Watch restart counts during the staging soak. |
| OpenAI data processing is outside the EU | Decision 1. The fallback is to keep Azure OpenAI and still leave the rest of Azure. |
| Cutover writes split between two databases | Short write freeze, or run the app in read-only maintenance for the final copy. Do not dual-write SQL Server and Postgres for chat. |
| Rollback after users have written to Neon | Prefer forward fix. A DNS rollback to Azure loses writes made after the copy unless those writes are replayed. State that in the cutover note. |

## Suggested order of pull requests

These are implementation PRs for later work. This document set is the only change in the current branch.

1. Search and storage interfaces, API Dockerfile, no behaviour change.
2. Npgsql baseline and Postgres tests.
3. pgvector implementation, staging only.
4. R2 and the Postgres chat queue, staging only.
5. OpenAI provider switch, staging only.
6. Fly (or Kamal) config and the new GitHub workflow, staging hostnames.
7. Production cutover checklist and Azure deletion, after the soak.
