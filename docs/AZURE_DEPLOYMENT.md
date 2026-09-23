# Azure Deployment Manual

This guide deploys Billing Counter with this production topology:

```text
Browser -> Azure Container Apps (React + ASP.NET Core) -> Azure SQL Database
```

SQLite remains for local development only. GitHub Container Registry (GHCR) stores the image, so Azure Container Registry is not required.

## 1. Before you begin

You need:

- An Azure subscription with permission to create resources.
- A GitHub account with access to the repository.
- Docker Desktop using Linux containers.
- Git and a terminal opened at the repository root.
- A GitHub personal access token that can write and read packages.

Run the project checks before deploying:

```powershell
dotnet test BillingSoftware.slnx --configuration Release
Set-Location frontend/billing-web
npm.cmd ci
npm.cmd run lint
npm.cmd run build
Set-Location ../..
```

Do not continue if tests or the production build fail.

## 2. Choose names

Azure and GHCR names must be lowercase where indicated. Replace these examples throughout the guide:

| Resource | Example |
| --- | --- |
| Azure region | `Central India` |
| Resource group | `rg-billing-prod` |
| SQL logical server | `sql-billing-<unique-suffix>` |
| SQL database | `sqldb-billing-prod` |
| Container Apps environment | `cae-billing-prod` |
| Container App | `ca-billing-prod` |
| GHCR image | `ghcr.io/<github-owner>/billing-counter:2026-09-21-1` |

Use the same Azure region for Container Apps and Azure SQL when that region supports both services and the SQL free offer.

## 3. Create the Azure SQL free database

