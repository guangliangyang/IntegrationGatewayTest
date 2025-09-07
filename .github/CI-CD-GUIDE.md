# GitHub CI/CD Configuration

This directory contains the complete GitHub Actions CI/CD pipeline configuration for the Integration Gateway project.

## 📁 Structure

```
.github/
├── workflows/               # GitHub Actions workflows
│   ├── ci-cd.yml           # Main CI/CD pipeline
│   ├── release.yml         # Release workflow
│   ├── dependency-update.yml # Automated dependency updates
│   └── infrastructure.yml  # Infrastructure deployment
├── scripts/                # Deployment and utility scripts
│   └── deploy-to-azure.sh  # Azure deployment script
├── templates/              # Issue and PR templates
├── ISSUE_TEMPLATE/         # Issue templates
│   ├── bug_report.yml      # Bug report template
│   └── feature_request.yml # Feature request template
├── pull_request_template.md # PR template
└── README.md               # This file
```

## 🚀 Workflows

### 1. CI/CD Pipeline (`ci-cd.yml`)

**Triggers:**
- Push to `main` and `develop` branches
- Pull requests to `main` and `develop`
- Manual workflow dispatch

**Jobs:**
1. **Code Quality Analysis**
   - Code formatting validation
   - Static code analysis
   - Security scanning

2. **Testing** (Matrix strategy)
   - Unit tests
   - Integration tests
   - Performance tests

3. **Docker Build**
   - Multi-platform container builds (linux/amd64, linux/arm64)
   - Push to GitHub Container Registry

4. **Security Scan**
   - Trivy container vulnerability scanning
   - CodeQL static analysis

5. **Deploy** (Production only)
   - Deploy to Azure Container Apps
   - Environment-specific configuration

6. **Notifications**
   - Microsoft Teams notifications
   - Success/failure reporting

### 2. Release Workflow (`release.yml`)

**Triggers:**
- Git tags matching `v*` pattern (e.g., `v1.0.0`)

**Features:**
- Automated changelog generation
- Multi-platform binary builds
- Docker image publishing with version tags
- GitHub Release creation
- Production deployment for stable releases

### 3. Dependency Updates (`dependency-update.yml`)

**Schedule:** Every Monday at 2 AM UTC

**Features:**
- Automated dependency scanning
- Security vulnerability detection
- Automated PR creation for updates
- Build and test verification

### 4. Infrastructure Deployment (`infrastructure.yml`)

**Triggers:**
- Changes to Azure APIM configuration
- Manual deployment with environment selection

**Environments:**
- Development
- Staging
- Production

## 🔧 Setup Instructions

### 1. Required Secrets

Configure these secrets in your GitHub repository:

```bash
# Azure Authentication
AZURE_CREDENTIALS                 # Azure service principal credentials
AZURE_RESOURCE_GROUP             # Development resource group
AZURE_RESOURCE_GROUP_STAGING     # Staging resource group  
AZURE_RESOURCE_GROUP_PROD        # Production resource group

# Application Configuration
APPINSIGHTS_CONNECTION_STRING    # Application Insights connection string
JWT_SECRET_KEY                   # JWT signing key (minimum 32 characters)

# API Management
APIM_SUBSCRIPTION_KEY           # API Management subscription key

# Notifications
TEAMS_WEBHOOK_URL               # Microsoft Teams webhook URL
```

### 2. Azure Service Principal

Create an Azure service principal with appropriate permissions:

```bash
# Create service principal
az ad sp create-for-rbac \
  --name "integration-gateway-github-actions" \
  --role contributor \
  --scopes /subscriptions/{subscription-id}/resourceGroups/{resource-group-name} \
  --sdk-auth

# Output format for AZURE_CREDENTIALS secret:
{
  "clientId": "...",
  "clientSecret": "...",
  "subscriptionId": "...",
  "tenantId": "...",
  "activeDirectoryEndpointUrl": "...",
  "resourceManagerEndpointUrl": "...",
  "activeDirectoryGraphResourceId": "...",
  "sqlManagementEndpointUrl": "...",
  "galleryEndpointUrl": "...",
  "managementEndpointUrl": "..."
}
```

### 3. Environment Protection Rules

Configure environment protection rules in GitHub:

1. Go to **Settings** → **Environments**
2. Create environments: `development`, `staging`, `production`
3. Configure protection rules:
   - **Production**: Require reviews, restrict to main branch
   - **Staging**: Require reviews
   - **Development**: No restrictions

## 🐳 Container Registry

Images are published to GitHub Container Registry:

```bash
# Pull latest image
docker pull ghcr.io/{username}/integration-gateway:latest

# Pull specific version
docker pull ghcr.io/{username}/integration-gateway:v1.0.0
```

## 📊 Monitoring and Notifications

### Teams Notifications

The pipeline sends notifications to Microsoft Teams for:
- Successful deployments
- Failed deployments
- Security vulnerabilities
- Release completions

### Test Results

Test results and code coverage reports are automatically uploaded and available in:
- GitHub Actions artifacts
- Codecov integration
- Pull request comments

## 🔒 Security Features

### Vulnerability Scanning

- **Container Scanning**: Trivy scans container images
- **Code Scanning**: CodeQL analyzes source code
- **Dependency Scanning**: Automated security audits
- **SARIF Upload**: Security findings in GitHub Security tab

### Security Practices

- Non-root container execution
- Minimal base images
- Secret management via GitHub secrets
- Least privilege service principals
- Branch protection rules

## 📈 Performance Testing

Performance tests are automatically run as part of the CI pipeline:

- **Smoke Tests**: Basic functionality validation
- **Load Tests**: Performance under normal load
- **Stress Tests**: Performance under high load

Results are stored as artifacts and can be compared across builds.

## 🚀 Deployment Process

### Automatic Deployments

1. **Development**: Every push to `main`
2. **Staging**: After successful development deployment
3. **Production**: Manual approval required

### Manual Deployments

Use the deployment script for manual deployments:

```bash
.github/scripts/deploy-to-azure.sh ghcr.io/{username}/integration-gateway:v1.0.0
```

## 📝 Contributing

When creating PRs, please:

1. Fill out the PR template completely
2. Ensure all CI checks pass
3. Add appropriate labels
4. Request reviews from code owners

For issues, use the appropriate issue template:
- Bug reports: Use the bug report template
- Feature requests: Use the feature request template

## 🔄 Workflow Status Badges

Add these badges to your main README:

```markdown
[![CI/CD](https://github.com/{username}/{repo}/actions/workflows/ci-cd.yml/badge.svg)](https://github.com/{username}/{repo}/actions/workflows/ci-cd.yml)
[![Security](https://github.com/{username}/{repo}/actions/workflows/security.yml/badge.svg)](https://github.com/{username}/{repo}/actions/workflows/security.yml)
[![Release](https://github.com/{username}/{repo}/actions/workflows/release.yml/badge.svg)](https://github.com/{username}/{repo}/actions/workflows/release.yml)
```

## 📚 Additional Resources

- [GitHub Actions Documentation](https://docs.github.com/en/actions)
- [Azure Container Apps Documentation](https://docs.microsoft.com/en-us/azure/container-apps/)
- [Docker Best Practices](https://docs.docker.com/develop/dev-best-practices/)
- [Security Best Practices](https://docs.github.com/en/actions/security-guides)

---

For questions or issues with the CI/CD pipeline, please create an issue using the appropriate template.