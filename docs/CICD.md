# CI/CD Deployment Guide

This project uses an Azure Container Apps generated GitHub Actions workflow to validate, package, and deploy the application automatically.

Workflow file:

```text
.github/workflows/ca-billing-prod-AutoDeployTrigger-f504e7aa-a4ce-4d36-87bb-6fc9667be767.yml
```

## Deployment flow

A push to the `main` branch starts this sequence:

```text
Push to main
  -> Run backend tests
  -> Install, lint, and build the frontend
  -> Build the root Dockerfile
  -> Push an immutable image to GHCR
  -> Authenticate to Azure with OIDC
  -> Update ca-billing-prod
  -> Azure Container Apps creates a new revision
```

Images use the full Git commit SHA as the tag:

```text
ghcr.io/rishabhc1101/billing-counter:<commit-sha>
```

This avoids stale image caching and identifies the source commit for every deployment.

## One-time GitHub configuration

Open the GitHub repository, then go to **Settings > Secrets and variables > Actions**.

The Azure portal integration creates or expects these repository secrets:

| Secret | Purpose |
| --- | --- |
| `CABILLINGPROD_AZURE_CLIENT_ID` | Microsoft Entra application/client ID used by OIDC |
| `CABILLINGPROD_AZURE_TENANT_ID` | Azure tenant ID |
| `CABILLINGPROD_AZURE_SUBSCRIPTION_ID` | Azure subscription containing the Container App |
| `CABILLINGPROD_REGISTRY_USERNAME` | GitHub account that owns or can access the GHCR package |
| `CABILLINGPROD_REGISTRY_PASSWORD` | GitHub token used to push and read the GHCR package |

`CABILLINGPROD_REGISTRY_PASSWORD` must be a GitHub token with:

- `read:packages`
- `write:packages`
- Repository access when the repository or package is private

Never put token values, Azure IDs, passwords, or connection strings directly in the workflow file.

Azure authentication uses OIDC, so no long-lived Azure client secret is stored in GitHub. The federated identity must trust this repository and the `main` branch, and its service principal requires **Container Apps Contributor** access to `ca-billing-prod` or its resource group.

## Container App settings preserved by CI/CD

The workflow updates only the image. Configure these settings once in Azure and keep them outside the workflow:

- Ingress target port: `8080`
- Health probes: `/health` on port `8080`
- Minimum replicas: `0`
- Maximum replicas: `1`
- Azure SQL and JWT secrets
- Production environment variables
- GHCR pull credentials

Before enabling automatic deployment, confirm the current healthy revision has these settings. An image-only update should inherit them.

## Automatic deployment

Commit all intended changes and push them to `main`:

```powershell
git status
git add <files-to-include>
git commit -m "Describe the change"
git push origin main
```

Open the GitHub repository and select **Actions**. Choose **Trigger auto deployment for ca-billing-prod** to monitor the run.

The Azure deployment starts only after backend tests, frontend linting, and frontend compilation succeed.

## Manual deployment trigger

To redeploy the current `main` branch without making another commit:

1. Open GitHub **Actions**.
2. Select **Trigger auto deployment for ca-billing-prod**.
3. Select **Run workflow**.
4. Choose branch `main`.
5. Select **Run workflow** again.

The workflow builds a new image tagged with the current commit SHA. Re-running the same commit can reuse the same image tag, so use a new commit when a distinct immutable deployment is required.

## Verify a deployment

After the workflow succeeds:

1. Open Azure Portal and select `ca-billing-prod`.
2. Open **Revisions and replicas**.
3. Confirm the latest revision is **Running** and receives 100% traffic.
4. Confirm its image tag matches the GitHub commit SHA.
5. Open the main Container App URL.
6. Verify `/health` returns HTTP 200.
7. Test Admin login, menu loading, order creation, and order history.

Use the main Container App URL, not a revision-specific URL, for normal access.

## Roll back

If a deployment is unhealthy:

1. Open **Revisions and replicas** in Azure Portal.
2. Reactivate the previous healthy revision or direct traffic to it.
3. Review the failed GitHub Actions run and Container App logs.
4. Fix the issue and push a new commit rather than reusing the failed tag.

Database migrations run during application startup and are not automatically reversed when the image is rolled back. Keep migrations compatible with the immediately previous application version.

## Troubleshooting

### Workflow contains placeholder keys

Remove generated entries such as `_dockerfilePathKey_`, `_targetLabelKey_`, and `_buildArgumentsKey_`. This repository uses:

```yaml
appSourcePath: ${{ github.workspace }}
dockerfilePath: Dockerfile
imageToBuild: ghcr.io/rishabhc1101/billing-counter:${{ github.sha }}
```

### GHCR push is denied

Confirm `CABILLINGPROD_REGISTRY_USERNAME` is correct and `CABILLINGPROD_REGISTRY_PASSWORD` has `write:packages`. If package permissions inherit from the repository, confirm the workflow repository has write access to the package.

### Azure Login fails

Confirm all three Azure ID secrets exist. In Microsoft Entra ID, verify the federated credential references the correct GitHub owner, repository, and `main` branch. Confirm the identity has **Container Apps Contributor** access.

### New revision uses the wrong port

The application listens on `8080`. In Azure Container Apps, set ingress target port and all HTTP probes to `8080`. An image-only workflow update should preserve this setting.

### Deployment succeeds but the app does not start

Open the Container App **Log stream**. Common causes are Azure SQL resuming, invalid secret references, SQL networking, or a missing production signing key. See [AZURE_DEPLOYMENT.md](AZURE_DEPLOYMENT.md) for the complete troubleshooting guide.

## Disable automatic deployment

To pause automatic deployment without deleting Azure resources, disable the workflow from GitHub **Actions**, or remove its `push` trigger and retain only `workflow_dispatch`.

Do not configure a second workflow to deploy the same Container App from the same branch. Two deployment workflows can create competing revisions.
