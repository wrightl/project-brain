# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Structure

ProjectBrain is a full-stack application built with .NET Aspire hosting Next.js frontend and ASP.NET Core Web API backend. The solution consists of:

- **ProjectBrain.AppHost** - .NET Aspire orchestrator that manages all services, databases, and deployment configuration
- **ProjectBrain.Api** - ASP.NET Core Web API with Auth0 JWT authentication, OpenAPI/Scalar documentation
- **ProjectBrain.Database** - Entity Framework Core data layer with SQL Server support (Cosmos DB support commented out)
- **ProjectBrain.ServiceDefaults** - Shared Aspire service configurations
- **projectbrain.frontend** - Next.js 15 React frontend with Auth0 authentication, TailwindCSS, and TypeScript

## Development Commands

### .NET Backend
- `dotnet run --project ProjectBrain.AppHost` - Run the complete application stack including database, API, and frontend
- `dotnet build` - Build all .NET projects
- `dotnet test` - Run tests (if any exist)

### Frontend 
- Navigate to `projectbrain.frontend/` directory first
- `npm run dev` - Start Next.js development server with turbopack and experimental HTTPS
- `npm run build` - Build for production
- `npm run lint` - Run ESLint

## Architecture Overview

### Backend API Structure
The API uses minimal APIs with endpoint mapping:
- Weather forecast endpoints (`WeatherForecast.cs`)
- Movie endpoints with Entity Framework (`Movies.cs`, `MovieService.cs`)
- Egg endpoints with Entity Framework (`Eggs.cs`, `EggService.cs`)
- JWT Bearer authentication via Auth0
- CORS configured for cross-origin requests

### Database
- Primary: SQL Server via Entity Framework Core
- Models: `Movie`, `Egg` entities
- Context: `AppDbContext` with DbSet properties
- Initializer: `ProjectBrainDbInitializer` for seeding data
- Commented-out Cosmos DB support available

### Authentication
Both frontend and backend use Auth0:
- Backend: JWT Bearer token validation
- Frontend: `@auth0/nextjs-auth0` for session management
- Custom authentication extensions in `Authentication/AuthenticationExtensions.cs`

### Deployment
- **Development**: Aspire runs NPM app with hot reload
- **Production**: Frontend built as Docker container, scaled to 0 replicas when idle
- Azure deployment via `azd up` command using Container Apps
- Configuration in `azure.yaml` and `next-steps.md`

## Key Files to Understand

- `ProjectBrain.AppHost/Program.cs:70-104` - Service orchestration and environment-specific frontend handling
- `ProjectBrain.Api/Program.cs:22-23` - Authentication setup
- `projectbrain.frontend/src/middleware.ts` - Auth0 middleware configuration
- `ProjectBrain.Database/AppDbContext.cs:12-13` - Entity sets for Movies and Eggs

## Common Patterns

- Minimal APIs with endpoint mapping extensions
- Service-based data access layer
- Aspire service defaults for observability and configuration
- Environment-specific configuration (Development vs Publish mode)
- Auth0 integration across frontend and backend