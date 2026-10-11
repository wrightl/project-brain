# Move the database to Neon, keep Container Apps

This replaces the [v0 plan](../v0/README.md). Instead of leaving Azure, keep the API and frontend on Azure Container Apps with one replica each running all the time, and move the database from Azure SQL serverless to Neon Postgres.

Azure SQL is both the largest line on the bill and the cause of the slow first request: when it resumes it can take up to 90 seconds (`DatabaseStartupHostedService`, SQL error 40613). Neon resumes in well under a second, so it can be left to scale to zero.

The app has no real users yet. There is no data to migrate and no downtime to avoid: each environment gets a fresh Postgres schema seeded by `ProjectBrain.MigrationService`, and the old Azure resources are deleted once the new setup works.

## Scope

| Concern | Today | After |
| --- | --- | --- |
| Database (deployed) | Azure SQL serverless, free offer with `BillOverUsage` | Neon Postgres, `aws-eu-west-2` (London), scale to zero after 5 minutes |
| Database (local) | SQL Server container from the AppHost | Postgres container from the AppHost |
| EF Core provider | `Aspire.Microsoft.EntityFrameworkCore.SqlServer` | `Aspire.Npgsql.EntityFrameworkCore.PostgreSQL` |
| Cache | Azure Managed Redis (deployed), Redis container (local) | In-process `AddDistributedMemoryCache()` everywhere |
| Database health check | `DatabaseMigrationsHealthCheck` on `/health/db` | Removed |
| API and frontend replicas | `minReplicas` 0 on staging (sleep/wake workflows), max set by Azure's default | `minReplicas = 1`, `maxReplicas = 1` |
| Azure SQL and Redis resources | Provisioned by `azd` | Deleted |

These stay as they are: Container Apps, Azure AI Search, Azure OpenAI, Blob Storage and Queues, App Configuration, Auth0, and the rest of the SaaS integrations. Moving them is a separate decision.

## Decisions

| Decision | Choice | Why |
| --- | --- | --- |
| Neon layout | One Neon project in `aws-eu-west-2` with two branches, `production` and `staging`, each with a `projectbrain` database | Container Apps run in UK South (London). Neon no longer allows new projects in its Azure regions. Branches share one project and one bill. |
| Postgres version | 17 on Neon and locally | Local and deployed should match. |
| Scale to zero | On for both branches, 5 minutes | The 90-second wake-up came from Azure SQL. A Neon wake adds a few hundred milliseconds to the first query after it has been idle. |
| Connection strings | API uses Neon's **pooled** string. The migrations job uses the **direct** string. | Neon recommends a direct connection for schema changes. |
| Npgsql connection settings | Add `Connection Idle Lifetime=60` to both strings | Npgsql keeps idle connections for 300 seconds by default, the same as Neon's suspend timeout. Pruning at 60 seconds means the pool never hands out a connection Neon has already closed. Aspire's EF retry policy covers anything else. |
| Existing SQL Server migrations | Delete them and generate one Postgres `InitialCreate` | There is no data to keep, and SQL Server migrations can't run on Postgres. Git keeps the history. |
| Data migration | None. Each environment is created empty and seeded. | No real users. |
| Replica count | Exactly 1 for both apps | The in-memory cache and SignalR (`/hubs/coach-messages`) both assume a single API instance. |

## Work, as pull requests

### PR 0 — Upgrade Aspire

Do this first, on its own, so the later PRs pick up the current Aspire integrations and any breaking change in Aspire shows up separately from the database work.

At the time of writing, the latest stable Aspire on NuGet is **13.6.1**. Most of the solution is on 13.4.6. Check NuGet again when you start, and use whatever is latest then.

