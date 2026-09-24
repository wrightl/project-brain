# Phased migration

Each phase leaves the product shippable on Azure. Production DNS moves once, in Phase 7. Do not start a phase until the previous one's exit criteria are true.

The order changed after [the review](06-plan-review.md). The cold start is now fixed in place, on Azure, in Phase 1 — before any migration work — because it is cheap and reversible. The migration itself sits behind an explicit decision in Phase 2 and is justified by the Azure AI Search 50 MB ceiling and store consolidation, not by an assumed cost saving.

| Phase | What it does | Can it be skipped? |
| --- | --- | --- |
| 0 | Measure | No |
| 1 | Remove the cold start on Azure | No. Do this even if you migrate. |
| 2 | Decide whether to migrate at all | No |
| 3 | Make providers replaceable, still on Azure | No, if migrating |
| 4 | Postgres and pgvector on staging | No, if migrating |
| 5 | Objects and the chat queue | No, if migrating |
| 6 | Models | **Yes.** Optional, and not a cost saving. |
| 7 | Compute cutover | No, if migrating |
| 8 | Decommission Azure | No, if migrating |
| 9 | Later work | Yes |

## Phase 0 — Measure and lock the decisions

No application code.

1. In Azure Cost Management, group last month by resource. Record Container Apps, the environment, Log Analytics, SQL, Search, Managed Redis, Cognitive Services, Storage, and bandwidth as separate lines. Every figure in these documents is a list price, not your bill.
2. Check whether the free-offer database ever exceeds 100,000 vCore-seconds in a month (the “Free amount remaining” metric). If it does, Azure SQL is already billing and the case for migrating strengthens.
3. **Measure the index size against the Azure AI Search Free 50 MB limit.** That number sets the deadline for the whole migration.
4. **Search production logs for “Hybrid memory search failed”.** If it is firing, memory retrieval is already degraded to a SQL `LIKE` search, which changes what pgvector has to match.
5. On staging, after a sleep cycle, time three things: replica started, `/health` returned 200, and the **first authenticated request that reads data** completed. The third is the one users feel; `/health` excludes database checks, so it returns early.
6. Answer decisions 1 to 4 in the [readme](README.md).

**Exit.** A one-page note records the per-resource bill, the search index size, whether the semantic fallback is firing, and the measured first-data-request time.

## Phase 1 — Remove the cold start on Azure

Infrastructure and configuration only. No data migration, no new vendor. This is the fix for the stated problem.

1. Set `minReplicas=1` for the API and the frontend in staging and production. Retire `sleep-staging.yml`, `wake-staging.yml`, and `scale-staging-container-apps.yml`, or reduce them to a manual tool. Warm idle replicas bill at roughly a tenth of the active vCPU rate and most of it falls inside the monthly free grant.
2. Set a **maximum** replica count of 1, which the AppHost does not currently set. This makes the existing single-instance assumptions (SignalR without a backplane, in-process rate limiting, TickerQ in every instance) explicit and safe rather than latent.
3. Stop the database pausing. **Do not simply disable auto-pause on the serverless free offer** — at the 0.5 vCore minimum that allocates roughly thirteen times the free allowance at General Purpose serverless rates. Move the database to a DTU tier (Basic or S0) that does not pause, after checking the size cap and the DTU ceiling against real load. Note that converting off the free offer is one-way.
4. Delete the unused Azure AI Speech resource and its Bicep template. Nothing reads `ConnectionStrings__speech`.
5. Re-run the Phase 0 timings. The first-data-request time should now be normal application latency.
6. Watch the next invoice for a month.

**Exit.** No workflow scales anything to zero. The first request after a deploy or restart does not wait on a database resume. The new monthly bill is known.

If the bill at this point is acceptable and the search index is far from 50 MB, stop here and revisit later. That is a legitimate outcome of this plan.

## Phase 2 — Decide whether to migrate

A decision, not an implementation. Write the answer down.

