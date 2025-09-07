# Integration Gateway

## Documentation Links

- [Design Answers](answers/DESIGN.md)
- [Quick Start Guide](docs/Quick-Start-Guide.md)
- [Testing Guide](docs/Testing-Guide.md)
- [API Multi-Versioning](docs/API-Multi-Versioning.md)
- [Observability Debugging Guide](docs/Observability-Debugging-Guide.md)
- [Cross-Cutting Concerns Strategy](docs/Cross-Cutting-Concerns-Strategy.md)
- [Framework & Technology Rationale](docs/Framework-Technology-Rationale.md)
- [**Azure API Management Integration**](src/azure-apim/docs/README.md) 🆕
- [**GitHub CI/CD Pipeline**](.github/README.md) 🆕

## Project Overview

**What**: Integration Gateway API for orchestrating ERP and Warehouse systems

**Why**: Production-ready enterprise integration platform showcasing coding excellence and Azure-native patterns

**Key Features**: API versioning, resilience patterns, idempotency, clean architecture, enterprise coding standards

## Quick Start

**Prerequisites**: .NET 8.0 SDK

**Clone and Run**: 
```bash
git clone https://github.com/guangliangyang/IntegrationGateway.git
cd IntegrationGateway
dotnet run --project src/IntegrationGateway.Api
```

**Detailed Guide**: See [Quick-Start-Guide.md](docs/Quick-Start-Guide.md)

## Architecture Options

### Option 1: Direct Access (Development/Internal)
```
Client → Integration Gateway (.NET) → ERP/Warehouse Systems
```
- Fast development cycles
- Full feature access
- No additional infrastructure

### Option 2: Enterprise Gateway (Production/External) 🆕
```
Client → Azure API Management → Integration Gateway (.NET) → ERP/Warehouse Systems
```
- Enterprise API management
- Advanced security & monitoring
- Rate limiting & caching
- External partner integration

See [Azure API Management Integration Guide](src/azure-apim/docs/README.md) for details.

## Architecture Highlights

- **API Versioning**: V1/V2 with inheritance pattern for zero-breaking-change evolution
- **Resilience Patterns**: 2 retries, 15s timeout, 5-failure circuit breaker with Polly
- **Idempotency**: 15-minute TTL with 3s fast-fail semantics for exactly-once operations
- **Clean Architecture**: CQRS/MediatR with pipeline behaviors for cross-cutting concerns
- **Caching Strategy**: 5-second TTL in-memory caching with type-safe configuration
- **Security**: Azure Key Vault integration with comprehensive SSRF protection

## Project Structure

```
IntegrationGateway/
├── .github/                 # GitHub CI/CD pipeline configuration 🆕
│   ├── workflows/          # GitHub Actions workflows
│   ├── scripts/            # Deployment automation scripts
│   └── ISSUE_TEMPLATE/     # Issue and PR templates
├── src/                     # Source code
│   ├── IntegrationGateway.Api/      # Web API layer
│   ├── IntegrationGateway.Application/  # CQRS handlers & behaviors  
│   ├── IntegrationGateway.Models/       # DTOs and domain models
│   ├── IntegrationGateway.Services/     # Business services
│   └── azure-apim/         # Azure API Management integration 🆕
│       ├── bicep/          # Infrastructure as Code
│       ├── policies/       # API Management policies
│       ├── scripts/        # Deployment scripts
│       └── docs/           # APIM documentation
├── stubs/                   # Mock services for development
├── tests/                   # Unit & integration tests
└── docs/                    # Technical documentation
```

## Technology Stack

### Core Framework

- **.NET 8.0** + **ASP.NET Core** - High-concurrency async/await foundation
- **MediatR** - CQRS pattern with pipeline behaviors for cross-cutting concerns
- **Polly** - Production-ready resilience patterns (retry, circuit breaker, timeout)
- **Azure Key Vault** - Enterprise-grade secret management with DefaultAzureCredential
- **Application Insights** - Comprehensive observability with dependency tracking
- **xUnit + Moq** - Complete testing ecosystem with integration test support

### Azure Integration 🆕

- **Azure API Management** - Enterprise API gateway with policies and monitoring
- **Bicep** - Infrastructure as Code for repeatable deployments
- **Application Insights** - Centralized logging and monitoring
- **Azure Monitor** - Performance and availability alerting

## API Endpoints Preview

- **V1**: `/api/v1/products` - Core product operations [swagger-v1.json](docs/swagger-v1.json) 
- **V2**: `/api/v2/products` - Enhanced with additional fields + batch operations [swagger-v2.json](docs/swagger-v2.json) 
- **Swagger UI**: `https://localhost:7000/swagger`
- **API Changelog**: [Version differences and migration guide](docs/API-Changelog.md)

### With Azure API Management 🆕
- **Gateway V1**: `https://your-apim.azure-api.net/gateway/api/v1/products`
- **Gateway V2**: `https://your-apim.azure-api.net/gateway/api/v2/products`
- **APIM Portal**: Enterprise developer portal with API documentation

## Production Considerations

### Current Implementation

- **Configuration**: Azure Key Vault integration for secrets
- **Security**: JWT authentication, SSRF protection, input validation  
- **Monitoring**: Application Insights telemetry and health checks
- **Performance**: High-concurrency support with async patterns

### Azure Production Options 🆕

Choose your deployment strategy based on requirements:

#### Option A: Integration Gateway Only
- **Pros**: Simple, fast, cost-effective
- **Use Cases**: Internal APIs, development, microservice communication
- **Deploy**: Container Apps, App Service, or Kubernetes

#### Option B: APIM + Integration Gateway (Recommended for External APIs)
- **Pros**: Enterprise features, external partner support, advanced monitoring
- **Use Cases**: Public APIs, partner integrations, complex rate limiting
- **Deploy**: See [APIM Deployment Guide](src/azure-apim/docs/DEPLOYMENT-GUIDE.md)

#### Additional Azure Services
- **Redis Cache**: Distributed caching for multi-instance scenarios 
- **Container Apps**: Microservices deployment with auto-scaling
- **Service Bus**: Async messaging and event-driven architecture

## Getting Started with APIM 🆕

1. **Deploy Integration Gateway**: Follow existing [Quick Start Guide](docs/Quick-Start-Guide.md)
2. **Deploy APIM Layer**: Follow [APIM Deployment Guide](src/azure-apim/docs/DEPLOYMENT-GUIDE.md)
3. **Configure Policies**: Customize authentication, rate limiting, and caching
4. **Test Integration**: Use provided test scripts and monitor through Azure portal

## Acknowledgments

- **Clean Architecture Pattern**: Inspired by [Jason Taylor's Clean Architecture Template](https://github.com/jasontaylordev/CleanArchitecture) - excellent foundation for .NET enterprise applications
- **Development Assistance**: Built with [Claude AI](https://claude.ai) assistance for code generation, architecture decisions, and best practices implementation
- **Open Source Libraries**: Grateful to the maintainers of MediatR, Polly, FluentValidation, and the entire .NET ecosystem

## License

This project is licensed under the MIT License - see the LICENSE file for details.

---

*This project demonstrates enterprise-grade integration patterns suitable for production Azure deployments with flexible API management options.*