| Project | Package | From | To |
| --- | --- | --- | --- |
| `ProjectBrain.AppHost` | `Aspire.AppHost.Sdk` (the `<Sdk>` element) | 13.4.6 | 13.6.1 |
| `ProjectBrain.AppHost` | `Aspire.Hosting.AppHost` and every `Aspire.Hosting.Azure.*` package (`AppConfiguration`, `AppContainers`, `CognitiveServices`, `Redis`, `Search`, `Sql`, `Storage`), `Aspire.Hosting.DevTunnels` | 13.4.6 | 13.6.1 |
| `ProjectBrain.AppHost` | `Aspire.Hosting.NodeJs` | **9.5.2** | Replace with `Aspire.Hosting.JavaScript` 13.6.1 (see below) |
| `ProjectBrain.Api` | `Aspire.Azure.AI.OpenAI` | 13.4.6-preview.1.26319.6 | 13.6.1-preview.1.26506.6 (this package only ships as a preview) |
| `ProjectBrain.Api` | `Aspire.Azure.Search.Documents`, `Aspire.Azure.Storage.Blobs`, `Aspire.Azure.Storage.Queues`, `Aspire.Microsoft.Extensions.Configuration.AzureAppConfiguration`, `Aspire.StackExchange.Redis.DistributedCaching` | 13.4.6 | 13.6.1 |
| `ProjectBrain.Database`, `ProjectBrain.MigrationService` | `Aspire.Microsoft.EntityFrameworkCore.SqlServer` | 13.4.6 | 13.6.1 |
| `ProjectBrain.ServiceDefaults` | `Microsoft.Extensions.ServiceDiscovery`, `Microsoft.Extensions.Http.Resilience` | 10.7.0 | 10.10.0 |
| `ProjectBrain.ServiceDefaults` | `OpenTelemetry.*` | 1.15–1.16 | Latest stable (the `EntityFrameworkCore` instrumentation stays on its beta line) |

PR 1 removes the SQL Server and Redis packages and PR 6 removes `Aspire.Hosting.Azure.Sql`. Upgrade them here anyway: it keeps every Aspire package on one version, so the restore doesn't fail on mixed versions.

**Node.js hosting.** `Aspire.Hosting.NodeJs` stopped at 9.5.2. It was replaced by `Aspire.Hosting.JavaScript` in Aspire 13, so the AppHost is mixing a 9.x package with 13.x.

- In the non-publish branch of `AppHost.cs`, replace `builder.AddNpmApp(frontendName, "../projectbrain.frontend", "dev")` with the `Aspire.Hosting.JavaScript` equivalent (`AddJavaScriptApp`, running the `dev` script).
- Keep `.WithReference(apiService)`, `.WaitFor(apiService)` and `.WithExternalHttpEndpoints()`.
- Check whether the new API installs npm packages on its own. If it does and you don't want that, turn it off.
- The publish branch uses `AddDockerfile` and doesn't change.

**How**

- Use `aspire update` from the Aspire CLI if it is installed. It updates the SDK and the hosting and client packages together. Otherwise use `dotnet outdated` or edit the `.csproj` files by hand.
- Optional: put the version in one `<AspireVersion>` property in a new `Directory.Build.props`, so later upgrades are a single change.
- Read the Aspire 13.5 and 13.6 release notes for breaking changes. Pay particular attention to the experimental APIs the AppHost uses: `ASPIREPROBES001` (`WithHttpProbe`) and `ASPIREACADOMAINS001` (`ConfigureCustomDomain`). Experimental APIs can change between minor versions.

**Done when**

- `dotnet build` and every test project pass.
- Locally, `dotnet run --project ProjectBrain.AppHost` starts everything, including the Next.js dev server, and the dashboard shows all resources healthy.
- A staging deploy through `azd` still works. Compare the generated infrastructure (`azd infra gen`, or the manifest) before and after to catch unexpected changes to the Container Apps, probes or custom domains.

### PR 1 — Postgres provider and schema

**Packages**

- `ProjectBrain.Database` and `ProjectBrain.MigrationService`: replace `Aspire.Microsoft.EntityFrameworkCore.SqlServer` with `Aspire.Npgsql.EntityFrameworkCore.PostgreSQL` (the same version PR 0 moved the other Aspire packages to).
- `ProjectBrain.Database`: remove `Microsoft.Data.SqlClient.Extensions.Azure`.

**Registration**

