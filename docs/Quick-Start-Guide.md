# Quick Start Guide

## Prerequisites
- **.NET 8.0 SDK** installed
- **Git** for cloning the repository

## Environment Setup

### 1. Configure Environment Variables
Create a `.env` file in the project root:
```bash
# Required for JWT authentication
Jwt__SecretKey=your-super-secret-jwt-key-that-is-at-least-32-characters-long

# Optional: Application Insights (for production monitoring)
APPLICATIONINSIGHTS_CONNECTION_STRING=your-connection-string
```

## Running the Services

### 1. Gateway (Main API)
```bash
# Navigate to project root
cd IntegrationGateway

# Start the Integration Gateway
dotnet run --project src/IntegrationGateway.Api
```

**Endpoints:**
- HTTP: `http://localhost:5050`
- HTTPS: `https://localhost:7000`
- Swagger: `https://localhost:7000/swagger`

### 2. ERP Stub (Mock ERP Service)
```bash
# In a new terminal
dotnet run --project stubs/ErpStub
```

**Endpoints:**
- HTTP: `http://localhost:5051`
- HTTPS: `https://localhost:7001`
- Swagger: `https://localhost:7001/swagger`

### 3. Warehouse Stub (Mock Warehouse Service)
```bash
# In a new terminal  
dotnet run --project stubs/WarehouseStub
```

**Endpoints:**
- HTTP: `http://localhost:5052`
- HTTPS: `https://localhost:7002`
- Swagger: `https://localhost:7002/swagger`

## Running Tests

### Integration Tests (18 tests total)
```bash
# Run all tests
dotnet test

# Run by category
dotnet test --filter "ReadPathIntegrationTests"    # GET endpoint tests
dotnet test --filter "WritePathIntegrationTests"   # POST endpoint tests  
dotnet test --filter "IdempotencyIntegrationTests" # Idempotency tests

# With detailed output
dotnet test --verbosity normal
```

## API Testing

### Using Swagger UI (Recommended)
1. **Start Gateway**: `dotnet run --project src/IntegrationGateway.Api`
2. **Open Swagger**: `https://localhost:7000/swagger`
3. **Get JWT Token** (for write operations):
   ```bash
   curl http://localhost:5050/api/dev/auth/token?username=testuser
   ```
4. **Authorize in Swagger**: Click 🔒, paste token (no "Bearer" prefix)
5. **Test APIs**: All endpoints available with validation

### Quick Health Checks
```bash
# Gateway health
curl http://localhost:5050/health

# Root endpoint info
curl http://localhost:5050/

# Test read endpoints (no auth required)
curl http://localhost:5050/api/v1/products
curl http://localhost:5050/api/v2/products
```

### HTTP Files (IDE Integration)
The project includes `.http` files for testing, but they need updating:
- `src/IntegrationGateway.Api/IntegrationGateway.http`
- `stubs/ErpStub/ErpStub.http` 
- `stubs/WarehouseStub/WarehouseStub.http`

*Note: Current .http files contain placeholder content and should be updated with actual API endpoints.*

## Authentication & Write Operations

### JWT Token Required
Write operations (POST, PUT) require:
- **Idempotency-Key** header (16-128 characters)
- **Authorization** header with JWT token

### Get Development JWT Token
```bash
# Start gateway with .env configured
dotnet run --project src/IntegrationGateway.Api

# Get token
curl http://localhost:5050/api/dev/auth/token?username=testuser
```

### Example Authenticated Request
```bash
# Get token first
TOKEN=$(curl -s http://localhost:5050/api/dev/auth/token?username=testuser | jq -r '.token')

# Create product with authentication and idempotency
curl -X POST http://localhost:5050/api/v1/products \
  -H "Authorization: Bearer $TOKEN" \
  -H "Idempotency-Key: $(uuidgen)" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Test Product",
    "description": "Created via API",
    "price": 99.99,
    "category": "Test",
    "isActive": true
  }'
```

## Development Workflow

### 1. Complete Setup
```bash
# Terminal 1: ERP Stub
dotnet run --project stubs/ErpStub

# Terminal 2: Warehouse Stub  
dotnet run --project stubs/WarehouseStub

# Terminal 3: Gateway (connects to stubs)
dotnet run --project src/IntegrationGateway.Api
```

### 2. Verify Everything Works
```bash
# Test all endpoints work
curl http://localhost:5050/health
curl http://localhost:5051/api/products
curl http://localhost:5052/api/stock

# Run integration tests
dotnet test
```

### 3. Development Testing
- **Swagger UI**: `https://localhost:7000/swagger` (best for interactive testing)
- **Integration Tests**: `dotnet test` (automated validation)
- **Manual Testing**: Use curl or Postman with JWT tokens

## Configuration Notes

### Default Service URLs
The gateway connects to:
- **ERP Service**: `http://localhost:5051` 
- **Warehouse Service**: `http://localhost:5052`

### Production Configuration
Update `appsettings.json` for production:
```json
{
  "ErpService": {
    "BaseUrl": "https://your-production-erp.com"
  },
  "WarehouseService": {
    "BaseUrl": "https://your-production-warehouse.com"  
  }
}
```

## Troubleshooting

### Common Issues
- **JWT Authentication**: Ensure `Jwt__SecretKey` in `.env` file
- **Port Conflicts**: Check `Properties/launchSettings.json` to change ports
- **Service Connection**: Start stubs before gateway
- **Missing Dependencies**: Run `dotnet restore` and `dotnet build`

### Debug Mode
```bash
# Run with detailed logging
dotnet run --project src/IntegrationGateway.Api --environment Development

# Test specific scenarios
dotnet test --filter "CreateProduct_WhenErpServiceSucceeds" --verbosity detailed
```

---

🚀 **Quick Start**: Run all 3 services, open `https://localhost:7000/swagger`, get a JWT token, and start testing!