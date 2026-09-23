# Deployment

Production deploy today is `azd provision` and `azd deploy` from `.github/workflows/azure-deploy.yml`, with federated Azure credentials, then `.github/scripts/run-migrations.sh`. Staging sleep and wake are separate workflows that set Container Apps replica counts. That mechanism is specific to Azure Container Apps and is the reason staging is cold.

The replacement is a normal container deploy: GitHub Actions builds images, publishes them, and the host rolls the process forward. Migrations run as a one-shot command before the new API revision takes traffic.

## Recommended: Fly.io + GitHub Actions

Three apps in one Fly organisation, EU region (`lhr` or `ams`, same region as Neon):

| App | Image | Machines | Network |
| --- | --- | --- | --- |
| `projectbrain-api` | New `ProjectBrain.Api/Dockerfile` | shared CPU, 1 GB RAM, `min_machines_running = 1` | Public 443, private to Redis and (via TLS) Neon |
| `projectbrain-web` | Existing `projectbrain.frontend/Dockerfile` | shared CPU, 512 MB–1 GB, `min_machines_running = 1` | Public 443 |
| `projectbrain-redis` | Official Redis image, append-only off if the cache may be empty after restart | shared CPU, 256 MB, `min_machines_running = 1` | Fly private network only |

`fly.toml` for the API sets:

- `auto_stop_machines = "off"` and `auto_start_machines = false` so the process is not scaled to zero.
- HTTP health check on `/health`, grace period long enough for a warm .NET start (the database is already up, so this is seconds, not the current 90 second SQL resume).
- Concurrency limits left at Fly defaults until real traffic says otherwise.

Secrets (`fly secrets set`) replace Azure App Configuration and the long `AZURE_*` list in the deploy workflow: Auth0, Stripe, Mailgun, Firebase, LaunchDarkly, database URL, R2 keys, OpenAI key, Grafana OTLP headers. GitHub environments `staging` and `production` hold the values the workflow passes through. Nothing in the workflow logs secret values.

### Pipeline

Replace `azure-deploy.yml` with a workflow that:

1. Runs the existing test workflow (already a prerequisite of production deploy).
2. Builds the API image and the frontend image.
3. Pushes them to Fly’s registry (`flyctl deploy --local-only` from a built image, or `flyctl deploy` with a remote builder).
4. Runs migrations as a release command: `flyctl machine run` or a Fly release command that executes `dotnet ef database update` (or the existing `ProjectBrain.MigrationService`) against the **direct** Neon connection string, then exits.
5. Deploys the API only after that command succeeds.
6. Deploys the frontend with `API_SERVER_URL` and Auth0 audience set to the API’s public URL.

Staging and production are two Fly apps (or two Fly organisations), not a replica-count toggle. Delete `sleep-staging.yml`, `wake-staging.yml`, and `scale-staging-container-apps.yml` when staging is no longer on Container Apps. Sleeping the new staging app would recreate the incident this plan exists to end.

### Domains and callbacks

Keep the current custom domains (`CUSTOMDOMAIN_API`, `CUSTOMDOMAIN_APP`). Attach them with Fly certificates. Update, in the same change window:

- Auth0 application callbacks, logout URLs, and the API audience if the hostname changes.
- Auth0 webhook URL.
- Stripe webhook URL.
- Any Mailgun or Firebase authorised domains.

Lower DNS TTL the day before production cutover.

### Rollback

`flyctl releases rollback` returns the previous image. Schema rollback is not automatic: expand migrations so the previous image still runs against the new schema until the release is accepted. Azure stays deployed and idle for one release cycle so DNS can point back if the new host fails health checks.

## Alternative: Hetzner + Kamal

Use this when the decision log picks a single VPS over Fly.

| Item | Choice |
| --- | --- |
| Host | One small Hetzner Cloud instance in Germany or Finland (2 vCPU, 4 GB class). A second, smaller host later if API and web need to fail independently. |
| Proxy | Kamal’s built-in proxy for TLS and zero-downtime deploys. |
| Processes | API container, Next.js container, Redis container. Postgres on Neon, or Postgres in another container with a nightly off-site backup. |
| Registry | GitHub Container Registry. |
| Workflow | The same test job, then `kamal deploy` using a GitHub Actions secret for SSH. |
| Migrations | `kamal app exec` for the migration command before the new containers are promoted. Kamal’s hook `pre-deploy` is the place to put it. |
| Firewall | SSH from GitHub Actions IPs or via a Tailscale interface; 80/443 public; Postgres and Redis unpublished. |

Kamal is the deploy mechanism. Docker Compose on the server, edited by hand, is not.

## What happens to Aspire and `azd`

| Tool | After migration |
| --- | --- |
| `dotnet run --project ProjectBrain.AppHost` | Still the local developer entry point. |
| `azure.yaml`, `azd provision`, `azd deploy` | Removed once production DNS has stayed on the new host for the rollback window. |
| `Aspire.Hosting.Azure.*` package references | Removed from the AppHost when no resource is published to Azure. Local SQL, Redis, and storage use containers only. |
| Container App Job for migrations | Replaced by the release command above. `ProjectBrain.MigrationService` can remain the program that applies migrations and seeds Auth0 users. |
| Federated Azure login in GitHub Actions | Removed with the Azure workflows. |

## Environments

| | Local | Staging | Production |
| --- | --- | --- | --- |
| API | Aspire, Kestrel | Fly app or Kamal destination | Fly app or Kamal destination |
| Database | Container | Neon branch or database `staging` | Neon database `production`, suspend disabled |
| Objects | Azurite or MinIO | R2 bucket `staging` | R2 bucket `production` |
| Models | OpenAI API key with a low project limit, or a recorded fixture in tests | OpenAI project `staging` | OpenAI project `production` |
| Auth0 | Existing tenant, localhost callbacks | Existing tenant, staging callbacks | Existing tenant, production callbacks |
| Replicas | n/a | Always on, smallest size | Always on, smallest size |

## Health and warmup

`DatabaseStartupHostedService` exists to absorb SQL 40613. After cutover the database does not pause, so a failed connection is a real outage and should fail `/health` immediately. Shorten the warmup budget in a follow-up once staging has run a week without retry noise. Leave the retry in place during the Azure soak so the old host still works.