Migrate if any of these hold:

- The Azure AI Search index is near the 50 MB Free limit, and a paid Search tier is unwanted.
- Running three data stores (SQL, Search, Blob) instead of one Postgres is a maintenance cost the team wants gone.
- The team wants off Azure for reasons beyond this document (tooling, `azd`, provisioning time, vendor preference).
- The Phase 1 bill is still unacceptable *and* the team will run Postgres on its own host, which is the only option that clearly beats Phase 1 on cost.

Do not migrate on the basis that Fly or Hetzner plus managed Postgres is cheaper than a fixed Azure setup. On current list prices it is roughly a wash.

**Exit.** A recorded decision naming the destination, the availability target, and whether the team owns Postgres backups.

## Phase 3 — Make providers replaceable, still on Azure

Code only. Production resources stay. Every item here is useful even if the migration is later abandoned.

1. Replace the search contract with a project-owned query and result model (filters, top-k, fields, query vector, optional keyword text). Adapt `AzureSearchClient` and the integration-test fake. Three call sites change: chat RAG, memory retrieval, erasure.
2. Add `IObjectStorage` and implement it over the current `BlobServiceClient`. Route `Storage`, upload endpoints, and blob erasure through it.
3. Add a Postgres implementation of `IChatPersistenceQueue` behind a config switch. Leave the Azure queue as the default.
4. Split `AddAzureOpenAI` into storage, search, and model registration as described in [technology](03-technology-and-stack.md).
5. Add `ProjectBrain.Api/Dockerfile` that runs the API the same way Container Apps does.
6. Add Npgsql behind a configuration switch. Default stays SQL Server.
7. Decide and implement the case-sensitivity strategy for email and other string lookups, with tests, before any data moves.

**Exit.** Existing suites pass. Staging on Azure still uses Blob, Search, SQL Server, and the Azure queue. The new API image boots locally.

## Phase 4 — Postgres and pgvector on staging

Keep the staging API on Container Apps so the database is the only new variable.

1. Provision Postgres: Neon in the EU with scale-to-zero disabled, or Postgres on the chosen host with continuous archiving. Disabling Neon's scale-to-zero requires a paid plan; the free plan re-creates the pause this project exists to remove.
2. Generate the Postgres baseline migration from `AppDbContext` and apply it with `ProjectBrain.MigrationService`.
3. Verify migrations properly. The current integration tests call `EnsureCreatedAsync()`, so they do not exercise the migration set. Apply migrations to an empty database and compare against the model. Switch the test container to a pgvector-enabled Postgres image.
4. Copy a staging snapshot. Reset sequences past the maximum identity values. Diff row counts per table.
5. Run the database integration tests against Postgres, including the case-sensitivity tests from Phase 3.
6. Add the pgvector search implementation: `vector(1536)` chunks with `user_id`, `resource_id`, `source`, and citation text, plus an HNSW index. Replace the schema-creation code in `background_tasks/AISeeding.cs` with a migration.
7. If the semantic fallback is **not** already firing in production, add hybrid retrieval (`tsvector` alongside the vector index, fused with reciprocal rank fusion) before switching memory retrieval. If it **is** firing, plain vector search is already an improvement; record that decision.
8. Re-embed staging content at 1536 dimensions. Do not attempt to export Azure AI Search's internal vector format. Assert the dimension in the indexing code.
9. Compare retrieval quality on a fixed set of staging questions before switching reads: same questions, compare returned citation ids.
10. Point staging at Postgres for both data and search.

**Exit.** Staging chat with citations, journal create, voice-note indexing, coach search, webhook idempotency, and user erasure all work against Postgres. A **restore drill** has been rehearsed: restore to a point in time and read the data back. Azure SQL and Azure AI Search for staging are unused for a week.

## Phase 5 — Objects and the chat queue

