# Docker Local Development

This project includes a Docker setup for local development and portfolio demos only. It runs the ASP.NET Core API and PostgreSQL in Docker Compose while keeping EF Core migrations as an explicit host-side step.

## Services

- `api`
  - ASP.NET Core API container
  - runs in `Development`
  - exposed on `http://localhost:5172`
- `postgres`
  - PostgreSQL 16 container
  - exposed on host port `5433`
  - reachable from the API container as `postgres:5432`

## Start the stack

```bash
docker compose up --build -d
```

Once the stack is ready:

- Swagger: `http://localhost:5172/swagger`
- Health: `http://localhost:5172/health`

## Stop the stack

```bash
docker compose down
```

Remove the PostgreSQL volume as well:

```bash
docker compose down -v
```

## Connection string rules

Use the correct PostgreSQL host based on where the process is running.

Inside Docker:

```text
Host=postgres;Port=5432;Database=ai_usage_guard_dev;Username=postgres;Password=postgres
```

From the host machine:

```text
Host=localhost;Port=5433;Database=ai_usage_guard_dev;Username=postgres;Password=postgres
```

## Apply EF Core migrations to the Docker database

Start Docker Compose first so the PostgreSQL container is listening on host port `5433`.

PowerShell:

```powershell
$env:ConnectionStrings__DefaultConnection="Host=localhost;Port=5433;Database=ai_usage_guard_dev;Username=postgres;Password=postgres"
dotnet ef database update --project src\AIUsageGuard.Infrastructure\AIUsageGuard.Infrastructure.csproj --startup-project src\AIUsageGuard.Api\AIUsageGuard.Api.csproj
Remove-Item Env:ConnectionStrings__DefaultConnection
```

Bash:

```bash
ConnectionStrings__DefaultConnection="Host=localhost;Port=5433;Database=ai_usage_guard_dev;Username=postgres;Password=postgres" \
dotnet ef database update --project src/AIUsageGuard.Infrastructure/AIUsageGuard.Infrastructure.csproj --startup-project src/AIUsageGuard.Api/AIUsageGuard.Api.csproj
```

If you need to create a new migration:

```bash
dotnet ef migrations add <MigrationName> --project src/AIUsageGuard.Infrastructure/AIUsageGuard.Infrastructure.csproj --startup-project src/AIUsageGuard.Api/AIUsageGuard.Api.csproj
```

## Manual testing notes

- Swagger stays enabled because the API container runs in `Development`.
- The development antiforgery endpoint remains available at `GET /dev/antiforgery-token`.
- For protected write endpoints, first call `GET /dev/antiforgery-token`, then send the returned request token in the `X-CSRF-TOKEN` header.
- This applies to endpoints such as `POST /workspaces/{workspaceId}/events`.
