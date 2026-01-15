# CI/CD Pipeline Documentation

## Overview

This project includes comprehensive CI/CD pipelines for both **GitHub Actions** and **Azure DevOps**, supporting automated build, test, security scanning, Docker image creation, and deployment to staging/production environments.

## Pipeline Architecture

```
┌─────────────┐     ┌──────────────┐     ┌─────────────┐
│   CI Build  │────▶│ Docker Build │────▶│ Deploy Stg  │
│   & Test    │     │  & Security  │     │  & Prod     │
└─────────────┘     └──────────────┘     └─────────────┘
```

## GitHub Actions Pipelines

### 1. CI Pipeline (`.github/workflows/ci.yml`)

**Triggers:**

- Push to `main` or `develop` branches
- Pull requests to `main` or `develop`
- Manual workflow dispatch

**Jobs:**

- **Build and Test**
  - Restores dependencies
  - Builds .NET 8 solution
  - Runs unit tests (Application.Tests)
  - Runs integration tests (Integration.Tests)
  - Publishes test results and build artifacts
  
- **Code Quality Analysis**
  - Runs `dotnet format` verification
  - Optional SonarCloud integration (commented)
  
- **Security Scan**
  - Checks for vulnerable NuGet packages
  - Runs Trivy filesystem security scan
  - Uploads results to GitHub Security tab

### 2. Docker Pipeline (`.github/workflows/docker.yml`)

**Triggers:**

- Push to `main` branch
- Git tags matching `v*.*.*`
- Manual workflow dispatch

**Features:**

- Builds multi-platform Docker images (amd64, arm64)
- Pushes to GitHub Container Registry (`ghcr.io`)
- Tags images with:
  - Branch name
  - Git SHA
  - Semantic version (from tags)
  - `latest` (for main branch)
- Runs Trivy vulnerability scan on images
- Supports Azure Container Registry (commented)

### 3. CD Pipeline (`.github/workflows/cd.yml`)

**Triggers:**

- After successful CI and Docker pipeline completion
- Manual workflow dispatch with environment selection

**Deployment Stages:**

#### Staging Deployment

- Deploys to Azure Web App (staging)
- Runs database migrations (if configured)
- Performs health checks
- Sends Slack notifications

#### Production Deployment

- Blue-Green deployment using Azure slot swaps
- Deploys to staging slot first
- Health check on staging slot
- Swaps to production slot
- Production health verification
- Creates GitHub release for tagged versions
- Automatic rollback on failure

**Alternative Kubernetes Deployment** (commented):

- kubectl configuration
- Manifest deployment
- Rollout verification

## Azure DevOps Pipeline (`azure-pipelines.yml`)

### Stages

1. **Build**
   - Install .NET 8 SDK
   - Restore, build, and test solution
   - Publish code coverage reports
   - Create artifacts for WebApi and Commands projects

2. **Security Scan**
   - Check for vulnerable packages
   - Optional SonarQube integration

3. **Docker**
   - Build Docker image
   - Push to Azure Container Registry
   - Only runs on `main` branch

4. **Deploy Staging**
   - Deploy container to Azure Web App (staging)
   - Run database migrations
   - Health check verification

5. **Deploy Production**
   - Stop Azure Web App
   - Deploy container
   - Start Azure Web App
   - Health check verification
   - Create GitHub release for tags

## Required Secrets and Variables

### GitHub Actions Secrets

Create these in GitHub Settings → Secrets → Actions:

```bash
# Azure Deployment
AZURE_CREDENTIALS_STAGING          # Azure service principal JSON for staging
AZURE_CREDENTIALS_PRODUCTION       # Azure service principal JSON for production
AZURE_WEBAPP_NAME_STAGING         # Staging web app name
AZURE_WEBAPP_NAME_PRODUCTION      # Production web app name
AZURE_RESOURCE_GROUP              # Azure resource group name

# Azure Container Registry (if using ACR instead of GHCR)
ACR_LOGIN_SERVER                  # e.g., myregistry.azurecr.io
ACR_USERNAME                      # ACR username
ACR_PASSWORD                      # ACR password

# Notifications
SLACK_WEBHOOK                     # Slack webhook URL for notifications

# Optional
SONAR_TOKEN                       # SonarCloud authentication token
KUBE_CONFIG                       # Kubernetes config for K8s deployments
```