1. Create the R2 bucket, EU jurisdiction, public access off.
2. Copy staging blobs, preserving the path strings already stored in the database. If paths must change, rewrite the database rows in the same job.
3. Switch staging `IObjectStorage` to R2. Upload a coach file, download it, delete a user, confirm the prefix is gone.
4. Switch staging `IChatPersistenceQueue` to Postgres. Confirm a chat turn still persists when the request ends before the write completes.
5. Verify the erasure orchestrator covers R2 objects and pgvector rows as well as database rows.
6. Copy production blobs as a verified dry run. Production traffic still reads Azure Blob until Phase 7.

**Exit.** Staging calls no Azure Storage. An object has been read back from R2 after a restore test.

## Phase 6 — Models (optional, not a cost saving)

Skip this phase unless the goal is to close the Azure account or consolidate vendors. Azure OpenAI GlobalStandard has no idle cost, so there is nothing to save, and prompt behaviour is a product risk.

1. Create an OpenAI project with a budget alert. Confirm model identifiers exist for the equivalents of `gpt-5-mini` and `text-embedding-3-small`.
2. Confirm the transcription upload limit (25 MB at the time of writing) against the voice-note limits in `Chat.cs` and `VoiceNotes.cs`, and that the same audio formats are accepted.
3. Obtain zero-retention and no-training terms in writing before any production prompt is sent.
4. On staging, switch chat, embeddings, and transcription. Keep 1536 dimensions.
5. Exercise a voice note, a journal summary, a coach chat with citations, and a tool-calling agent turn. Confirm new query embeddings still hit the Phase 4 index.

**Exit.** Staging generative calls succeed with no Azure OpenAI connection string, and token spend appears on the OpenAI project.

## Phase 7 — Compute cutover

Staging first. Production is a DNS change after staging has been the only staging host for several days.

1. **Compliance gate.** Signed data processing agreements with each new sub-processor, an updated sub-processor list and privacy notice, a DPIA review, and a recorded decision on where backups live. The data includes journals, mood, coping strategies, and coach conversations, so this is a gate, not paperwork to follow.
2. Deploy Redis, API, and frontend as described in [deployment](04-deployment.md), with one instance and a maximum of one unless the SignalR backplane work is done. Build the web image with `DEPLOY_ENV` for the target environment; a staging web image cannot be promoted, because `NEXT_PUBLIC_*` values are baked at build time.
3. Audit configuration by connection name before the first boot: `ConnectionStrings__projectbraindb`, `ConnectionStrings__azurecache`, `ConnectionStrings__blobs`, the search and model client settings, and every `Auth0__*`, `Stripe__*`, `Mailgun__*`, `Firebase__*`, `LaunchDarkly__*`, `GoogleMaps__*`, and `OTEL_*` value Aspire used to inject.
4. Run migrations as a release command against the direct connection string, asserting on `deploy-env` so test users are never seeded into production.
5. Attach staging domains. Update Auth0 callbacks, logout URLs, audience, and the Auth0 webhook URL, plus the Stripe webhook URL. Confirm WebSocket upgrades work on the API domain for the coach-messages hub.
6. Exercise: sign-in, onboarding, chat with a citation, file upload, journal, voice note, coach geo search, a Stripe test webhook, an Auth0 deletion webhook, and full account erasure across rows, vectors, and objects.
7. For production: lower DNS TTL the day before, take a final database backup, run the data copy again or replicate the delta, deploy, shift DNS, then watch the first real chat.
8. Leave Azure deployed and idle for one release cycle.

**Exit.** Production hostnames resolve to the new host. A fresh deploy does not call `azd`. A restart is a normal process start.

Rollback is DNS back to Azure plus the pre-cutover backup. Be explicit in the cutover note: once users have written to the new database, a DNS rollback loses those writes unless they are replayed. Prefer a forward fix.

## Phase 8 — Decommission Azure

After the rollback window:

