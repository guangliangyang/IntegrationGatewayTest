# Azure API Management Integration

This folder contains the complete Azure API Management (APIM) integration solution for the Integration Gateway project. It provides enterprise-grade API gateway functionality including authentication, rate limiting, caching, and monitoring.

## 🏗️ Architecture Overview

```
Client → Azure APIM → Integration Gateway (.NET) → ERP/Warehouse Systems
```

**APIM Responsibilities:**
- API Gateway (routing, load balancing)
- Authentication & Authorization (JWT, API keys)
- Rate Limiting & Throttling
- Response Caching
- Monitoring & Analytics
- Security Headers & CORS

**Integration Gateway Responsibilities:**
- Complex Business Logic
- Data Orchestration & Transformation
- Advanced Idempotency Handling
- CQRS Pattern Implementation
- Custom Exception Handling

## 📁 Folder Structure

```
azure-apim/
├── bicep/                          # Infrastructure as Code
│   ├── main.bicep                  # Main deployment template
│   ├── apim.bicep                  # APIM service definition
│   ├── monitoring.bicep            # Monitoring resources
│   └── parameters/                 # Environment-specific parameters
│       ├── dev.bicepparam         # Development environment
│       ├── staging.bicepparam     # Staging environment
│       └── prod.bicepparam        # Production environment
├── policies/                       # APIM Policy Definitions
│   ├── global/                    # Global policies
│   │   └── global-policy.xml      # CORS, security headers, error handling
│   ├── products/                  # Product-level policies
│   │   └── integration-gateway-policy.xml  # Rate limiting, auth, caching
│   └── operations/                # Operation-specific policies
│       ├── get-products-policy.xml    # List products with caching
│       └── get-product-policy.xml     # Single product with ETag support
├── scripts/                       # Deployment & Management Scripts
│   ├── deploy.ps1                 # PowerShell deployment script
│   └── deploy.sh                  # Bash deployment script
├── configs/                       # Configuration Files
│   └── apim-config.json          # APIM configuration settings
└── docs/                         # Documentation
    └── README.md                 # This file
```

## 🚀 Quick Start

### Prerequisites

- Azure CLI installed and logged in
- PowerShell 7+ (for Windows) or Bash (for Linux/macOS)
- Appropriate Azure permissions to create APIM resources

### 1. Deploy APIM Infrastructure

**Using PowerShell:**
```powershell
./scripts/deploy.ps1 -Environment dev -ResourceGroupName "rg-integration-gateway-dev"
```

**Using Bash:**
```bash
./scripts/deploy.sh -e dev -g "rg-integration-gateway-dev"
```

### 2. Configure Backend Service

Update the backend URL in your parameter files:
```bicep
param backendServiceUrl = 'https://your-integration-gateway.azurewebsites.net'
```

### 3. Test the API Gateway

Once deployed, your APIs will be available at:
```
https://apim-integration-gateway-dev.azure-api.net/gateway/api/v1/products
https://apim-integration-gateway-dev.azure-api.net/gateway/api/v2/products
```

## 🔧 Configuration

### Environment Settings

Each environment has its own parameter file in `bicep/parameters/`:

- **dev.bicepparam**: Development environment with Developer SKU
- **staging.bicepparam**: Staging environment with Standard SKU  
- **prod.bicepparam**: Production environment with Premium SKU

### Policy Configuration

Policies are organized by scope:

1. **Global Policies** (`policies/global/`):
   - CORS configuration
   - Security headers
   - Request/response transformation
   - Error handling

2. **Product Policies** (`policies/products/`):
   - Authentication (JWT validation)
   - Rate limiting and quotas
   - Subscription key validation
   - Circuit breaker patterns

3. **Operation Policies** (`policies/operations/`):
   - Input validation
   - Response caching with appropriate TTLs
   - ETag support for conditional requests
   - Error-specific handling

## 📊 Monitoring & Analytics

### Application Insights Integration

- **Request Tracking**: All API calls are tracked with performance metrics
- **Dependency Tracking**: Backend service calls are monitored
- **Error Tracking**: Failed requests are logged with detailed context
- **Custom Metrics**: Business-specific metrics can be added

### Built-in Monitoring

- **Response Time Alerts**: Triggers when API response time exceeds 5 seconds
- **Error Rate Alerts**: Triggers when error rate exceeds 10 failures per 5 minutes
- **Log Analytics**: 30-day retention for detailed log analysis

## 🔒 Security Features

