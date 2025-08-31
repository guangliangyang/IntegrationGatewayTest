using System.Net;
using FluentAssertions;
using IntegrationGateway.Models.DTOs;
using IntegrationGateway.Tests.Integration.Base;

namespace IntegrationGateway.Tests.Integration.Products;

/// <summary>
/// Integration tests for write path: POST /api/v1/products
/// Tests the complete chain: Controller → MediatR → Handler → ProductService → ErpService
/// </summary>
public class WritePathIntegrationTests : IntegrationTestBase
{
    public WritePathIntegrationTests(IntegrationTestWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task CreateProduct_WhenErpServiceSucceeds_ShouldReturnCreatedProduct()
    {
        // Arrange - Setup successful ERP create response
        var createRequest = new CreateProductRequest
        {
            Name = "New Integration Test Product",
            Description = "A product created through integration testing",
            Price = 299.99m,
            Category = "Electronics",
            IsActive = true
        };

        var erpResponse = new
        {
            id = "new-prod-123",
            name = createRequest.Name,
            description = createRequest.Description,
            price = createRequest.Price,
            category = createRequest.Category,
            isActive = createRequest.IsActive,
            sku = "ELEC-20241231-1234",
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };

        _mockHttpHandler.SetupErpCreateProductSuccess(erpResponse);

        // Act - Make POST request to create product with idempotency key
        _client.DefaultRequestHeaders.Remove("Idempotency-Key");
        _client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString());
        var response = await _client.PostAsync("/api/v1/products", CreateJsonContent(createRequest));

        // Assert - Verify successful creation
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        // Verify response content
        var createdProduct = await DeserializeResponse<ProductDto>(response);
        createdProduct.Should().NotBeNull();
        createdProduct!.Id.Should().Be("new-prod-123");
        createdProduct.Name.Should().Be(createRequest.Name);
        createdProduct.Description.Should().Be(createRequest.Description);
        createdProduct.Price.Should().Be(createRequest.Price);
        createdProduct.Category.Should().Be(createRequest.Category);
        createdProduct.IsActive.Should().Be(createRequest.IsActive);
    }

    [Fact]
    public async Task CreateProduct_WhenErpValidationFails_ShouldReturnBadRequest()
    {
        // Arrange - Setup ERP validation failure
        var createRequest = new CreateProductRequest
        {
            Name = "", // Invalid empty name
            Description = "Test product",
            Price = 99.99m,
            Category = "Test",
            IsActive = true
        };

        _mockHttpHandler.SetupErpCreateProductFailed(400, "Product name is required");

        // Act - Add idempotency key for write operation
        _client.DefaultRequestHeaders.Remove("Idempotency-Key");
        _client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString());
        var response = await _client.PostAsync("/api/v1/products", CreateJsonContent(createRequest));

        // Assert - Should return BadRequest
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateProduct_WhenErpServiceUnavailable_ShouldReturnServerError()
    {
        // Arrange - Setup ERP service failure
        var createRequest = new CreateProductRequest
        {
            Name = "Test Product",
            Description = "Test Description",
            Price = 49.99m,
            Category = "Test",
            IsActive = true
        };

        _mockHttpHandler.SetupErpCreateProductFailed(503, "ERP service temporarily unavailable");

        // Act - Add idempotency key for write operation
        _client.DefaultRequestHeaders.Remove("Idempotency-Key");
        _client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString());
        var response = await _client.PostAsync("/api/v1/products", CreateJsonContent(createRequest));

        // Assert - Should handle service unavailability
        response.StatusCode.Should().Be(HttpStatusCode.BadGateway);
    }

    [Fact]
    public async Task CreateProduct_WhenErpServiceTimesOut_ShouldReturnServerError()
    {
        // Arrange - Setup ERP timeout
        var createRequest = new CreateProductRequest
        {
            Name = "Timeout Test Product",
            Description = "This request will timeout",
            Price = 199.99m,
            Category = "Test",
            IsActive = true
        };

        _mockHttpHandler.SetupErpTimeout("POST http://localhost:5051/api/products");

        // Act - Add idempotency key for write operation
        _client.DefaultRequestHeaders.Remove("Idempotency-Key");
        _client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString());
        var response = await _client.PostAsync("/api/v1/products", CreateJsonContent(createRequest));

        // Assert - Should handle timeout appropriately (BadGateway is returned for service issues)
        response.StatusCode.Should().Be(HttpStatusCode.BadGateway);
    }

    [Fact]
    public async Task CreateProduct_WithInvalidRequest_ShouldReturnBadRequest()
    {
        // Arrange - Invalid request (missing required fields)
        var invalidRequest = new
        {
            name = "", // Empty name should fail validation
            price = -10.0m // Negative price should fail validation
        };

        // Act - Add idempotency key for write operation
        _client.DefaultRequestHeaders.Remove("Idempotency-Key");
        _client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString());
        var response = await _client.PostAsync("/api/v1/products", CreateJsonContent(invalidRequest));

        // Assert - Should fail model validation before reaching ERP
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateProduct_WithoutAuthorizationHeader_ShouldReturnUnauthorized()
    {
        // Arrange - Create valid request but no auth header
        var createRequest = new CreateProductRequest
        {
            Name = "Unauthorized Test Product",
            Description = "This should be rejected",
            Price = 99.99m,
            Category = "Test",
            IsActive = true
        };

        // Remove any existing auth headers
        _client.DefaultRequestHeaders.Authorization = null;

        // Act - Add idempotency key but no auth header (testing auth specifically)
        _client.DefaultRequestHeaders.Remove("Idempotency-Key");
        _client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString());
        var response = await _client.PostAsync("/api/v1/products", CreateJsonContent(createRequest));

        // Assert - Should require authorization
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateProduct_WithMalformedJson_ShouldReturnBadRequest()
    {
        // Arrange - Malformed JSON content
        var malformedJson = @"{""name"": ""Test"", ""price"": invalid_number}";
        var content = new StringContent(malformedJson, System.Text.Encoding.UTF8, "application/json");

        // Act - Add idempotency key for write operation
        _client.DefaultRequestHeaders.Remove("Idempotency-Key");
        _client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString());
        var response = await _client.PostAsync("/api/v1/products", content);

        // Assert - Should handle malformed JSON
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}