- [ProjectBrain.Database/ProgramExtensions.cs](../../../ProjectBrain.Database/ProgramExtensions.cs): `AddSqlServerDbContext` becomes `AddNpgsqlDbContext<AppDbContext>("projectbraindb", settings => settings.DisableHealthChecks = true)`.
- [ProjectBrain.MigrationService/Program.cs](../../../ProjectBrain.MigrationService/Program.cs): same swap.
- Add an `IDesignTimeDbContextFactory<AppDbContext>` in `ProjectBrain.Database` that calls `UseNpgsql` with a placeholder local connection string. Then `dotnet ef migrations add` works without the Aspire host running.

**Model fixes.** The model has SQL Server types in it:

- Thirteen `[Column(TypeName = "nvarchar(...)")]` attributes in `ProjectBrain.Database/models/` (`JournalEntry`, `JournalEntrySystemTag`, `OnboardingData`, `QuizQuestion`, `QuizResponse`, `SystemTagFieldDefinition`, `AgentWorkflow` ×2, `AgentAction` ×2, `CommunityPost`, `Goal`, `CoachProfile`):
  - `nvarchar(max)` becomes `[Column(TypeName = "text")]`, or drop the attribute (a `string` maps to `text` by default).
  - `nvarchar(n)` becomes `[MaxLength(n)]`.
- `date` and `decimal(10,2)` are valid in Postgres and stay.

**Raw SQL**

