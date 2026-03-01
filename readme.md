# ProjectBrain

Full-stack application built with .NET Aspire (orchestrator), ASP.NET Core Web API, and Next.js frontend. Uses Auth0 for authentication, SQL Server with Entity Framework Core, Redis cache, and Azure Container Apps for deployment.

## Table of contents

- [Prerequisites](#prerequisites)
- [Getting started](#getting-started)
- [Project structure](#project-structure)
- [Development](#development)
- [Testing](#testing)
- [Database and migrations](#database-and-migrations)
- [Deployment](#deployment)
- [Auth0 and configuration](#auth0-and-configuration)
- [Further reading](#further-reading)

---

## Prerequisites

- **.NET 10 SDK** (or version specified in `global.json` / workflows)
- **Node.js 22** and npm (for frontend)
- **Azure Developer CLI (`azd`)** – for deployment ([install](https://learn.microsoft.com/azure/developer/azure-developer-cli/install-azd))
- **Docker** – optional; used for frontend in published mode and for some integration tests

---

## Getting started

### Clone and restore

```bash
git clone <repo-url>
cd ProjectBrain
dotnet restore
```

### Run the full stack locally

From the solution root, run the App Host. It starts the API, database, cache, frontend (npm in dev), and other services:

```bash
dotnet run --project ProjectBrain.AppHost
```

Or with watch and HTTPS:

```bash
dotnet watch run --project ProjectBrain.AppHost/ProjectBrain.AppHost.csproj --launch-profile https
```

The Aspire dashboard and app/API URLs will be shown in the console.

### Run frontend only (against a running API)

If the API is already running (e.g. via App Host):

```bash
cd projectbrain.frontend
npm install
npm run dev
```

Use `.env.local` for local overrides (Auth0, API URL, etc.). See [Auth0 and configuration](#auth0-and-configuration).

### Run backend tests

```bash
dotnet test ProjectBrain.Api.Tests/ProjectBrain.Api.Tests.csproj
dotnet test ProjectBrain.Database.Tests/ProjectBrain.Database.Tests.csproj
```

### Run frontend tests

```bash
cd projectbrain.frontend
npm ci
npm run lint
npm test
```

---

## Project structure

| Path | Description |
|------|-------------|
| **ProjectBrain.AppHost** | .NET Aspire orchestrator: provisions and runs API, frontend, SQL, Redis, Azure AI Search, etc. |
| **ProjectBrain.Api** | ASP.NET Core minimal APIs, Auth0 JWT, OpenAPI/Scalar |
| **ProjectBrain.Domain** | Business logic, services, repositories, unit of work |
| **ProjectBrain.Database** | EF Core models, `AppDbContext`, migrations |
| **ProjectBrain.Shared.Dtos** | Shared DTOs between API and domain |
| **ProjectBrain.ServiceDefaults** | Shared Aspire config (resilience, telemetry) |
| **projectbrain.frontend** | Next.js 16, React 19, TypeScript, Tailwind, Auth0 |

Backend uses a **service/repository** pattern: APIs call domain services; services use repositories and unit of work. Do not use `DbContext` directly in API code—use services instead. See [ARCHITECTURE.md](ARCHITECTURE.md).

Frontend uses **Next.js API routes** as proxies to the backend; client components call these API routes rather than the backend directly. See `.cursor/rules/api-routes.mdc`.

---

## Development

### Backend

- **Build:** `dotnet build`
- **API endpoints:** `ProjectBrain.Api/apis/*.cs` (minimal APIs with endpoint mapping).
- **New features:** Add/use services and repositories in Domain/Database; expose via minimal APIs and DTOs. Register new services in `ProjectBrain.Domain/ProgramExtensions.cs`.
- **Theme:** UI uses `data-theme` on `<html>`; use theme-aware classes (e.g. `bg-white`, `text-gray-900`). Do not rely on Tailwind `dark:*` for theme. See `.cursor/rules/theme.mdc` and `projectbrain.frontend/src/styles/themes/`.

### Frontend

- **Dev server:** `cd projectbrain.frontend && npm run dev`
- **Build:** `npm run build`
- **Lint:** `npm run lint`
- **Structure:** `src/app/` (App Router), `src/_components/`, `src/_services/`, `src/_hooks/`, `src/api/` (API routes). Use API routes and services for data; use React Query in hooks where appropriate.

### Conventions

- **API:** DTOs for all request/response; no raw entities. Use `PagedRequest`/`PagedResponse<T>` for lists. Throw domain exceptions; global middleware returns ProblemDetails.
- **Database:** Use `AsNoTracking()` for read-only queries; paginate with `Skip`/`Take` in the database.
- **Auth:** Endpoints are authorized by default; use role-based policies (e.g. `AdminOnly`, `CoachOnly`) where needed. See [authorisation_guide.md](authorisation_guide.md) for Auth0 scopes and policies.

---

## Testing

| Suite | Command |
|-------|--------|
| All .NET tests | `dotnet test` |
| API tests | `dotnet test ProjectBrain.Api.Tests/ProjectBrain.Api.Tests.csproj` |
| Database/service tests | `dotnet test ProjectBrain.Database.Tests/ProjectBrain.Database.Tests.csproj` |
| Frontend | `cd projectbrain.frontend && npm test` |
| Frontend watch | `npm run test:watch` |
| Frontend coverage | `npm run test:coverage` |

CI (`.github/workflows/ci.yml`) runs on push/PR to `main`: .NET tests and frontend lint + tests. For integration tests and Testcontainers, see [TESTING.md](TESTING.md) and [INTEGRATION_TESTS.md](INTEGRATION_TESTS.md).

---

## Database and migrations

- **Provider:** SQL Server (EF Core). Models and context in `ProjectBrain.Database`.
- **Add or update migrations** (from solution root):

```bash
cd ProjectBrain.Api
dotnet ef migrations add <MigrationName> --project ../ProjectBrain.Database/ProjectBrain.Database.csproj
cd ..
```

Use a descriptive name instead of `InitialCreate` for schema changes. Apply migrations as part of your deployment or local startup (App Host / app configuration).

---

## Deployment

Deployment uses **Azure Developer CLI (`azd`)** and **Azure Container Apps**. The App Host defines the deployable stack (see `ProjectBrain.AppHost/AppHost.cs`).

### Local deploy with azd

1. Install [azd](https://learn.microsoft.com/azure/developer/azure-developer-cli/install-azd).
2. Log in: `azd auth login`.
3. Initialize environment (first time): `azd env new <environment-name>` and select subscription/location.
4. Set required parameters and secrets (see [DEPLOYMENT_GITHUB_ACTIONS.md](DEPLOYMENT_GITHUB_ACTIONS.md) for the full list).
5. Provision and deploy:

```bash
azd up
```

Or separately:

```bash
azd provision
azd deploy --all
```

To remove a deployment, delete the resource group (there is no dedicated “undeploy”):

```bash
az group delete --name <your-resource-group-name>
```

### Deploy with Aspire CLI (alternative)

You can also use the Aspire CLI:

```bash
aspire deploy
```

### GitHub Actions (CI/CD)

The repo uses **GitHub Actions** with **OIDC** (no stored Azure credentials). The `.azure/` folder is not committed; each workflow run configures the `azd` environment from GitHub **Environments**.

**Workflows:**

- **`ci.yml`** – On push/PR to `main`: .NET and frontend tests.
- **`deploy-staging.yml`** – On push to `main` or manual: provisions and deploys to the **staging** environment.
- **`deploy-production.yml`** – Manual only: deploys to **production** (use environment protection rules for approvals).

**Setup (summary):**

1. Create GitHub Environments: `staging`, `production`.
2. **Secrets** (per environment): `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, and all app secrets (Auth0, SQL, cache, Mailgun, LaunchDarkly, Firebase, etc.) as listed in [DEPLOYMENT_GITHUB_ACTIONS.md](DEPLOYMENT_GITHUB_ACTIONS.md).
3. **Variables** (per environment): `AZURE_ENV_NAME`, `AZURE_LOCATION`, `AZURE_SUBSCRIPTION_ID`, `DEPLOYMENT_ENVIRONMENT`, `MIN_REPLICAS`, and optional custom domain/certificate vars.

Full list of env vars and secrets, and how they map to `azd`, is in [DEPLOYMENT_GITHUB_ACTIONS.md](DEPLOYMENT_GITHUB_ACTIONS.md).

### Custom domains and certificates

For custom domains and TLS on Container Apps:

1. Deploy once without custom domain/certificate in App Host if needed.
2. Get verification ID and FQDN:

```bash
az containerapp list --query "[].{Name: name, VerificationId: properties.customDomainVerificationId, Fqdn: properties.configuration.ingress.fqdn}" -o table
```

3. Add DNS: TXT `asuid.<subdomain>` = verification ID; CNAME `<subdomain>` = FQDN.
4. Create certificate (CNAME validation, host = your domain):

```bash
az containerapp env certificate create \
  --name <container-env-name> \
  --resource-group <resource-group-name> \
  --validation-method CNAME \
  --hostname <host-domain>
```

5. Re-add custom domain and certificate names in App Host / config, then redeploy.

Detailed steps: [withaspire.dev – Custom domains with Aspire](https://www.withaspire.dev/custom-domains-with-aspire/).

### SQL Server access (new Azure deployments)

For new Azure deployments, you may need to grant yourself access to the SQL database. In Azure Portal: **SQL Server → Settings → Microsoft Entra ID → Set admin** to your identity.

### Infrastructure as code

Generate Bicep/infra from the App Host:

```bash
aspire infra
# or
azd infra gen
```

Regenerating overwrites infra files; use `--force` to overwrite without prompt. See [next-steps.md](next-steps.md).

---

## Auth0 and configuration

- **Backend:** JWT Bearer validation; Auth0 domain/audience and parameters come from configuration (e.g. App Host parameters, `appsettings`, or `azd` env).
- **Frontend:** `@auth0/nextjs-auth0`; middleware in `projectbrain.frontend/src/middleware.ts`. Use `.env.local` for local Auth0 (and API URL) values.
- **Roles:** The web app uses an Auth0 Post-Login action that adds roles to the token under a custom namespace (e.g. `https://projectbrain.app/roles`). Ensure the application has the correct metadata for that namespace.
- **API permissions:** Add permissions in Auth0 under **APIs → [Your API] → Permissions**.
- **New Auth0 tenant:** Create a Post-Login action that sets the roles namespace and adds roles to ID and access tokens (see [readme-old.md](readme-old.md) for a sample snippet).

---

## Further reading

| Document | Purpose |
|----------|--------|
| [ARCHITECTURE.md](ARCHITECTURE.md) | Backend/frontend architecture, patterns, adding entities and APIs |
| [CLAUDE.md](CLAUDE.md) | High-level project and command reference for AI/developers |
| [DEPLOYMENT_GITHUB_ACTIONS.md](DEPLOYMENT_GITHUB_ACTIONS.md) | Full GitHub Actions and `azd` env setup |
| [TESTING.md](TESTING.md) | Test layout and how to run tests |
| [INTEGRATION_TESTS.md](INTEGRATION_TESTS.md) | Integration and Testcontainers |
| [authorisation_guide.md](authorisation_guide.md) | Auth0 scopes, policies, and .NET/Next.js auth |
| [next-steps.md](next-steps.md) | After `azd init`, billing, troubleshooting |

---

**Quick reference – common commands**

```bash
# Full stack (from repo root)
dotnet run --project ProjectBrain.AppHost

# Frontend only
cd projectbrain.frontend && npm run dev

# All backend tests
dotnet test

# Frontend tests
cd projectbrain.frontend && npm test

# Deploy (after azd env is configured)
azd up
```