1. Delete Container Apps and the environment, the database, AI Search, the storage account, Managed Redis, App Configuration, and the Azure OpenAI account if Phase 6 ran.
2. Remove `azure.yaml`, the Azure deploy workflow, and any remaining scale workflows.
3. Remove `Aspire.Hosting.Azure.*` from the AppHost. Keep Aspire for local containers, and switch the local database container to Postgres with pgvector.
4. Revisit health checks now that the database does not pause: add a database check to readiness and shorten the 90 second warmup budget.
5. Delete the Azure federated credential from the GitHub organisation.

**Exit.** The invoice no longer contains those resources, and `dotnet run --project ProjectBrain.AppHost` still starts the stack locally.

## Phase 9 — Later work

| Follow-up | When |
| --- | --- |
| SignalR backplane and multi-instance | Availability during deploys matters more than simplicity, or one instance cannot carry the load |
| Move off Auth0 | Auth0 exceeds compute and database combined, and a user-visible re-login is acceptable |
| Dedicated vector service or a reranker | Measured recall or latency on pgvector with HNSW and fusion is not good enough |
| Second region | Chat latency is dominated by geography rather than the model |
| Re-price the host | Fly's October 2026 increase, or Hetzner's availability, changes the comparison |
| Re-enable sleeping | Never for an API serving real users. A separate preview app may stay off; that is a different app, not `minReplicas=0` on staging. |

## Risk register

| Risk | Mitigation |
| --- | --- |
| Migration is undertaken for a cost saving that does not exist | Phase 1 and Phase 2. Measure the fixed-in-place bill before committing. |
| Converting off the Azure SQL free offer is irreversible | Confirm the target tier's size and DTU limits against real load in Phase 1 before converting. |
| Case-sensitive Postgres breaks user lookup | Phase 3 decides the strategy; Phase 4 tests it before data moves. |
| Postgres baseline misses a default, collation, or cascade | Row-count diff plus migration verification in Phase 4, since CI does not exercise migrations today. |
| Identity sequences collide after the copy | Set each sequence to `max(id) + 1` in the copy job. |
| Retrieval quality drops | Same model and dimensions; compare citation ids on fixed questions before switching reads; add hybrid search if the semantic path is currently working. |
| Semantic ranker parity is assumed rather than measured | Check for the fallback warning in Phase 0. |
| Erasure misses R2 or pgvector | Extend the orchestrator in Phases 3 to 5 and run it in staging before production. |
| Coach messages stop arriving after scale-out | Maximum one instance until the Redis backplane is added. |
| Jobs run twice | Verify TickerQ locking before more than one instance. |
| Web image promoted between environments | One image per environment; `NEXT_PUBLIC_*` is build-time. |
| Aspire-injected configuration missing on the new host | Connection-name audit in Phase 7 step 3. |
| Single instance means downtime on deploy | Stated availability target in Phase 2; two instances if unacceptable. |
| Self-managed Postgres loses data | Continuous archiving to object storage plus a rehearsed restore, or use managed Postgres. |
| Health endpoint reports healthy without a database | Deliberate today. Revisit in Phase 8. |
| New sub-processors handle health-adjacent data without agreements | Phase 7 compliance gate. |
| Model provider retains prompts | Keep Azure OpenAI, or get zero-retention terms in writing in Phase 6. |
| Cutover writes split across two databases | Short write freeze or read-only maintenance for the final copy. Do not dual-write. |

## Suggested order of pull requests

This document set is the only change in the current branch.

1. Phase 1 infrastructure: warm replicas, maximum replica count, database tier change, delete the Speech resource, retire the sleep workflows.
2. Search query model, `IObjectStorage`, API Dockerfile. No behaviour change.
3. Case-sensitivity strategy plus tests.
4. Npgsql baseline, Postgres and pgvector test containers, migration verification.
5. pgvector search implementation, staging only.
6. R2 and the Postgres chat queue, staging only.
7. Host configuration and the new deploy workflow, staging hostnames.
8. Optional model provider switch, staging only.
9. Production cutover checklist and Azure deletion, after the soak.
