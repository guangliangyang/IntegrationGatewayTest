# Integration Gateway Design Answers

## 1. Your approach to backward-compatible contract evolution for APIs and/or events.

My approach uses **inheritance-based controller versioning** combined with **extended DTO patterns** to ensure zero breaking changes. V2 controllers inherit from V1, automatically preserving all V1 functionality while adding enhancements. Response DTOs follow extension inheritance (ProductV2Dto extends ProductDto), ensuring all V1 fields remain present with identical structure. I implement **URL path versioning** (`/api/v1/` vs `/api/v2/`) with **ASP.NET Core API versioning**, supporting multiple access methods (URL, query params, headers). **Separate MediatR handlers** (GetProductsV1Query vs GetProductsV2Query) allow version-specific optimizations while maintaining clear separation. This pattern scales infinitely (V1,V2,V3,V4) with each version building upon the previous, never removing or changing existing contracts.

## 2. Retries, timeouts, and circuit breakers: your defaults and how you set budgets.

**Polly resilience patterns** with carefully tuned defaults based on enterprise integration requirements. 

**Retry policy**: 2 attempts with exponential backoff plus jitter (1s,2s,4s) to avoid thundering herd. 

**Timeouts**: 15-second request timeout, 10-second connection timeout to balance responsiveness with network variance. 

**Circuit breaker**: Opens after 5 consecutive failures with 2-minute break duration, 
requiring 10 minimum requests in 30-second sampling window before evaluation. 

**Budget setting**: Total request budget of ~45 seconds (15s base + 2 retries with backoff), allowing for network issues while preventing cascading failures. Circuit breaker prevents request amplification during downstream outages, providing **graceful degradation** with cached responses or default values when services are unavailable. 

**Exception**
In case of back-end exceptions or timeouts, caching and idempotency mechanisms are bypassed in favor of circuit breaker functionality.

## 3. Idempotency strategies for writes and replay handling

  Write Strategy: The system uses request fingerprinting with idempotencyKey|operation|bodyHash
   to uniquely identify operations. First-time requests execute business logic and cache the
  response using GetOrCreateOperationAsync and UpdateOperationResponseAsync.

  Replay Handling: Subsequent identical requests (same key + same body hash) return the cached
  response without re-executing business logic, ensuring true idempotency for network retries.

  Conflict Detection: When the same idempotency key is used with different request bodies, the
  system throws IdempotencyConflictException and returns a 400 error, preventing misuse of
  idempotency keys.

  Concurrency Protection: The implementation uses per-operation SemaphoreSlim locking to handle
   concurrent requests safely, returning 409 Conflict when the same operation is already
  processing.

  Storage Strategy: Operations are stored in memory with TTL-based expiration (24 hours) and
  automatic cleanup to prevent memory leaks, supporting high-throughput scenarios with
  thread-safe concurrent access.


## 4. Observability: logs/metrics/traces you’d emit to debug slow/failed integrations.
 
  
  - [Demo for debug slow/failed integrations](../docs/Observability-Debugging-Guide.md) 
  - [Demo for KQL Monitoring Queries](../docs/KQL-Monitoring-Queries.md)
  
  Structured Logging: The system emits comprehensive logs at three layers - MediatR pipeline
  behaviors log request lifecycles with unique RequestIds, Service layer logs HTTP operations
  with detailed timing/errors, and Application Insights automatically captures dependency calls
   with correlation tracking.

  Performance Metrics: PerformanceBehaviour automatically measures and alerts on slow requests
  (>500ms warnings, >2000ms critical), while individual ErpService/WarehouseService operations 
  log timeout events with configured timeout values through TaskCanceledException handling. 
  HTTP execution times are automatically tracked by Application Insights dependency telemetry.

  Distributed Tracing: Application Insights dependency tracking correlates HTTP calls across
  the gateway-to-ERP/Warehouse integration points, with CloudRoleName configuration enabling
  end-to-end request tracing through Activity.Current context propagation.

  Exception Correlation: UnhandledExceptionBehaviour captures application-level failures with
  full request context, while Service layer distinguishes between timeout exceptions, HTTP
  errors (4xx/5xx), and network failures with specific error handling and fallback logging.

  KQL Debugging: Use Application Insights queries like dependencies | where target contains 
  "ErpStub" and (duration > 5000 or success == false) to identify slow/failed integration calls
   with timing, status codes, and correlation data for root cause analysis.


## 5. Security controls you’d apply (authN/Z at the edge, input validation, rate limiting, config/secrets, SSRF/misconfig hardening).

I implement **defense-in-depth security** across multiple layers. 

**Authentication/Authorization**: JWT Bearer token validation at API gateway with configurable issuer/audience, role-based authorization on sensitive endpoints, and API key support for service-to-service communication. 
**Input validation**: FluentValidation with data annotations, request size limits (configurable), JSON parsing protection against malicious payloads, and SQL injection prevention through parameterized queries. 
**Rate limiting**: Configurable per-client limits using ASP.NET Core rate limiting with sliding window, IP-based throttling, and burst protection. 
**SSRF protection**: URL validation service preventing internal network access, allowlist-based external URL filtering, and request sanitization. 
**Configuration security**: Azure Key Vault integration for secrets, environment variable separation, and sensitive data encryption at rest. 
**Additional hardening**: CORS configuration, security headers (HSTS, CSP), request/response size limits, and comprehensive error handling without information disclosure.

## 6. Your preferred framework/tooling and why for this use case.

I chose .NET 8.0 with ASP.NET Core as the foundation due to its excellent async/await support for high-concurrency integration scenarios and mature enterprise ecosystem. 

### 1. MediatR with CQRS pattern 
provides a clean separation between queries and commands. This architecture enables the use of powerful pipeline behaviors to address cross-cutting concerns such as caching (to reduce upstream fan-out), validation, performance, and logging. This results in a project structure that is clear, improves code quality, and is easy to maintain.

### 2. Polly library 
The Polly library simplifies integration with external services like ERP and warehouse systems. By injecting HttpClient with resilience patterns—including retry, circuit breaker, timeout, backoff, and jitter—it decouples communication logic from business logic. This approach leverages existing .NET libraries and avoids reinventing the wheel.
### 3. Azure Key Vault 
Azure Key Vault, used with DefaultAzureCredential, ensures enterprise-grade secret management. It maintains a seamless development-to-production workflow, eliminating the need for hardcoded credentials.

### 4. Application Insights 
Application Insights provides comprehensive observability with automatic dependency tracking, structured logging, and correlation IDs across the entire request pipeline. This is crucial for effective troubleshooting, such as finding slow and failed integrations.

### 5. ASP.NET Core API Versioning 
combined with controller inheritance patterns enables zero-breaking-change evolution, allowing V2 to automatically inherit all V1 functionality while adding enhancements. 

### 6. Custom idempotency implementation 
A custom idempotency implementation with middleware provides a unified, cross-cutting solution for handling idempotency. Using SemaphoreSlim and ConcurrentDictionary ensures thread-safe, "exactly-once" semantics with fast-fail timeouts, which is essential for preventing duplicate processing in integration scenarios.

 
---

*These answers reflect the implemented solution in the Integration Gateway codebase, demonstrating production-ready patterns for enterprise API integration scenarios.*