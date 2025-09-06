# Azure API Management Deployment Guide

This guide provides step-by-step instructions for deploying Azure API Management for the Integration Gateway project.

## 📋 Prerequisites

### Azure Requirements
- Azure subscription with contributor access
- Resource groups created for target environments
- Azure CLI installed and logged in
- PowerShell 7+ or Bash shell

### Project Requirements
- Integration Gateway backend service deployed and running
- Valid publisher email address for APIM service
- Understanding of target environment configuration

## 🚀 Deployment Steps

### Step 1: Prepare Environment

1. **Clone or navigate to the project directory:**
   ```bash
   cd /path/to/your/APIGateWay/azure-apim
   ```

2. **Log in to Azure CLI:**
   ```bash
   az login
   az account set --subscription "your-subscription-id"
   ```

3. **Verify subscription context:**
   ```bash
   az account show
   ```

### Step 2: Configure Parameters

1. **Edit environment-specific parameter files:**
   
   For Development (`bicep/parameters/dev.bicepparam`):
   ```bicep
   param apimName = 'apim-integration-gateway-dev'
   param publisherEmail = 'your-email@company.com'  // Update this
   param backendServiceUrl = 'https://localhost:7000'  // Update this
   ```

   For Staging (`bicep/parameters/staging.bicepparam`):
   ```bicep
   param apimName = 'apim-integration-gateway-staging'
   param publisherEmail = 'your-email@company.com'  // Update this
   param backendServiceUrl = 'https://your-staging-app.azurewebsites.net'  // Update this
   ```

   For Production (`bicep/parameters/prod.bicepparam`):
   ```bicep
   param apimName = 'apim-integration-gateway-prod'
   param publisherEmail = 'your-email@company.com'  // Update this
   param backendServiceUrl = 'https://your-prod-app.azurewebsites.net'  // Update this
   ```

2. **Update configuration file (`configs/apim-config.json`) if needed:**
   - Subscription IDs
   - Resource group names
   - Backend URLs
   - Rate limiting settings

### Step 3: Deploy Infrastructure

#### Option A: Using PowerShell Script (Recommended)

```powershell
# Development
./scripts/deploy.ps1 -Environment dev -ResourceGroupName "rg-integration-gateway-dev"

# Staging
./scripts/deploy.ps1 -Environment staging -ResourceGroupName "rg-integration-gateway-staging"

# Production
./scripts/deploy.ps1 -Environment prod -ResourceGroupName "rg-integration-gateway-prod"
```

#### Option B: Using Bash Script

```bash
# Development
./scripts/deploy.sh -e dev -g "rg-integration-gateway-dev"

# Staging
./scripts/deploy.sh -e staging -g "rg-integration-gateway-staging"

# Production  
./scripts/deploy.sh -e prod -g "rg-integration-gateway-prod"
```

#### Option C: Manual Azure CLI Deployment

```bash
# Validate first
az deployment group validate \
  --resource-group "rg-integration-gateway-dev" \
  --template-file "bicep/main.bicep" \
  --parameters "bicep/parameters/dev.bicepparam"

# Deploy
az deployment group create \
  --resource-group "rg-integration-gateway-dev" \
  --template-file "bicep/main.bicep" \
  --parameters "bicep/parameters/dev.bicepparam" \
  --name "apim-deployment-$(date +%Y%m%d-%H%M%S)"
```

### Step 4: Post-Deployment Configuration

1. **Wait for APIM provisioning:**
   - APIM provisioning takes 30-45 minutes
   - Monitor progress in Azure portal
   - Check deployment status:
   ```bash
   az apim show --name "apim-integration-gateway-dev" --resource-group "rg-integration-gateway-dev" --query "provisioningState"
   ```

2. **Verify deployment outputs:**
   The deployment will provide these URLs:
   - **APIM Gateway URL**: `https://apim-integration-gateway-dev.azure-api.net/gateway`
   - **APIM Management URL**: For administration
   - **Application Insights**: For monitoring

### Step 5: Configure API Subscriptions

1. **Create a subscription key for testing:**
   ```bash
   # Get the product ID
   PRODUCT_ID=$(az apim product show \
     --resource-group "rg-integration-gateway-dev" \
     --service-name "apim-integration-gateway-dev" \
     --product-id "integration-gateway-product" \
     --query "id" -o tsv)

   # Create subscription
   az apim subscription create \
     --resource-group "rg-integration-gateway-dev" \
     --service-name "apim-integration-gateway-dev" \
     --subscription-id "test-subscription" \
     --display-name "Test Subscription" \
     --product-id "integration-gateway-product" \
     --state "active"

   # Get subscription key
   az apim subscription show \
     --resource-group "rg-integration-gateway-dev" \
     --service-name "apim-integration-gateway-dev" \
     --subscription-id "test-subscription" \
     --query "primaryKey" -o tsv
   ```

### Step 6: Test the Deployment

1. **Test using PowerShell script:**
   ```powershell
   $subscriptionKey = "your-subscription-key-from-step-5"
   $gatewayUrl = "https://apim-integration-gateway-dev.azure-api.net"
   
   ./scripts/test-apis.ps1 -ApimGatewayUrl $gatewayUrl -SubscriptionKey $subscriptionKey -ApiVersion 1
   ```

2. **Test manually with curl:**
   ```bash
   # Get products list
   curl -H "Ocp-Apim-Subscription-Key: your-subscription-key" \
        "https://apim-integration-gateway-dev.azure-api.net/gateway/api/v1/products"

   # Get specific product
   curl -H "Ocp-Apim-Subscription-Key: your-subscription-key" \
        "https://apim-integration-gateway-dev.azure-api.net/gateway/api/v1/products/product-id"
   ```

