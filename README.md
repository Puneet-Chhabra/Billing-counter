# Billing Counter

A React/Vite billing interface backed by an ASP.NET Core API. The API owns authentication, pricing, GST calculations, order numbering, and persistence.

See [docs/USER_MANUAL.md](docs/USER_MANUAL.md) for role-based operating instructions and a local test checklist. Follow [docs/AZURE_DEPLOYMENT.md](docs/AZURE_DEPLOYMENT.md) for the complete production deployment procedure.

## Architecture

- `frontend/billing-web`: React 19 single-page application.
- `backend/Billing.Api`: HTTP API, JWT authentication, health checks, and production static-file host.
- `backend/Billing.Application`: billing calculations.
- `backend/Billing.Domain`: entities and business data structures.
- `backend/Billing.Infrastructure`: EF Core context, authentication, orders, and SQLite migrations.
- `backend/Billing.Migrations.SqlServer`: Azure SQL migrations and an idempotent migration script.
- `backend/tests/Billing.UnitTests`: calculator and hosted API integration tests.

Development uses SQLite. Production uses Azure SQL by setting `Database__Provider=SqlServer`. The Docker image builds the frontend and serves it from the API, giving production one origin and one deployable container.

## Prerequisites

- .NET 10 SDK
- Node.js 24
- Docker, only when building the deployment image

## Run locally

Start the API:

```powershell
dotnet run --project backend/Billing.Api/Billing.Api.csproj --urls http://localhost:5000
```

In another terminal, start the UI:

```powershell
Set-Location frontend/billing-web
npm.cmd ci
npm.cmd run dev
```

Development creates `billing.db`, applies migrations, and bootstraps these local-only accounts:

- Admin: `admin` / `admin123`
- Billing staff: `staff` / `staff123`

The Vite app calls `http://localhost:5000/api`. Set `VITE_API_URL` only when the local API uses another address.

## Validate

```powershell
dotnet test BillingSoftware.slnx --configuration Release
Set-Location frontend/billing-web
npm.cmd ci
npm.cmd run lint
npm.cmd run build
Set-Location ../..
docker build --tag billing-counter:local .
```

## Database migrations

Create a local SQLite migration:

```powershell
dotnet ef migrations add MigrationName --project backend/Billing.Infrastructure --startup-project backend/Billing.Api --output-dir Migrations
```

For Azure SQL, first set `Database__Provider=SqlServer` and a SQL Server `ConnectionStrings__Billing`, then run:

```powershell
dotnet ef migrations add MigrationName --project backend/Billing.Migrations.SqlServer --startup-project backend/Billing.Api --output-dir Migrations
dotnet ef migrations script --idempotent --project backend/Billing.Migrations.SqlServer --startup-project backend/Billing.Api --output backend/Billing.Migrations.SqlServer/migrate.sql
```

The API applies pending migrations during startup. Keep the production Container App at a maximum of one replica while startup migration is enabled.

## Low-cost Azure deployment

Recommended resources:

1. **Azure Container Apps Consumption** for the combined UI/API image. Configure `minReplicas=0`, `maxReplicas=1`, target port `8080`, and health probe `/health`.
2. **Azure SQL Database free offer** for durable data. Select **Auto-pause the database until next month** when its free allowance is exhausted to prevent unexpected database charges.

Publish the container image to GitHub Container Registry so no Azure Container Registry resource is required. For a private image, configure the GitHub Container Registry credentials as Container Apps registry secrets.

Container App configuration:

| Setting | Value |
| --- | --- |
| `Database__Provider` | `SqlServer` |
| `ConnectionStrings__Billing` | Azure SQL connection string, stored as a secret |
| `Authentication__SigningKey` | Random secret of at least 32 bytes |
| `Authentication__BootstrapUsersEnabled` | `true` for the first deployment, then `false` |
| `Authentication__BootstrapUsers__0__Username` | Initial admin username |
| `Authentication__BootstrapUsers__0__Password` | Strong initial password, stored as a secret |
| `Authentication__BootstrapUsers__0__Role` | `Admin` |

After the first healthy deployment and successful login, set `Authentication__BootstrapUsersEnabled=false` and remove the bootstrap password secret. Do not use the development accounts in Azure.

Build and smoke-test the image locally:

```powershell
docker build --tag billing-counter:local .
docker run --rm -p 8080:8080 `
	-e Authentication__SigningKey="replace-with-a-random-secret-at-least-32-bytes" `
	-e Authentication__BootstrapUsersEnabled=true `
	-e Authentication__BootstrapUsers__0__Username=admin `
	-e Authentication__BootstrapUsers__0__Password="replace-with-a-strong-password" `
	-e Authentication__BootstrapUsers__0__Role=Admin `
	billing-counter:local
```

This local container defaults to SQLite inside the disposable container and is only a smoke test. Azure must use Azure SQL.

Production topology:

```text
Browser -> Azure Container Apps (React + ASP.NET Core) -> Azure SQL Database
```

## Cost and operations guardrails

- Create an Azure Cost Management budget and alerts before deployment.
- Keep Container Apps at zero minimum replicas; expect a cold start after idle periods.
- Select the Azure SQL free-offer stop-at-limit behavior rather than paid overage.
- Keep the automatic Azure SQL backups enabled and periodically test a restore.
- Keep customer data out of application logs.
- Rotate the JWT signing key and administrator password if either is exposed.

## API

- `POST /api/auth/login`
- `GET /api/menu`
- `GET /api/menu/manage` (Admin)
- `POST /api/menu` (Admin)
- `PUT /api/menu/{id}` (Admin)
- `POST /api/orders`
- `POST /api/orders/preview`
- `GET /api/orders`
- `GET /api/orders/{id}`
- `GET /health`

All menu and order endpoints require a bearer token. Order creation reloads current menu prices from the database and snapshots item name, price, GST percentage, quantity, and total.
