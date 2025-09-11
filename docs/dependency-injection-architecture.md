# Dependency Injection Architecture

## Overview

This document provides a comprehensive guide to the dependency injection implementation in the Integration Gateway project. The project leverages ASP.NET Core's built-in dependency injection container to manage service lifecycles, configurations, and cross-cutting concerns through a well-structured and modular approach.

## Architecture Principles

### High Cohesion, Low Coupling
- Each service registration is organized by domain/concern
- Extensions methods group related configurations
- Conditional registration based on configuration settings

### Service Lifecycle Management
The project uses three primary service lifetimes:
- **Singleton**: Single instance for application lifetime
- **Scoped**: One instance per HTTP request
- **Transient**: New instance every time requested

## Core Service Registration

### Program.cs Registration Flow

The main service registration occurs in `Program.cs` with the following sequence:

```csharp
// 1. Configuration Services
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
builder.Services.Configure<ErpServiceOptions>(builder.Configuration.GetSection("ErpService"));
builder.Services.Configure<WarehouseServiceOptions>(builder.Configuration.GetSection("WarehouseService"));

// 2. Infrastructure Services
builder.Services.AddHttpClients(builder.Configuration);

// 3. Business Services
builder.Services.AddScoped<IErpService, ErpService>();
builder.Services.AddScoped<IWarehouseService, WarehouseService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddSingleton<IIdempotencyService, IdempotencyService>();

// 4. Application Layer Services
builder.Services.AddApplicationServices(builder.Configuration);

// 5. Cross-cutting Concerns
builder.Services.AddConfiguredSsrfProtection(builder.Configuration);
builder.Services.AddJwtAuthentication(builder.Configuration);
```

## Service Lifecycle Details

### Scoped Services
**Purpose**: Services that maintain state within a single HTTP request but should be fresh for each request.

```csharp
builder.Services.AddScoped<IErpService, ErpService>();
builder.Services.AddScoped<IWarehouseService, WarehouseService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
```

**Rationale**:
- **ErpService/WarehouseService**: Use HTTP clients that may maintain connection state during a request
- **ProductService**: Orchestrates multiple external services and maintains request context
- **CurrentUserService**: Accesses HttpContext which is request-scoped

### Singleton Services
**Purpose**: Services that are expensive to create, stateless, or maintain global state.

```csharp
builder.Services.AddSingleton<IIdempotencyService, IdempotencyService>();
builder.Services.AddSingleton<IUrlValidationService, UrlValidationService>();
```

**Rationale**:
- **IdempotencyService**: Uses concurrent collections and semaphores for thread-safe operations
- **UrlValidationService**: Contains only validation logic, no mutable state

### Transient Services
**Purpose**: Lightweight services that should be created fresh each time.

```csharp
services.AddTransient<SsrfProtectionHandler>();
services.AddTransient<NoOpSsrfProtectionHandler>();
```

**Rationale**:
- **HTTP Message Handlers**: Should be lightweight and created per HTTP client usage

## HTTP Client Configuration

### Named HTTP Clients
The project uses named HTTP clients for different external services:

```csharp
services.AddHttpClient("ErpClient", client => /* configuration */);
services.AddHttpClient("WarehouseClient", client => /* configuration */);
```

### Client Registration Pattern
```csharp
public static IServiceCollection AddHttpClients(this IServiceCollection services, IConfiguration configuration)
{
    var erpClientBuilder = services.AddHttpClient("ErpClient", /* config */);
    
    // Conditional SSRF protection
    if (ssrfEnabled)
    {
        erpClientBuilder.AddHttpMessageHandler<SsrfProtectionHandler>();
    }
    else
    {
        erpClientBuilder.AddHttpMessageHandler<NoOpSsrfProtectionHandler>();
    }
    
    // Resilience policies
    erpClientBuilder
        .AddPolicyHandler(GetRetryPolicy())
        .AddPolicyHandler(GetCircuitBreakerPolicy())
        .AddPolicyHandler(GetTimeoutPolicy());
}
```

## MediatR Pipeline Registration

### Application Layer Services
Located in `IntegrationGateway.Application/DependencyInjection.cs`:

```csharp
public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
{
    // Conditional services based on configuration
    var cacheEnabled = configuration.GetValue<bool>("Cache:Enabled", false);
    
    if (cacheEnabled)
    {
        services.AddMemoryCache();
    }

    // MediatR with ordered pipeline behaviors
    services.AddMediatR(cfg =>
    {
        cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
        
        // Pipeline order matters!
        cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(UnhandledExceptionBehaviour<,>));
        cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>));
        cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(PerformanceBehaviour<,>));
        cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehaviour<,>));
        
        if (cacheEnabled)
        {
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(CachingBehaviour<,>));
        }
    });
}
```

### Pipeline Behavior Lifecycle
All MediatR pipeline behaviors are registered as **Scoped** by default:
- **UnhandledExceptionBehaviour**: Scoped (catches exceptions per request)
- **ValidationBehaviour**: Scoped (validates input per request)
- **PerformanceBehaviour**: Scoped (measures performance per request)
- **LoggingBehaviour**: Scoped (logs request context)
- **CachingBehaviour**: Scoped (but uses Singleton IMemoryCache)

## Configuration-Driven Registration

### Conditional Service Registration
Services are registered conditionally based on configuration:

```csharp
// SSRF Protection
var ssrfOptions = configuration.GetSection("Security:SsrfProtection").Get<SsrfProtectionOptions>();
if (ssrfOptions?.Enabled == true)
{
    services.AddTransient<SsrfProtectionHandler>();
}
else
{
    services.AddTransient<NoOpSsrfProtectionHandler>();
}

// Caching
var cacheEnabled = configuration.GetValue<bool>("Cache:Enabled", false);
if (cacheEnabled)
{
    services.AddMemoryCache();
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(CachingBehaviour<,>));
}
```

### Configuration Options Pattern
All configuration is bound using the Options pattern:

```csharp
builder.Services.Configure<SecurityOptions>(builder.Configuration.GetSection("Security"));
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
builder.Services.Configure<ErpServiceOptions>(builder.Configuration.GetSection("ErpService"));
```

## Security Service Registration

### SSRF Protection
```csharp
public static class SsrfProtectionExtensions
{
    public static IServiceCollection AddConfiguredSsrfProtection(this IServiceCollection services, IConfiguration configuration)
    {
        // Always register validation service
        services.AddSingleton<IUrlValidationService, UrlValidationService>();
        
        // Conditionally register handlers
        if (ssrfOptions?.Enabled == true)
        {
            services.AddTransient<SsrfProtectionHandler>();
        }
        else
        {
            services.AddTransient<NoOpSsrfProtectionHandler>();
        }
    }
}
```

## Extension Method Pattern

### Modular Registration
Each concern is encapsulated in extension methods:

- `AddHttpClients()` - HTTP client configuration
- `AddApplicationServices()` - Application layer services
- `AddConfiguredSsrfProtection()` - SSRF protection services
- `AddJwtAuthentication()` - JWT authentication
- `AddConfiguredRateLimiting()` - Rate limiting
- `AddConfiguredCors()` - CORS configuration

### Benefits
1. **Separation of Concerns**: Each extension handles one aspect
2. **Testability**: Extensions can be tested independently
3. **Maintainability**: Easy to modify specific configurations
4. **Readability**: Clear intention in Program.cs

## Service Dependencies

### Dependency Graph
```
IProductService (Scoped)
├── IErpService (Scoped)
│   └── HttpClient "ErpClient" (Transient)
│       └── SsrfProtectionHandler (Transient)
├── IWarehouseService (Scoped)
│   └── HttpClient "WarehouseClient" (Transient)
│       └── SsrfProtectionHandler (Transient)
└── ILogger<ProductService> (Singleton)

IdempotencyService (Singleton)
├── IMemoryCache (Singleton)
└── ILogger<IdempotencyService> (Singleton)
```

### Circular Dependency Prevention
- Services depend on interfaces, not concrete implementations
- HTTP clients are injected via IHttpClientFactory
- Configuration is injected via IOptions<T> pattern

## Best Practices Implemented

### 1. Interface Segregation
```csharp
public interface IErpService
{
    Task<ErpResponse<Product>> GetProductAsync(string productId);
    Task<ErpResponse<BulkProductResponse>> GetProductsAsync(List<string> productIds);
}
```

### 2. Factory Pattern for HTTP Clients
```csharp
public ErpService(IHttpClientFactory httpClientFactory, ILogger<ErpService> logger)
{
    _httpClient = httpClientFactory.CreateClient("ErpClient");
    _logger = logger;
}
```

### 3. Configuration Validation
Configuration options are validated at startup through dedicated validation methods.

### 4. Conditional Registration
Services are only registered when needed based on configuration flags.

## Testing Implications

### Service Registration for Testing
The modular registration approach allows for:
1. **Unit Testing**: Mock individual services easily
2. **Integration Testing**: Override specific registrations
3. **Performance Testing**: Profile service creation costs
4. **Configuration Testing**: Test different configuration scenarios

### Test-Friendly Patterns
- All services depend on interfaces
- HTTP clients use IHttpClientFactory
- Configuration uses IOptions<T>
- Conditional registration can be bypassed in tests

## Performance Considerations

### Service Lifetime Impact
- **Singleton services** reduce allocation overhead
- **Scoped services** balance performance with request isolation
- **Transient services** used only for lightweight operations

### HTTP Client Management
- Named HTTP clients prevent socket exhaustion
- Message handlers are transient to avoid state issues
- Connection pooling managed by HttpClientFactory

## Configuration Integration

### appsettings.json Structure
```json
{
  "ErpService": {
    "BaseUrl": "http://localhost:5051",
    "TimeoutSeconds": 15,
    "MaxRetries": 2
  },
  "Cache": {
    "Enabled": true,
    "DefaultExpirationSeconds": 5
  },
  "Security": {
    "SsrfProtection": {
      "Enabled": true,
      "AllowedDomains": ["erp.example.com"]
    }
  }
}
```

### Environment-Specific Configuration
- Development: Lenient security, detailed logging
- Production: Strict security, optimized performance
- Testing: Mock services, fast execution

## Conclusion

The Integration Gateway's dependency injection architecture demonstrates:

1. **Clear separation of concerns** through modular extension methods
2. **Flexible configuration** enabling/disabling features via appsettings
3. **Proper service lifecycle management** matching usage patterns
4. **Testable design** with interface-based dependencies
5. **Performance optimization** through appropriate service lifetimes
6. **Security integration** with conditional protection mechanisms

This architecture provides a solid foundation for a maintainable, scalable, and secure API gateway while remaining flexible enough to adapt to changing requirements.