### Authentication
- **JWT Validation**: Validates Azure AD tokens with proper audience claims
- **Subscription Keys**: Required for all API access
- **IP-based Rate Limiting**: Additional protection against abuse

### Security Headers
- `X-Content-Type-Options: nosniff`
- `X-Frame-Options: DENY`
- `X-XSS-Protection: 1; mode=block`
- `Strict-Transport-Security: max-age=31536000; includeSubDomains`

### Request Protection
- **Maximum Request Size**: 1MB limit to prevent large payload attacks
- **CORS Configuration**: Controlled cross-origin access
- **Circuit Breaker**: Automatic failover protection

## ⚡ Performance Features

### Caching Strategy
- **Product Lists**: 5-minute cache duration
- **Individual Products**: 10-minute cache with ETag support
- **Error Responses**: Short-term caching to prevent cascading failures

### Rate Limiting
- **Per Subscription**: 1,000 calls/hour, 10,000 calls/day
- **Per IP Address**: 500 calls/hour for additional protection
- **Burst Protection**: Handles traffic spikes gracefully

## 🎯 API Endpoints

All endpoints support both V1 and V2 versions:

### Product Management
- `GET /gateway/api/v{1,2}/products` - List products with pagination
- `GET /gateway/api/v{1,2}/products/{id}` - Get product by ID
- `POST /gateway/api/v{1,2}/products` - Create new product
- `PUT /gateway/api/v{1,2}/products/{id}` - Update existing product
- `DELETE /gateway/api/v{1,2}/products/{id}` - Delete product

### Headers
- `Ocp-Apim-Subscription-Key`: Required subscription key
- `Authorization`: JWT token (Bearer scheme)
- `X-Request-ID`: Optional request tracking ID

## 🔄 Integration Options

### Option 1: Full APIM Integration (Recommended)
```
Client → Azure APIM → Integration Gateway → Backend Systems
```
- Complete enterprise API management
- Full monitoring and analytics
- Advanced security and caching

### Option 2: Selective Integration
```
Client → Integration Gateway (direct access for internal calls)
Client → Azure APIM → Integration Gateway (for external/partner APIs)
```
- Flexible deployment model
- Cost optimization for internal traffic
- External API management through APIM

### Option 3: Development/Testing
```
Client → Integration Gateway (bypass APIM for development)
```
- Fast development cycles
- No additional infrastructure costs
- Direct access to all features

## 🛠️ Deployment Commands

### Manual Deployment
```bash
# Development
az deployment group create \
  --resource-group rg-integration-gateway-dev \
  --template-file bicep/main.bicep \
  --parameters bicep/parameters/dev.bicepparam

# Staging  
az deployment group create \
  --resource-group rg-integration-gateway-staging \
  --template-file bicep/main.bicep \
  --parameters bicep/parameters/staging.bicepparam

# Production
az deployment group create \
  --resource-group rg-integration-gateway-prod \
  --template-file bicep/main.bicep \
  --parameters bicep/parameters/prod.bicepparam
```

### Validation
```bash
# Validate template before deployment
az deployment group validate \
  --resource-group rg-integration-gateway-dev \
  --template-file bicep/main.bicep \
  --parameters bicep/parameters/dev.bicepparam
```

## 📋 Prerequisites Checklist

- [ ] Azure subscription with appropriate permissions
- [ ] Resource groups created for each environment
- [ ] Integration Gateway backend service deployed
- [ ] Azure CLI installed and configured
- [ ] Publisher email configured for APIM service
- [ ] DNS/custom domain configured (for production)

## 🎉 Benefits of This Approach

### For Development Teams
- **Flexibility**: Can choose when to enable APIM layer
- **Development Speed**: Direct access during development
- **Testing**: Easy to test with/without APIM layer

### For Operations Teams
- **Enterprise Features**: Complete API management solution
- **Monitoring**: Comprehensive observability
- **Security**: Multi-layered security approach
- **Scalability**: Enterprise-grade scaling capabilities

### For Business Stakeholders
- **Cost Control**: Pay for APIM only when needed
- **Risk Mitigation**: Gradual adoption approach
- **Compliance**: Enterprise security and compliance features
- **Analytics**: Business insights from API usage data

## 🔗 Additional Resources

- [Azure API Management Documentation](https://docs.microsoft.com/en-us/azure/api-management/)
- [APIM Policy Reference](https://docs.microsoft.com/en-us/azure/api-management/api-management-policies)
- [Bicep Documentation](https://docs.microsoft.com/en-us/azure/azure-resource-manager/bicep/)
- [Integration Gateway Documentation](../docs/)