### Azure DevOps Variables

Configure in Azure DevOps → Pipelines → Library:

```yaml
# Variable Group: "Enterprise-CI-CD"
AzureWebAppNameStaging           # Staging web app name
AzureWebAppNameProduction        # Production web app name
ContainerRegistry                # ACR URL (e.g., myregistry.azurecr.io)
```

### Azure DevOps Service Connections

Create these in Azure DevOps → Project Settings → Service Connections:

1. **AzureServiceConnection** - Azure Resource Manager connection
2. **AzureContainerRegistry** - Docker registry connection
3. **GitHubServiceConnection** - GitHub connection for releases
4. **SonarQubeServiceConnection** - SonarQube connection (optional)

## Setup Instructions

### GitHub Actions Setup

1. **Enable GitHub Actions**

   ```bash
   # Ensure .github/workflows/ directory exists (already created)
   ```

2. **Configure GitHub Container Registry**
   - Go to repository Settings → Actions → General
   - Enable "Read and write permissions" for GITHUB_TOKEN

3. **Add Azure Credentials**

   ```bash
   # Create Azure service principal
   az ad sp create-for-rbac --name "github-actions-enterprise" \
     --role contributor \
     --scopes /subscriptions/{subscription-id}/resourceGroups/{resource-group} \
     --sdk-auth
   
   # Copy JSON output to AZURE_CREDENTIALS_STAGING and AZURE_CREDENTIALS_PRODUCTION
   ```

4. **Configure Environment Protection**
   - Go to Settings → Environments
   - Create `staging` and `production` environments
   - Add required reviewers for production deployments
   - Add environment secrets

### Azure DevOps Setup

1. **Import Pipeline**

   ```bash
   # In Azure DevOps:
   # Pipelines → New Pipeline → Existing YAML file → Select azure-pipelines.yml
   ```

2. **Create Service Connections**
   - Azure subscription connection
   - Azure Container Registry connection
   - GitHub connection (for releases)

3. **Configure Variable Groups**

   ```bash
   # Pipelines → Library → + Variable group
   # Name: "Enterprise-CI-CD"
   # Add variables listed above
   ```

4. **Enable Branch Policies**
   - Repos → Branches → main → Branch Policies
   - Require build validation before PR merge

## Docker Image Usage

### Pull from GitHub Container Registry

```bash
# Authenticate
echo $GITHUB_TOKEN | docker login ghcr.io -u USERNAME --password-stdin

# Pull latest image
docker pull ghcr.io/YOUR_ORG/enterprise:latest

# Pull specific version
docker pull ghcr.io/YOUR_ORG/enterprise:v1.0.0
```

### Run Locally

```bash
docker run -d \
  -p 5000:8080 \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e ConnectionStrings__DefaultConnection="your-connection-string" \
  -e JwtSettings__SecretKey="your-secret-key" \
  ghcr.io/YOUR_ORG/enterprise:latest
```

## Database Migrations

### Automated Migrations (Recommended)

Configure automatic migrations on startup by ensuring `appsettings.json` contains:

```json
{
  "DatabaseSettings": {
    "AutoMigrate": true
  }
}
```

### Manual Migrations

```bash
# From Azure Cloud Shell or CI/CD pipeline
az webapp config connection-string set \
  --resource-group YOUR_RG \
  --name YOUR_WEBAPP \
  --connection-string-type SQLServer \
  --settings DefaultConnection="your-connection-string"

# Run migrations via Azure CLI or SSH
az webapp ssh --resource-group YOUR_RG --name YOUR_WEBAPP
dotnet ef database update --project /app/Enterprise.Infrastructure.dll
```

## Monitoring and Troubleshooting

### View Pipeline Status

**GitHub Actions:**

- Go to repository → Actions tab
- View workflow runs, logs, and artifacts

**Azure DevOps:**

- Pipelines → Recent runs
- Click on run → View logs