3. **Verify caching is working:**
   - Make the same request twice
   - Check response headers for cache status
   - Look for `X-Cache-Status: HIT` on second request

## 🔧 Configuration Details

### Environment-Specific Settings

| Setting | Dev | Staging | Production |
|---------|-----|---------|------------|
| SKU | Developer | Standard | Premium |
| Capacity | 1 | 1 | 2 |
| Rate Limit | 1000/hr | 1000/hr | 1000/hr |
| Daily Quota | 10,000 | 10,000 | 10,000 |
| Cache Duration | 5-10 min | 5-10 min | 5-10 min |

### Policy Configuration

The deployment includes these pre-configured policies:

1. **Global Policies:**
   - CORS headers
   - Security headers (HSTS, XSS protection)
   - Request ID generation
   - Error standardization

2. **Product Policies:**
   - JWT token validation
   - Subscription key validation
   - Rate limiting (1000 calls/hour per subscription)
   - IP-based rate limiting (500 calls/hour per IP)
   - Request size limiting (1MB max)
   - Circuit breaker (3 retries, 1-minute recovery)

3. **Operation Policies:**
   - Input validation for pagination parameters
   - Response caching with appropriate TTLs
   - ETag generation for conditional requests
   - Specific error handling (404, 400, etc.)

## 📊 Monitoring Setup

### Application Insights

The deployment automatically creates and configures:
- Application Insights workspace
- Log Analytics workspace
- APIM diagnostic settings
- Custom alerts for response time and error rates

### Key Metrics to Monitor

1. **Performance Metrics:**
   - Average response time
   - 95th percentile response time
   - Request rate

2. **Error Metrics:**
   - HTTP error rates (4xx, 5xx)
   - Backend connection failures
   - Authentication failures

3. **Business Metrics:**
   - API usage by subscription
   - Most popular endpoints
   - Geographic distribution of requests

## 🔒 Security Configuration

### Authentication Setup

1. **JWT Validation:**
   - Configured for Azure AD tokens
   - Validates audience claim: `api://integration-gateway`
   - Can be customized in policies

2. **Subscription Keys:**
   - Required for all API calls
   - Can be passed in header (`Ocp-Apim-Subscription-Key`) or query string
   - Automatically generated per subscription

### Additional Security Measures

1. **HTTPS Enforcement:**
   - All traffic redirected to HTTPS
   - TLS 1.2+ required
   - Security headers added

2. **Rate Limiting:**
   - Multiple layers of protection
   - Per-subscription and per-IP limits
   - Configurable thresholds

3. **Request Validation:**
   - Size limits enforced
   - Parameter validation
   - Content type validation

## 🚨 Troubleshooting

### Common Issues

1. **Deployment Fails with "Publisher Email Required":**
   - Ensure `publisherEmail` is set in parameter files
   - Email must be a valid format

2. **Backend Service Unreachable:**
   - Verify `backendServiceUrl` is correct and accessible
   - Check firewall rules if using private endpoints
   - Test direct access to backend service

3. **Policies Not Applied:**
   - Check policy XML syntax
   - Verify policy files are correctly referenced in Bicep
   - Review APIM portal for policy errors

4. **Rate Limiting Too Restrictive:**
   - Adjust limits in `policies/products/integration-gateway-policy.xml`
   - Redeploy or update policies through Azure portal

5. **Caching Not Working:**
   - Verify cache policies are applied
   - Check for `Vary-By` headers that might prevent caching
   - Review cache keys in policy configuration

### Diagnostic Commands

```bash
# Check APIM status
az apim show --name "apim-name" --resource-group "rg-name" --query "provisioningState"

# List API operations
az apim api operation list --resource-group "rg-name" --service-name "apim-name" --api-id "integration-gateway-api"

# Check backend service
az apim backend show --resource-group "rg-name" --service-name "apim-name" --backend-id "integration-gateway-backend"

# View recent deployments
az deployment group list --resource-group "rg-name" --query "[?contains(name, 'apim')].{Name:name, State:properties.provisioningState, Timestamp:properties.timestamp}"
```

### Getting Help

1. **Azure Portal:**
   - Navigate to APIM service
   - Check "Activity log" for deployment issues
   - Review "Monitoring" section for runtime issues

2. **Application Insights:**
   - Search for specific error messages
   - Review dependency call failures
   - Check performance metrics

3. **APIM Developer Portal:**
   - Test APIs interactively
   - Review API documentation
   - Check subscription status

## 🔄 Updates and Maintenance

### Updating Policies

1. **Option 1: Redeploy with Bicep:**
   ```bash
   az deployment group create \
     --resource-group "rg-name" \
     --template-file "bicep/main.bicep" \
     --parameters "bicep/parameters/env.bicepparam"
   ```

2. **Option 2: Update via Azure CLI:**
   ```bash
   az apim api policy create \
     --resource-group "rg-name" \
     --service-name "apim-name" \
     --api-id "api-id" \
     --policy-content @path/to/policy.xml
   ```

### Scaling Considerations

- **Developer SKU**: Single unit, no scaling options
- **Standard SKU**: Can scale to multiple units
- **Premium SKU**: Supports multi-region deployment and VNet integration

### Backup and Recovery

- APIM configuration is stored in Azure Resource Manager
- Policies are version controlled in this repository  
- Application Insights data has configurable retention
- Consider exporting APIM configuration for disaster recovery

## 📞 Support

For issues or questions:
1. Check this documentation
2. Review Azure APIM documentation
3. Open GitHub issue in the project repository
4. Contact the Integration Gateway team