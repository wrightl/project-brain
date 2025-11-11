# Copilot Instructions for ProjectBrain

## Overview

ProjectBrain is a full-stack .NET Aspire solution with a Next.js frontend and ASP.NET Core Web API backend. It uses Auth0 for authentication, Entity Framework Core for data access, and is designed for cloud-native deployment (Azure Container Apps).

## Architecture

-   **AppHost** (`ProjectBrain.AppHost/`): .NET Aspire orchestrator. Runs all services, manages environment-specific config, and launches the frontend in dev mode.
-   **API** (`ProjectBrain.Api/`): ASP.NET Core Web API. Uses minimal APIs, Auth0 JWT authentication, and OpenAPI docs. Endpoints are mapped via extension methods.
-   **Database** (`ProjectBrain.Database/`): Entity Framework Core data layer. SQL Server is primary; Cosmos DB support is present but commented out. Models: `User`, `Conversation`.
-   **Frontend** (`projectbrain.frontend/`): Next.js 15 app with TypeScript, TailwindCSS, and Auth0. Uses `@auth0/nextjs-auth0` for session management.
-   **ServiceDefaults**: Shared Aspire service config for observability and environment settings.

## Developer Workflows

-   **Run full stack (dev):**
    -   `dotnet run --project ProjectBrain.AppHost` (runs API, DB, and frontend together)
-   **Build all .NET projects:**
    -   `dotnet build`
-   **Run backend tests:**
    -   `dotnet test` (all)
    -   `dotnet test ProjectBrain.Api.Tests/ProjectBrain.Api.Tests.csproj` (API)
    -   `dotnet test ProjectBrain.Database.Tests/ProjectBrain.Database.Tests.csproj` (DB)
-   **Frontend dev:**
    -   `cd projectbrain.frontend && npm run dev`
    -   `npm run lint`, `npm test`, `npm run test:watch`, `npm run test:coverage`

## Key Patterns & Conventions

-   **Minimal APIs:** Endpoints are mapped in `ProjectBrain.Api/apis/` via extension methods.
-   **Auth0 Integration:**
    -   Backend: JWT Bearer validation (`Program.cs`)
    -   Frontend: Auth0 Next.js SDK (`src/middleware.ts`)
-   **Testing:**
    -   Backend: xUnit, Moq, FluentAssertions, in-memory EF for isolation
    -   Frontend: Jest, React Testing Library
-   **Environment Config:**
    -   Use `appsettings.{Environment}.json` for .NET
    -   Use `.env.local` for Next.js
-   **Deployment:**
    -   Azure Container Apps via `azd up`
    -   Config in `azure.yaml`, `next-steps.md`

## Integration Points

-   **Auth0:** Used for both frontend and backend authentication
-   **Database:** SQL Server (default), Cosmos DB (optional)
-   **Frontend/Backend comms:** REST API, CORS enabled

## Examples

-   **API endpoint mapping:** See `ProjectBrain.Api/apis/Users.cs`, `Conversations.cs`
-   **DB context:** `ProjectBrain.Database/AppDbContext.cs`
-   **Frontend Auth0:** `projectbrain.frontend/src/middleware.ts`
-   **Service orchestration:** `ProjectBrain.AppHost/Program.cs`

## Tips for AI Agents

-   Always use the orchestrator (`AppHost`) for local dev to ensure all services run together
-   Follow Arrange-Act-Assert in tests; use mocks/in-memory DB for isolation
-   Reference `CLAUDE.md` for more detailed architecture and workflow notes
-   Prefer updating/adding endpoint mappings via extension methods in `apis/`
-   For new models, update both EF context and migrations

---

For more details, see `CLAUDE.md` and `next-steps.md`.
