# Testing Guide

## Test Overview

The project includes **18 integration tests** focused on testing the complete API pipeline with real HTTP requests and mocked external dependencies.

### 🔗 **Integration Tests Only**
- **Read Path**: GET `/api/v1/products` endpoint testing
- **Write Path**: POST `/api/v1/products` endpoint testing  
- **Idempotency**: Comprehensive idempotency middleware testing
- **External Dependencies**: Mocked ERP and Warehouse services
- **Authentication**: JWT token validation

## Test Structure

```
tests/IntegrationGateway.Tests/
├── Integration/
│   ├── Base/
│   │   ├── IntegrationTestBase.cs       # Test setup & JWT authentication
│   │   └── MockHttpMessageHandler.cs   # HTTP mocking for ERP/Warehouse
│   └── Products/
│       ├── ReadPathIntegrationTests.cs  # GET endpoint tests (5 tests)
│       ├── WritePathIntegrationTests.cs # POST endpoint tests (7 tests)
│       └── IdempotencyIntegrationTests.cs # Idempotency tests (6 tests)
└── appsettings.Test.json               # Test configuration
```

## Running Tests

### All Tests
```bash
# Run all 18 tests
dotnet test

# With detailed output
dotnet test --verbosity normal

# Show individual test results
dotnet test --logger "console;verbosity=detailed"
```

### By Test Category
```bash
# Read path tests (GET /products)
dotnet test --filter "ReadPathIntegrationTests"

# Write path tests (POST /products)  
dotnet test --filter "WritePathIntegrationTests"

# Idempotency tests
dotnet test --filter "IdempotencyIntegrationTests"
```

### Specific Test Examples
```bash
# Test successful product creation
dotnet test --filter "CreateProduct_WhenErpServiceSucceeds_ShouldReturnCreatedProduct"

# Test ERP service timeout handling
dotnet test --filter "CreateProduct_WhenErpServiceTimesOut_ShouldReturnServerError"

# Test idempotency key validation
dotnet test --filter "CreateProduct_WithoutIdempotencyKey_ShouldReturnBadRequest"
```

## Test Categories

### 📖 **Read Path Tests** (5 tests)
- Tests GET `/api/v1/products` endpoint
- ERP and Warehouse service integration
- Error handling and fallback scenarios
- Caching behavior validation

### ✍️ **Write Path Tests** (7 tests)  
- Tests POST `/api/v1/products` endpoint
- ERP service integration for product creation
- Authentication and authorization
- Input validation and error handling

### 🔄 **Idempotency Tests** (6 tests)
- Idempotency-Key header validation
- Request caching and replay detection
- Body hash conflict detection
- Key format validation (16-128 characters)

## Test Environment Setup

### Prerequisites
```bash
# Ensure .env file exists for JWT configuration
cp .env.example .env  # If needed

# Required environment variable:
# Jwt__SecretKey=your-256-bit-secret-key-here
```

### Authentication
Tests automatically generate JWT tokens using configuration from `appsettings.Test.json`. No manual token setup required.

### External Service Mocking
- **ERP Service**: Mocked at `http://localhost:5051`
- **Warehouse Service**: Mocked at `http://localhost:5052`
- **Automatic Setup**: MockHttpMessageHandler handles all HTTP mocking

## Common Test Commands

### Quick Development Testing
```bash
# Fast feedback during development
dotnet test --filter "CreateProduct_WhenErpServiceSucceeds" --verbosity minimal

# Test authentication works
dotnet test --filter "CreateProduct_WithoutAuthorizationHeader" 

# Test idempotency validation
dotnet test --filter "WithoutIdempotencyKey"
```

### Comprehensive Testing
```bash
# Run all tests with summary
dotnet test --logger "console;verbosity=normal"

# Test specific scenarios
dotnet test --filter "Timeout" --verbosity detailed
dotnet test --filter "Fails" --verbosity normal
```

## Test Technologies

### 🛠️ **Testing Stack**
- **xUnit**: Test framework
- **FluentAssertions**: Readable assertions  
- **WebApplicationFactory**: In-memory test server
- **HttpClient**: Real HTTP request testing
- **Custom Mocking**: MockHttpMessageHandler for external services

### 🔧 **Test Configuration**
- **JWT Authentication**: Automatic token generation
- **Environment**: Isolated test configuration
- **Logging**: Detailed test execution logging
- **HTTP Mocking**: ERP and Warehouse service simulation

## Troubleshooting

### Environment Issues
```bash
# Check JWT secret is configured
echo $Jwt__SecretKey

# Verify test configuration
cat tests/IntegrationGateway.Tests/appsettings.Test.json
```

### Common Test Failures
- **JWT Authentication**: Ensure `Jwt__SecretKey` is set in environment
- **Slow Tests**: Integration tests may take 5-10 seconds per test
- **Port Conflicts**: Tests use in-memory server, no port conflicts

### Debug Specific Tests
```bash
# Run single test with full details
dotnet test --filter "CreateProduct_WhenErpServiceSucceeds_ShouldReturnCreatedProduct" --logger "console;verbosity=detailed"

# Test authentication flow
dotnet test --filter "WithoutAuthorizationHeader" --verbosity normal
```

---

💡 **Quick Start**: Run `dotnet test` to execute all 18 integration tests and verify the complete API pipeline works correctly.