- [UserActivityService.cs:158](../../../ProjectBrain.Domain/services/UserActivityService.cs#L158): replace `ExecuteSqlRawAsync("UPDATE Users ...")` with `_context.Users.Where(u => u.Id == userId).ExecuteUpdateAsync(s => s.SetProperty(u => u.LastActivityAt, timestamp))`. As written, the statement would fail on Postgres (the unquoted `Users` is folded to lowercase `users`), and the `catch` would hide the failure.
- [CriticalDatabaseSeeder.cs:64](../../../ProjectBrain.MigrationService/Seeding/CriticalDatabaseSeeder.cs#L64): remove the `SET IDENTITY_INSERT` branch. The `AddRangeAsync` branch already inserts explicit IDs. Then reset the sequence with `SELECT setval(pg_get_serial_sequence('"SubscriptionTiers"', 'Id'), (SELECT MAX("Id") FROM "SubscriptionTiers"))`, otherwise the next insert collides with ID 1. A simpler option is `HasData` for the tiers in `OnModelCreating`.

**Case sensitivity.** SQL Server's default collation ignores case. Postgres doesn't.

- `EF.Functions.Like` in [UserFactRepository.cs:58](../../../ProjectBrain.Domain/Repositories/UserFactRepository.cs#L58) and [UserEpisodeRepository.cs:49](../../../ProjectBrain.Domain/Repositories/UserEpisodeRepository.cs#L49) becomes `EF.Functions.ILike`.
- `u.Email == email` in [UserRepository.cs:18](../../../ProjectBrain.Domain/Repositories/UserRepository.cs#L18) and line 51: normalise emails to lower case when they are written and when they are looked up, or make the column `citext`. Auth0 usually sends lowercase emails, but don't rely on it.

**DateTime.** Npgsql maps `DateTime` to `timestamp with time zone` and throws if you write a `DateTime` whose `Kind` isn't `Utc`. Review the 18 uses of `DateTime.Now`, `DateTime.Today` and `DateTimeKind.Local` or `Unspecified`, and the 6 `new DateTime(...)` / `DateTime.Parse` calls, in `ProjectBrain.Api`, `ProjectBrain.Domain`, `ProjectBrain.Database` and `ProjectBrain.MigrationService`. Any that reach the database must be UTC. Don't turn on `Npgsql.EnableLegacyTimestampBehavior`.

**Migrations**

- Delete `ProjectBrain.Database/Migrations/`. There are 45 migrations plus the snapshot, all SQL Server.
- Generate the Postgres baseline: `dotnet ef migrations add InitialCreate --project ProjectBrain.Database`.
- Check the generated SQL for cascade paths. The comment on `Connection` in `AppDbContext` about avoiding cascade cycles is a SQL Server limit that Postgres doesn't have. Leave the delete behaviour as it is for now, so behaviour stays the same.

### PR 2 — Local development on Postgres, no Redis

[ProjectBrain.AppHost/AppHost.cs](../../../ProjectBrain.AppHost/AppHost.cs), in the non-publish branch:

```csharp
var postgres = builder.AddPostgres("postgres", password: dbPassword, port: 49976)
    .WithImageTag("17")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithDataVolume();

var db = postgres.AddDatabase("projectbraindb", databaseName: "projectbrain");
```

- Add `Aspire.Hosting.PostgreSQL` to the AppHost. Remove `Aspire.Hosting.SqlServer` if it is only used here.
- Rename the `projectbrain-password` parameter to something neutral, such as `db-password`, and update user secrets and `readme.md`.
- Keep the resource name `projectbraindb`, so the API and migrations still read `ConnectionStrings__projectbraindb`.
- Remove the `cache` resource (`AddAzureManagedRedis(...).RunAsContainer()`), `.WithReference(cache)` on the API, `.WaitFor(cache)` on both frontend definitions, the `Azure.Provisioning.RedisEnterprise` using, and the `Aspire.Hosting.Azure.Redis` package.
- Developers delete the old SQL Server volume and container once. Nothing in it needs keeping.
- Optional: `.WithPgAdmin()` or `.WithPgWeb()` for a local database browser.

### PR 3 — In-memory cache

- [ProjectBrain.Api/Program.cs:33-35](../../../ProjectBrain.Api/Program.cs#L33-L35): replace `builder.AddRedisDistributedCache("azurecache")` with `builder.Services.AddDistributedMemoryCache()`.
- Remove `Aspire.StackExchange.Redis.DistributedCaching` from `ProjectBrain.Api.csproj`. This also removes the Redis health check that Aspire added to `/health`.
- Rename `RedisCacheService` to `DistributedCacheService` in `ProjectBrain.Domain/Caching/`. It only depends on `IDistributedCache`. Fix the "Redis" wording in `UserActivityService` and `UserActivitySyncService` comments and log messages.
- `ProjectBrain.Api.IntegrationTests/CustomWebApplicationFactory.cs`: remove the `ConnectionStrings:azurecache` and `Aspire:StackExchange:Redis:DisableHealthChecks` settings.

All callers already use `IDistributedCache`, so no other code changes. What changes in behaviour:

- **The cache is empty after every restart or deploy.** `UserActivitySyncService` already rebuilds the activity entries from the database.
- **Webhook idempotency keys are lost on restart** (`DistributedWebhookIdempotencyService`). A Stripe or Auth0 webhook that is retried across a restart could be processed twice. That's acceptable before launch. Revisit before going live, either by making the webhook handlers idempotent on database state or by storing the keys in Postgres.

### PR 4 — Remove the database health check

The readiness probe (`/health`) already skips database checks, and the Aspire EF health check is already disabled. So the probes are not what keeps a database awake. The database-only check and its endpoint are what's left to remove:

- `ProjectBrain.Api/Program.cs:78-79`: remove the `DatabaseMigrationsHealthCheck` registration.
- Delete `ProjectBrain.Database/DatabaseMigrationsHealthCheck.cs` and `ProjectBrain.Database.Tests/DatabaseMigrationsHealthCheckTests.cs`.
- [ProjectBrain.ServiceDefaults/Extensions.cs](../../../ProjectBrain.ServiceDefaults/Extensions.cs):
  - Remove the `/health/db` mapping and the `DbHealthCheckTag` / `DatabaseHealthEndpointPath` constants.
  - Map `/health` with no predicate, and update the comment there that mentions Azure SQL.
- Update `ProjectBrain.Api.Tests/HealthEndpointMappingTests.cs` and the `/health/db` case in `DatabaseStartupGateMiddlewareTests.cs`.
- `DatabaseStartupHostedService`:
  - Cut `MaxWarmupDuration` from 90 seconds to 20 seconds, and drop the comment about the Azure SQL 40613 error.
  - Keep the pending-migrations log warning, since it is the only remaining signal that migrations didn't run.
  - Remove `MarkMigrationsApplied` and `IsMigrationsApplied` from `IDatabaseStartupState` if nothing else reads them after the health check is gone.
- Keep `DatabaseStartupGateMiddleware`. It returns 503 only for the few seconds before the first database connection after a deploy.

Nothing else in the API queries the database on a timer, so Neon can suspend:

- `UserActivitySyncService` checks every 30 seconds but only queries when a request has triggered it.
- `DeviceTokenCleanupBackgroundService` runs every few hours.
- The only TickerQ schedule is `UserContext_MemoryDecay` at 03:00.

### PR 5 — Tests

- `ProjectBrain.Database.IntegrationTests`:
  - Replace `Testcontainers.MsSql` with `Testcontainers.PostgreSql`, using the `postgres:17` image.
  - Replace `Microsoft.EntityFrameworkCore.SqlServer` with `Npgsql.EntityFrameworkCore.PostgreSQL`.
  - `UseSqlServer` becomes `UseNpgsql` in `DatabaseIntegrationTests.cs` (lines 38 and 298).
- Add integration tests for the behaviour that changed:
  - `UserActivityService` updating `LastActivityAt`.
  - Case-insensitive memory search.
  - Seeding subscription tiers twice (sequence reset, then a new insert).
  - Email lookup with mixed case.
- `CustomWebApplicationFactory`: replace the `Aspire:Microsoft:EntityFrameworkCore:SqlServer:DisableHealthChecks` setting with the Npgsql equivalent (`Aspire:Npgsql:EntityFrameworkCore:PostgreSQL:DisableHealthChecks`), or drop it, since `DisableHealthChecks` is set in code.
- `.github/workflows/test.yml` needs no change: Testcontainers starts its own container on the GitHub runner.

### PR 6 — Deployment

**AppHost, publish branch**

- Remove `AddAzureSqlServer`, `azureDb`, the `Azure.Provisioning.Sql` using, the `Aspire.Hosting.Azure.Sql` package and the `sqlPassword` parameter.
- Add two secret connection strings and give the migrations job the direct one under the name it already reads:

  ```csharp
  var db = builder.AddConnectionString("projectbraindb");              // Neon pooled
  var dbDirect = builder.AddParameter("projectbraindb-direct", secret: true); // Neon direct

  var migrations = builder.AddProject<Projects.ProjectBrain_MigrationService>("migrations")
      .WithEnvironment("ConnectionStrings__projectbraindb", dbDirect)
      .WithMigrationSeedingEnvironment(/* unchanged */)
      .PublishAsAzureContainerAppJob();

  apiService.WithReference(db).WaitForCompletion(migrations) /* OTEL settings unchanged */;
  ```

- Set both apps to exactly one replica in `PublishAsAzureContainerApp`: `app.Template.Scale.MinReplicas = 1; app.Template.Scale.MaxReplicas = 1;`. Remove the `minReplicas` parameter. Container Apps would otherwise scale out under load, and the in-memory cache and SignalR would break.
- In `ContainerAppResourceDefaults`, add a `SetSingleReplica(ContainerApp app)` helper next to `ApplyQuarterCoreHalfGi`, so both apps share it.

**Workflows and scripts**

- Delete `sleep-staging.yml`, `wake-staging.yml` and `scale-staging-container-apps.yml`.
- Delete `.github/scripts/compute-min-replicas.sh` and `set-min-replicas.sh`.
- Remove the `scale` job from `deploy-staging.yml`.
- `azure-deploy.yml`:
  - Remove `AZURE_MIN_REPLICAS` and `AZURE_PROJECTBRAIN_PASSWORD`.
  - Pass the two Neon connection strings as secrets, the same way `AZURE_PROJECTBRAIN_PASSWORD` is passed today. Confirm the exact variable names from the parameter names in the generated `azd` infrastructure.
- Add `NEON_DATABASE_URL_POOLED` and `NEON_DATABASE_URL_DIRECT` to the `staging` and `production` GitHub environments. Remove the `MIN_REPLICAS` variable and the `SQL_PASSWORD` secret.

**Delete the old resources by hand.** `azd provision` only adds and updates resources. It won't delete Azure SQL or Managed Redis when they disappear from the AppHost. After each environment is verified on Neon:

```bash
az sql server delete --name <server> --resource-group rg-projectbrain-<env>
az redisenterprise delete --cluster-name <cluster> --resource-group rg-projectbrain-<env>
```

Look up the names with `az resource list -g rg-projectbrain-<env> -o table`.

### PR 7 — Docs

- `readme.md`: local setup (Postgres instead of SQL Server, no Redis), the `dotnet ef` command, and the parameter rename.
- `CLAUDE.md`: database and architecture sections.
- Mark the [v0 plan](../v0/README.md) as superseded by this one.

## Rollout

Not production-critical, so in this order:

1. **Neon.**
   - Create the project in `aws-eu-west-2` with the `production` and `staging` branches, a `projectbrain` database on each, and scale to zero at 5 minutes.
   - Copy the pooled and direct connection strings, adding `Connection Idle Lifetime=60` to each, into the GitHub environments.
2. **Merge PR 0** and deploy it to staging, to confirm the upgrade on its own.
3. **Merge PRs 1–5.** CI goes green. Local `dotnet run --project ProjectBrain.AppHost` starts Postgres, runs migrations and seeding, and the app works end to end.
4. **Merge PR 6 and deploy staging.**
   - The migrations job creates the schema on the Neon `staging` branch and seeds it.
   - Check the list below.
   - Delete the staging Azure SQL server and Redis cluster.
5. **Deploy production** the same way, then delete the production Azure SQL server and Redis cluster.
6. **Merge PR 7.**

## Checks per environment

- [ ] The migrations job exits successfully and the API log shows `Database warmup completed` on the first attempt.
- [ ] Sign in through the web app and the mobile app, complete onboarding, and see the profile.
- [ ] Chat with a citation from AI Search. The conversation is saved and the title is summarised by TickerQ.
- [ ] Create a journal entry and a voice note. Set a goal. Search memory with mixed-case text.
- [ ] Run a coach geo search. Exchange coach messages over SignalR.
- [ ] Stripe test webhook, sent twice: processed once.
- [ ] Delete an account through the Auth0 webhook: rows, search documents and blobs are gone.
- [ ] Leave the environment idle for 10 minutes. The Neon console shows the compute **Suspended**, and the next request succeeds with no 5xx and only a small delay.
- [ ] Restart the API revision. `/alive` and `/health` are green within seconds, with no 90-second retry loop.
- [ ] After a week, check Cost Management: no SQL or Redis lines. The Container Apps cost per environment should be about $15–25 a month.

## Risks and notes

| Risk | Mitigation |
| --- | --- |
| A Postgres behaviour difference slips through (case sensitivity, UTC `DateTime`, ordering of ties, string length) | The integration tests in PR 5, and the manual checks above. No user data is at risk. |
| Neon doesn't suspend because something holds connections open | The idle-lifetime setting, plus the suspend check above. If it still never suspends, look in the Neon console for active connections. |
| The first request after Neon has been idle sees a closed connection | Aspire's Npgsql retry (on by default) plus the 60-second idle lifetime. |
| In-memory cache and SignalR stop working if a second replica appears | `maxReplicas = 1`. Scaling past one replica needs Redis (or a Postgres-backed cache) and a SignalR backplane. |
| TickerQ jobs are lost on restart | Already true today: TickerQ has no operational store. Unchanged here. Fix it with `TickerQ.EntityFrameworkCore` on Postgres in a follow-up, since every deploy drops queued journal and voice-note jobs. |
| Log Analytics cost goes up because replicas never stop | Lower the Container Apps environment's log level or turn its logging off. Grafana Cloud already receives OpenTelemetry. |

## Expected cost

| Item | Monthly, per environment |
| --- | --- |
| Container Apps, API + frontend, 0.25 vCPU / 0.5 GiB each, one replica | About $10 if idle, about $34 if busy the whole time. Likely about $15–25. The free allowance applies once per subscription. |
| Neon (existing paid plan, $0.106 per compute-hour, $0.35 per GB-month of storage) | A few dollars with scale to zero. About $19 at most for compute that runs all the time at 0.25 CU. |
| Azure SQL, Managed Redis | $0 (deleted) |

These are list prices from public sources, not quotes. Check Azure Cost Management and Neon's billing page after the first full month.