1. Open [Azure SQL hub](https://aka.ms/azuresqlhub).
2. Under **Create a database**, select **Start free**.
3. Confirm the page displays **Free offer applied**.
4. Select your subscription.
5. Create or select resource group `rg-billing-prod`.
6. Enter database name `sqldb-billing-prod`.
7. Create a new logical SQL server in the same region planned for Container Apps.
8. Choose **SQL authentication** for this initial deployment.
9. Create a unique administrator username and a strong password. Store them in a password manager.
10. Under **Behavior when free limit reached**, select **Auto-pause the database until next month**.
11. Confirm the cost summary displays an estimated monthly database cost of zero.
12. Select **Review + create**, then **Create**.

The current free offer includes 100,000 vCore-seconds, 32 GB of data, and 32 GB of backup storage per database each month. Verify the current limits in the portal before deployment.

### Configure SQL networking

For the lowest-complexity initial deployment:

1. Open the new SQL logical server, not only the database.
2. Open **Networking**.
3. Keep **Public network access** enabled.
4. Enable **Allow Azure services and resources to access this server**.
5. Save the configuration.

This rule permits connections from Azure services but still requires valid SQL credentials and encrypted TLS. A private endpoint or VNet integration is more restrictive but adds complexity and can add cost.

### Prepare the connection string

Use this format, replacing every placeholder:

```text
Server=tcp:<server-name>.database.windows.net,1433;Initial Catalog=sqldb-billing-prod;Persist Security Info=False;User ID=<sql-admin>;Password=<sql-password>;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;
```

Do not save the completed connection string in Git, README files, terminal history, screenshots, or chat.

## 4. Build and publish the container image

Use an immutable image tag such as a date plus build number or a Git commit SHA. Do not use `latest` for production.

1. In GitHub, create a personal access token with package read/write permissions. Private repositories may also require repository access.
2. Sign in to GHCR:

```powershell
docker login ghcr.io -u <github-username>
```

3. When Docker asks for a password, paste the token directly into the terminal.
4. Build the Linux image from the repository root:

```powershell
docker build --tag ghcr.io/<github-owner>/billing-counter:2026-09-21-1 .
```

5. Push it:

```powershell
docker push ghcr.io/<github-owner>/billing-counter:2026-09-21-1
```

6. Open the package in GitHub and confirm the pushed tag exists.

For a private package, create a separate read-only token for Azure Container Apps. It only needs permission to read packages.

## 5. Generate production secrets

Generate a JWT signing key locally:

```powershell
$bytes = New-Object byte[] 48
$generator = [Security.Cryptography.RandomNumberGenerator]::Create()
try {
	$generator.GetBytes($bytes)
	[Convert]::ToBase64String($bytes)
}
finally {
	$generator.Dispose()
}
```

Store the output in a password manager. Do not commit it.

Also prepare strong initial passwords for:

- One Admin account.
- One Billing staff account.
- GHCR read access if the package is private.

## 6. Create the Container App

1. In the Azure portal, select **Create a resource** and search for **Container App**.
2. Select **Create**.
3. Choose the same subscription and `rg-billing-prod` resource group.
4. Enter `ca-billing-prod` as the app name.
5. Create environment `cae-billing-prod` in the same region as Azure SQL.
6. Select the **Consumption** workload profile.
7. For the portal's separate registry fields, enter:
	- **Registry login server:** `ghcr.io`
	- **Image/repository:** `<github-owner>/billing-counter` (do not include `ghcr.io/` again)
	- **Image tag:** `2026-09-21-1`
8. If the image is private, also configure your GitHub username and the read-only package token.
9. Allocate **0.5 CPU** and **1 GiB memory** initially.
10. Enable external ingress.
11. Set ingress traffic to HTTPS only.
12. Set target port to `8080`.
13. Use **Single revision** mode.

Do not create an Azure Files mount. Production data belongs in Azure SQL.

## 7. Configure Container Apps secrets

Open the Container App and go to **Security > Secrets**. Add these secrets:

| Secret name | Value |
| --- | --- |
| `billing-db` | Complete Azure SQL connection string |
| `jwt-signing-key` | Generated 48-byte signing key |
| `bootstrap-admin-password` | Strong Admin password |
| `bootstrap-staff-password` | Strong Billing staff password |

Use lowercase secret names. If GHCR is private, its token is also stored as a registry credential by Container Apps.

## 8. Configure environment variables

Environment variables belong to the container inside a revision. They are not shown directly on the initial **Create new revision** page.

In the Azure portal:

1. Open `ca-billing-prod`.
2. Select **Application > Revisions and replicas** in the left menu.
3. Select **Create new revision**.
4. Find the **Container image** or **Containers** section.
5. Select the existing container named `ca-billing-prod`, or select its **Edit** button.
6. In the container-edit panel, scroll to **Environment variables**.
7. Select **Add** once for each variable below.
8. For a secret-backed value, set **Source** to **Reference a secret**, then select the secret. Do not paste the secret as a manual value.
9. Select **Save** in the container-edit panel.
10. Before deploying, confirm the revision's ingress **Target port** is `8080`. Azure can otherwise infer or retain port `80`, while this image listens on `8080`.
11. Select **Create** or **Deploy** on the revision page.

Add these variables to the application container:

| Environment variable | Source | Value |
| --- | --- | --- |
| `ASPNETCORE_ENVIRONMENT` | Manual | `Production` |
| `Database__Provider` | Manual | `SqlServer` |
| `ConnectionStrings__Billing` | Secret reference | `billing-db` |
| `Authentication__SigningKey` | Secret reference | `jwt-signing-key` |
| `Authentication__BootstrapUsersEnabled` | Manual | `true` |
| `Authentication__BootstrapUsers__0__Username` | Manual | Your Admin username |
| `Authentication__BootstrapUsers__0__Password` | Secret reference | `bootstrap-admin-password` |
| `Authentication__BootstrapUsers__0__Role` | Manual | `Admin` |
| `Authentication__BootstrapUsers__1__Username` | Manual | Your staff username |
| `Authentication__BootstrapUsers__1__Password` | Secret reference | `bootstrap-staff-password` |
| `Authentication__BootstrapUsers__1__Role` | Manual | `BillingStaff` |

Double underscores are intentional. ASP.NET Core uses them for nested configuration.

If the portal still does not expose the container editor, use Azure Cloud Shell or a local terminal with Azure CLI. Sign in with `az login`, then run:

```powershell
az containerapp update `
	--name ca-billing-prod `
	--resource-group rg-billing-prod `
	--set-env-vars `
		ASPNETCORE_ENVIRONMENT=Production `
		Database__Provider=SqlServer `
		ConnectionStrings__Billing=secretref:billing-db `
		Authentication__SigningKey=secretref:jwt-signing-key `
		Authentication__BootstrapUsersEnabled=true `
		Authentication__BootstrapUsers__0__Username=<admin-username> `
		Authentication__BootstrapUsers__0__Password=secretref:bootstrap-admin-password `
		Authentication__BootstrapUsers__0__Role=Admin `
		Authentication__BootstrapUsers__1__Username=<staff-username> `
		Authentication__BootstrapUsers__1__Password=secretref:bootstrap-staff-password `
		Authentication__BootstrapUsers__1__Role=BillingStaff
```

Replace only `<admin-username>` and `<staff-username>`. This update creates a new revision because environment variables are revision-scoped.

The first successful startup will:

1. Connect to Azure SQL.
2. Apply SQL Server migrations.
3. Seed menu categories and starter items.
4. Hash and store the two bootstrap passwords.

Keep the SQL login sufficiently privileged to apply EF Core migrations while this startup-migration model is used.

## 9. Configure scaling

1. Open the Container App.
2. Select **Scale** and then **Edit and deploy**.
3. Open **Scale and replicas**.
4. Set minimum replicas to `0`.
5. Set maximum replicas to `1`.
6. Add an HTTP scaling rule if the portal requires one; the default HTTP rule is otherwise sufficient.

A minimum of zero minimizes compute cost but causes a cold start after idle periods. Maximum one avoids simultaneous startup migrations and is sufficient for a small billing counter.

## 10. Configure health probes

The application exposes `GET /health`, which checks database connectivity.

Configure HTTP probes on port `8080`:

| Probe | Path | Suggested initial delay | Period | Failure threshold |
| --- | --- | ---: | ---: | ---: |
| Startup | `/health` | 10 seconds | 5 seconds | 30 |
| Readiness | `/health` | 5 seconds | 10 seconds | 6 |
| Liveness | `/health` | 30 seconds | 30 seconds | 3 |

Azure SQL may need time to resume from auto-pause, so the startup probe must allow a reasonably long startup window.

## 11. Verify the first deployment

1. Open the Container App **Overview** page.
2. Wait until the latest revision is healthy.
3. Open its HTTPS application URL.
4. Sign in with the production Admin account.
5. Confirm menu items and categories load.
6. Create a clearly labeled test order.
7. Open **Order history** and confirm it appears.
8. Open **Menu**, edit an item category, and verify the billing card changes.
9. Open `https://<container-app-host>/health` and confirm HTTP 200.
10. Sign out and verify the Billing staff account cannot access Menu management.

If startup fails, open **Monitoring > Log stream** and check for SQL networking, authentication, or migration errors.

## 12. Disable bootstrap configuration

After both accounts can sign in:

1. Create a new Container App revision.
2. Set `Authentication__BootstrapUsersEnabled` to `false`.
3. Remove both bootstrap password environment-variable references.
4. Deploy the revision and verify login still works.
5. Deactivate older revisions that still reference bootstrap password secrets.
6. Delete `bootstrap-admin-password` and `bootstrap-staff-password` from Container Apps secrets.

Changing or deleting a Container Apps secret does not automatically update existing revisions. Deploy or restart the relevant revision.

## 13. Deploy an update

1. Run tests and builds locally.
2. Choose a new immutable tag.
3. Build and push the image:

```powershell
docker build --tag ghcr.io/<github-owner>/billing-counter:<new-tag> .
docker push ghcr.io/<github-owner>/billing-counter:<new-tag>
```

4. In Container Apps, select **Revisions and replicas > Create new revision**.
5. Change only the image tag unless configuration also changed.
6. Confirm the revision's ingress **Target port** remains `8080` before deploying.
7. Wait for `/health` to report healthy.
8. Test login, one menu request, and order history.
9. Deactivate the old revision after verification.

Do not reuse an old tag because image caching makes deployments difficult to diagnose.

## 14. Roll back

If a new revision fails:

1. Open **Revisions and replicas**.
2. Reactivate the previous healthy revision or direct traffic back to it.
3. Review the failed revision's logs.

Database migrations are automatically applied and are not automatically reversed during an application rollback. Design future migrations to remain compatible with the immediately previous application version.

## 15. Cost controls

1. Create an Azure Cost Management budget for the subscription or resource group.
2. Add alerts at low thresholds appropriate for your currency.
3. Keep Container Apps minimum replicas at zero.
4. Keep maximum replicas at one until load requires more.
5. Keep Azure SQL set to **Auto-pause the database until next month** at the free limit.
6. On Azure SQL **Metrics**, alert when **Free amount remaining** falls below 10,000 vCore-seconds.
7. Avoid enabling unnecessary high-volume diagnostic logging or long log retention.
8. Review GHCR package storage and transfer limits for your GitHub plan.

With light usage, Container Apps may remain inside its monthly free grant and Azure SQL inside its free offer. Pricing and allowances can change, so verify the Azure pricing pages before deployment.

## 16. Backup and recovery

Azure SQL free databases include backup storage and limited point-in-time restore retention. Before production use:

1. Confirm automated backups are enabled.
2. Record the point-in-time restore retention shown in the portal.
3. Perform a test restore into a temporary database.
4. Delete the temporary database after verification.
5. Export business-critical reports separately when required by accounting policy.

## 17. Troubleshooting

### Container revision never becomes healthy

- Confirm target port is `8080`.
- Confirm the image is Linux `amd64` and GHCR credentials can pull it.
- Check that every secret reference exists.
- Check SQL networking and credentials.
- Allow enough startup-probe time for Azure SQL to resume and migrations to run.

### `TargetPort 80 does not match the listening port 8080`

The image listens on port `8080`, but the revision is routing traffic to port `80`. Create a new revision, set ingress **Target port** to `8080`, and deploy it. Also keep all HTTP health probes on port `8080`. The revision should change from **Activating** to **Running** after the corrected revision starts.

### `Authentication:SigningKey` startup error

The `Authentication__SigningKey` secret reference is missing or resolves to fewer than 32 bytes.

### SQL login or firewall error

Confirm the SQL connection string, server name, administrator username, password, TLS settings, and **Allow Azure services and resources to access this server** setting.

### SQL error 40613: database is not currently available

The Container App reached Azure SQL, but a paused serverless/free database was still resuming. Open the database in the Azure portal and confirm its status changes from **Paused** or **Resuming** to **Online**. Opening **Query editor** and signing in also triggers a resume. The application image includes transient retries for this expected cold-start condition; restart or deploy the active revision after the database is online if its earlier retry window already expired.

### `Globalization Invariant Mode is not supported`

`Microsoft.Data.SqlClient` requires globalization data. The Alpine runtime image must install `icu-libs` and set `DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false`. Rebuild and deploy a new immutable image tag after changing the Dockerfile; setting only an Azure environment variable does not install the required ICU package.

### Login fails after first deployment

Confirm bootstrap was enabled on the first successful startup and inspect the `Users` table with the Azure SQL query editor. Do not re-enable bootstrap with known or weak passwords.

### The app is slow on the first request

Both Container Apps and Azure SQL can be paused while idle. Their first request can experience a cold start. Set the Container App minimum replicas to one only if faster response is worth the additional compute cost.

## Official references

- [Azure Container Apps containers](https://learn.microsoft.com/azure/container-apps/containers)
- [Container Apps secrets](https://learn.microsoft.com/azure/container-apps/manage-secrets)
- [Container Apps scaling](https://learn.microsoft.com/azure/container-apps/scale-app)
- [Container Apps health probes](https://learn.microsoft.com/azure/container-apps/health-probes)
- [Azure SQL Database free offer](https://learn.microsoft.com/azure/azure-sql/database/free-offer)
