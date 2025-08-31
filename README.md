# Integration Gateway


## Documentation Links

- [Design Answers](answers/DESIGN.md)
- [Quick Start Guide](docs/Quick-Start-Guide.md)
- [Testing Guide](docs/Testing-Guide.md)
- [API Multi-Versioning](docs/API-Multi-Versioning.md)
- [Observability Debugging Guide](docs/Observability-Debugging-Guide.md)
- [Cross-Cutting Concerns Strategy](docs/Cross-Cutting-Concerns-Strategy.md)
- [Framework & Technology Rationale](docs/Framework-Technology-Rationale.md) 
 

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
├── src/                     # Source code
│   ├── IntegrationGateway.Api/      # Web API layer
│   ├── IntegrationGateway.Application/  # CQRS handlers & behaviors  
│   ├── IntegrationGateway.Models/       # DTOs and domain models
│   └── IntegrationGateway.Services/     # Business services
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
 

## API Endpoints Preview

- **V1**: `/api/v1/products` - Core product operations [swagger-v1.json](docs/swagger-v1.json) 
- **V2**: `/api/v2/products` - Enhanced with additional fields + batch operations  [swagger-v2.json](docs/swagger-v2.json) 
- **Swagger UI**: `https://localhost:7000/swagger`

## Production Considerations

### Current Implementation

- **Configuration**: Azure Key Vault integration for secrets
- **Security**: JWT authentication, SSRF protection, input validation  
- **Monitoring**: Application Insights telemetry and health checks
- **Performance**: High-concurrency support with async patterns

### Azure Production Upgrades

When scaling to enterprise production environments, consider these Azure services:

- **API Management**: Centralized API gateway with throttling, caching, analytics
- **Redis Cache**: Distributed caching for multi-instance scenarios 
- **Container Apps**: Microservices deployment with auto-scaling
 

## Acknowledgments

- **Clean Architecture Pattern**: Inspired by [Jason Taylor's Clean Architecture Template](https://github.com/jasontaylordev/CleanArchitecture) - excellent foundation for .NET enterprise applications
- **Development Assistance**: Built with [Claude AI](https://claude.ai) assistance for code generation, architecture decisions, and best practices implementation
- **Open Source Libraries**: Grateful to the maintainers of MediatR, Polly, FluentValidation, and the entire .NET ecosystem

## License

This project is licensed under the MIT License - see the LICENSE file for details.

---

*This project demonstrates enterprise-grade integration patterns suitable for production Azure deployments.*