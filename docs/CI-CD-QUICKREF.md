# CI/CD Quick Reference

## Quick Commands

### Local Testing

```bash
# Build and test locally before pushing
dotnet build Enterprise.sln --configuration Release
dotnet test --configuration Release --no-build

# Test Docker build
docker build -t enterprise-api:local .
docker run -p 5000:8080 enterprise-api:local
```

### Trigger Deployments

**GitHub Actions:**

```bash
# Push to main triggers full pipeline
git push origin main

# Manual deployment
gh workflow run cd.yml --ref main -f environment=production
```

**Azure DevOps:**

```bash
# Queue new build
az pipelines run --name "Enterprise-CI-CD" --branch main
```

## Pipeline Status

| Pipeline | Status | Purpose |
|----------|--------|---------|
| [ci.yml](.github/workflows/ci.yml) | Build & Test | Runs on all PRs and pushes |
| [docker.yml](.github/workflows/docker.yml) | Container Build | Builds Docker images on main |
| [cd.yml](.github/workflows/cd.yml) | Deployment | Deploys to staging/production |
| [azure-pipelines.yml](../azure-pipelines.yml) | Full CI/CD | Azure DevOps end-to-end pipeline |

## Required Secrets

### GitHub Actions

- `AZURE_CREDENTIALS_STAGING`
- `AZURE_CREDENTIALS_PRODUCTION`
- `AZURE_WEBAPP_NAME_STAGING`
- `AZURE_WEBAPP_NAME_PRODUCTION`
- `AZURE_RESOURCE_GROUP`
- `SLACK_WEBHOOK` (optional)

### Azure DevOps

- Service Connection: `AzureServiceConnection`
- Service Connection: `AzureContainerRegistry`
- Variables: `AzureWebAppNameStaging`, `AzureWebAppNameProduction`

## Deployment Workflow

```
┌─────────────┐
│   Code Push │
└──────┬──────┘
       │
       ▼
┌─────────────┐
│   CI Build  │ ← Builds, runs tests, security scans
└──────┬──────┘
       │
       ▼
┌─────────────┐
│Docker Build │ ← Creates container image
└──────┬──────┘
       │
       ▼
┌─────────────┐
│Deploy Stg   │ ← Deploys to staging, health check
└──────┬──────┘
       │
       ▼ (manual approval)
┌─────────────┐
│Deploy Prod  │ ← Blue-green swap to production
└─────────────┘
```

## Emergency Procedures

### Rollback Production

```bash
# GitHub Actions - Manual dispatch cd.yml with previous commit SHA
# OR Azure CLI:
az webapp deployment slot swap \
  --resource-group YOUR_RG \
  --name YOUR_WEBAPP \
  --slot production \
  --target-slot staging
```

### Skip CI for Commits

```bash
git commit -m "docs: update README [skip ci]"
```

### Force Rerun Failed Pipeline

- **GitHub**: Re-run failed jobs button in Actions tab
- **Azure DevOps**: Stage → Rerun failed jobs

## Monitoring

- **GitHub**: Repository → Actions tab → Workflow runs
- **Azure DevOps**: Pipelines → Recent → Build/Release
- **Application**: Check `/health` endpoint after deployment
- **Logs**: `az webapp log tail --name YOUR_WEBAPP --resource-group YOUR_RG`

## Next Steps After Setup

1. ✅ Add secrets to GitHub/Azure DevOps
2. ✅ Test CI pipeline with a PR
3. ✅ Configure deployment environments
4. ✅ Set up Slack notifications (optional)
5. ✅ Enable branch protection rules
6. ✅ Schedule regular security scans

## Troubleshooting

| Issue | Solution |
|-------|----------|
| Build fails | Check NuGet restore: `dotnet restore --force` |
| Tests fail | Verify connection strings in test settings |
| Docker build fails | Test locally: `docker build -t test .` |
| Deployment timeout | Check Azure Web App logs |
| Health check fails | Verify `/health` endpoint is accessible |

For detailed documentation, see [CI-CD-PIPELINES.md](CI-CD-PIPELINES.md)