### Common Issues

1. **Build Fails - Dependency Resolution**

   ```bash
   # Clear NuGet cache
   dotnet nuget locals all --clear
   ```

2. **Test Failures**
   - Check test logs in pipeline artifacts
   - Verify database connection strings in test environment

3. **Docker Build Fails**

   ```bash
   # Test locally
   docker build -t enterprise-api:test .
   docker run --rm enterprise-api:test
   ```

4. **Deployment Health Check Fails**
   - Verify `/health` endpoint is accessible
   - Check Azure Web App logs:

     ```bash
     az webapp log tail --resource-group YOUR_RG --name YOUR_WEBAPP
     ```

5. **Security Scan Failures**
   - Update vulnerable packages:

     ```bash
     dotnet list package --vulnerable
     dotnet add package [PackageName] --version [NewVersion]
     ```

## Blue-Green Deployment Details

The production deployment uses Azure Web App deployment slots for zero-downtime deployments:

1. **Deploy to Staging Slot** - New version runs in isolation
2. **Health Check** - Verify application is healthy
3. **Swap Slots** - Instant traffic switch to new version
4. **Rollback** - Instant swap back if issues detected

### Manual Slot Management

```bash
# Swap slots manually
az webapp deployment slot swap \
  --resource-group YOUR_RG \
  --name YOUR_WEBAPP \
  --slot staging \
  --target-slot production

# Rollback (swap back)
az webapp deployment slot swap \
  --resource-group YOUR_RG \
  --name YOUR_WEBAPP \
  --slot production \
  --target-slot staging
```

## Performance Optimization

### Pipeline Speed Improvements

1. **Parallel Test Execution**
   - Already configured with separate jobs

2. **Docker Layer Caching**
   - Enabled with GitHub Actions cache
   - Azure DevOps uses cached layers

3. **Artifact Retention**
   - GitHub: 7 days (configurable)
   - Azure DevOps: 30 days default

### Build Time Optimization

```bash
# Skip tests in emergency deployments (not recommended)
# In GitHub Actions workflow_dispatch:
dotnet build --configuration Release --no-restore /p:RunTests=false
```

## Security Best Practices

1. **Secrets Management**
   - Never commit secrets to repository
   - Use GitHub Secrets or Azure Key Vault
   - Rotate credentials regularly

2. **Least Privilege**
   - Service principals should have minimal required permissions
   - Use managed identities where possible

3. **Vulnerability Scanning**
   - Trivy scans run on every build
   - Review security tab regularly
   - Enable Dependabot for automated dependency updates

4. **Container Security**
   - Use minimal base images
   - Scan images before deployment
   - Run containers as non-root user

## Notifications and Monitoring

### Slack Integration

Configure Slack notifications by:

1. Create Slack webhook URL
2. Add `SLACK_WEBHOOK` secret
3. Notifications sent on deployment success/failure

### Email Notifications

**GitHub Actions:**

- Automatic email on workflow failures
- Configure in GitHub notification settings

**Azure DevOps:**

- Project Settings → Notifications
- Create subscription for build/release events

## Next Steps

1. **Configure Secrets** - Add all required secrets/variables
2. **Test CI Pipeline** - Create a PR to trigger build
3. **Configure Environments** - Set up staging and production
4. **Enable SonarCloud** - Uncomment and configure code quality
5. **Add Monitoring** - Integrate Application Insights
6. **Document Runbooks** - Create deployment and rollback procedures

## Resources

- [GitHub Actions Documentation](https://docs.github.com/en/actions)
- [Azure DevOps Pipelines](https://docs.microsoft.com/azure/devops/pipelines/)
- [Azure Web Apps](https://docs.microsoft.com/azure/app-service/)
- [Docker Best Practices](https://docs.docker.com/develop/dev-best-practices/)
- [.NET Deployment Guide](https://docs.microsoft.com/dotnet/core/deploying/)

## Support

For pipeline issues:

1. Check pipeline logs for detailed error messages
2. Verify all secrets and variables are configured
3. Test Docker builds locally before pushing
4. Review Azure resource logs for deployment issues
