using System.Net;
using FluentAssertions;
using IntegrationGateway.Models.DTOs;
using Integration.Tests.Integration.Base;

namespace Integration.Tests.Integration.Products;

/// <summary>
/// Integration tests for idempotency handling in write operations
/// Tests the complete chain: IdempotencyMiddleware → Controller → MediatR → Handler → IdempotencyService
/// </summary>
public class IdempotencyIntegrationTests : IntegrationTestBase
{
    public IdempotencyIntegrationTests(IntegrationTestWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task CreateProduct_FirstRequest_ShouldExecuteAndCacheResponse()
    {
        // Arrange
        var idempotencyKey = Guid.NewGuid().ToString();
        var createRequest = new CreateProductRequest
        {
            Name = "Idempotent Test Product",
            Description = "Testing idempotency behavior",
            Price = 149.99m,
            Category = "Test",
            IsActive = true
        };

        var erpResponse = new
        {
            id = "idempotent-prod-001",
            name = createRequest.Name,
            description = createRequest.Description,
            price = createRequest.Price,
            category = createRequest.Category,
            isActive = createRequest.IsActive,
            sku = "TEST-IDEM-001",
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };

        _mockHttpHandler.SetupErpCreateProductSuccess(erpResponse);

        // Act - First request with idempotency key
        _client.DefaultRequestHeaders.Add("Idempotency-Key", idempotencyKey);
        var response = await _client.PostAsync("/api/v1/products", CreateJsonContent(createRequest));

        // Assert - Should succeed and execute business logic
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var createdProduct = await DeserializeResponse<ProductDto>(response);
        createdProduct.Should().NotBeNull();
        createdProduct!.Id.Should().Be("idempotent-prod-001");
        createdProduct.Name.Should().Be(createRequest.Name);

        // Response should be successful - no specific headers required
    }

    [Fact]
    public async Task CreateProduct_ReplayRequest_ShouldReturnCachedResponse()
    {
        // Arrange - Same request as previous test
        var idempotencyKey = Guid.NewGuid().ToString();
        var createRequest = new CreateProductRequest
        {
            Name = "Replay Test Product",
            Description = "Testing replay behavior",
            Price = 89.99m,
            Category = "Test",
            IsActive = true
        };

        var erpResponse = new
        {
            id = "replay-prod-001",
            name = createRequest.Name,
            description = createRequest.Description,
            price = createRequest.Price,
            category = createRequest.Category,
            isActive = createRequest.IsActive,
            sku = "TEST-REPLAY-001",
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };

        _mockHttpHandler.SetupErpCreateProductSuccess(erpResponse);

        // Act - Send same request twice with same idempotency key
        _client.DefaultRequestHeaders.Remove("Idempotency-Key");
        _client.DefaultRequestHeaders.Add("Idempotency-Key", idempotencyKey);

        var response1 = await _client.PostAsync("/api/v1/products", CreateJsonContent(createRequest));
        var response2 = await _client.PostAsync("/api/v1/products", CreateJsonContent(createRequest));

        // Assert - Both requests should succeed
        response1.StatusCode.Should().Be(HttpStatusCode.Created);
        response2.StatusCode.Should().Be(HttpStatusCode.Created);

        // Verify both responses have the same product ID (cached response)
        var product1 = await DeserializeResponse<ProductDto>(response1);
        var product2 = await DeserializeResponse<ProductDto>(response2);

        product1.Should().NotBeNull();
        product2.Should().NotBeNull();
        product1!.Id.Should().Be(product2!.Id);
        product1.Name.Should().Be(product2.Name);

        // Both responses should be successful - no specific headers required
    }

    [Fact]
    public async Task CreateProduct_SameKeyDifferentBody_ShouldReturnConflictError()
    {
        // Arrange
        var idempotencyKey = Guid.NewGuid().ToString();
        
        var firstRequest = new CreateProductRequest
        {
            Name = "Original Product",
            Description = "Original description",
            Price = 100.00m,
            Category = "Original",
            IsActive = true
        };

        var conflictRequest = new CreateProductRequest
        {
            Name = "Different Product", // Different name = different body
            Description = "Different description", 
            Price = 200.00m, // Different price
            Category = "Different", // Different category
            IsActive = false // Different status
        };

        var erpResponse = new
        {
            id = "conflict-test-001",
            name = firstRequest.Name,
            description = firstRequest.Description,
            price = firstRequest.Price,
            category = firstRequest.Category,
            isActive = firstRequest.IsActive,
            sku = "TEST-CONFLICT-001",
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };

        _mockHttpHandler.SetupErpCreateProductSuccess(erpResponse);

        // Act - Send first request successfully
        _client.DefaultRequestHeaders.Remove("Idempotency-Key");
        _client.DefaultRequestHeaders.Add("Idempotency-Key", idempotencyKey);

        var response1 = await _client.PostAsync("/api/v1/products", CreateJsonContent(firstRequest));
        
        // Send second request with same key but different body
        var response2 = await _client.PostAsync("/api/v1/products", CreateJsonContent(conflictRequest));

        // Assert
        response1.StatusCode.Should().Be(HttpStatusCode.Created);
        response2.StatusCode.Should().Be(HttpStatusCode.Conflict); // IdempotencyConflictException returns 409

        // Verify conflict response structure
        var errorResponse = await DeserializeResponse<object>(response2);
        errorResponse.Should().NotBeNull();
        
        // Verify error response contains conflict information
        var responseContent = await response2.Content.ReadAsStringAsync();
        responseContent.Should().Contain("idempotency_conflict");
    }

    [Fact]
    public async Task CreateProduct_DifferentKeys_ShouldBothSucceed()
    {
        // Arrange
        var idempotencyKey1 = Guid.NewGuid().ToString();
        var idempotencyKey2 = Guid.NewGuid().ToString();

        var product1Request = new CreateProductRequest
        {
            Name = "Product One",
            Description = "First product",
            Price = 50.00m,
            Category = "Category1",
            IsActive = true
        };

        var product2Request = new CreateProductRequest
        {
            Name = "Product Two", 
            Description = "Second product",
            Price = 75.00m,
            Category = "Category2",
            IsActive = true
        };

        // Setup different ERP responses for each
        var erpResponse1 = new { id = "different-key-001", name = product1Request.Name, price = product1Request.Price };
        var erpResponse2 = new { id = "different-key-002", name = product2Request.Name, price = product2Request.Price };

        _mockHttpHandler.SetupErpCreateProductSuccess(erpResponse1);

        // Act - Send requests with different idempotency keys
        _client.DefaultRequestHeaders.Remove("Idempotency-Key");
        _client.DefaultRequestHeaders.Add("Idempotency-Key", idempotencyKey1);
        var response1 = await _client.PostAsync("/api/v1/products", CreateJsonContent(product1Request));

        // Update mock for second request
        _mockHttpHandler.ClearAll();
        _mockHttpHandler.SetupErpCreateProductSuccess(erpResponse2);

        _client.DefaultRequestHeaders.Remove("Idempotency-Key");
        _client.DefaultRequestHeaders.Add("Idempotency-Key", idempotencyKey2);
        var response2 = await _client.PostAsync("/api/v1/products", CreateJsonContent(product2Request));

        // Assert - Both should succeed as separate operations
        response1.StatusCode.Should().Be(HttpStatusCode.Created);
        response2.StatusCode.Should().Be(HttpStatusCode.Created);

        var createdProduct1 = await DeserializeResponse<ProductDto>(response1);
        var createdProduct2 = await DeserializeResponse<ProductDto>(response2);

        createdProduct1.Should().NotBeNull();
        createdProduct2.Should().NotBeNull();
        createdProduct1!.Name.Should().Be("Product One");
        createdProduct2!.Name.Should().Be("Product Two");

        // They should have different IDs
        createdProduct1.Id.Should().NotBe(createdProduct2.Id);
    }

    [Fact]
    public async Task CreateProduct_WithoutIdempotencyKey_ShouldReturnBadRequest()
    {
        // Arrange
        var createRequest = new CreateProductRequest
        {
            Name = "No Idempotency Key Product",
            Description = "Should fail without idempotency key",
            Price = 199.99m,
            Category = "Normal",
            IsActive = true
        };

        // Act - Send request without Idempotency-Key header
        _client.DefaultRequestHeaders.Remove("Idempotency-Key");
        var response = await _client.PostAsync("/api/v1/products", CreateJsonContent(createRequest));

        // Assert - Should return BadRequest because Idempotency-Key is required for POST requests
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // Verify error response contains missing idempotency key information
        var responseContent = await response.Content.ReadAsStringAsync();
        responseContent.Should().Contain("missing_idempotency_key"); // Special handling for idempotency ArgumentException
        responseContent.Should().Contain("Idempotency-Key header is required for POST and PUT requests");
    }

    [Fact]
    public async Task CreateProduct_WithInvalidIdempotencyKey_ShouldReturnBadRequest()
    {
        // Arrange
        var createRequest = new CreateProductRequest
        {
            Name = "Invalid Key Test Product",
            Description = "Testing invalid idempotency key",
            Price = 99.99m,
            Category = "Test",
            IsActive = true
        };

        // Act - Send request with too short idempotency key (less than 16 characters)
        _client.DefaultRequestHeaders.Remove("Idempotency-Key");
        _client.DefaultRequestHeaders.Add("Idempotency-Key", "short"); // Only 5 characters
        var response = await _client.PostAsync("/api/v1/products", CreateJsonContent(createRequest));

        // Assert - Should return BadRequest because key is too short
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // Verify error response contains invalid idempotency key information
        var responseContent = await response.Content.ReadAsStringAsync();
        responseContent.Should().Contain("invalid_idempotency_key"); // Special handling for idempotency ArgumentException
        responseContent.Should().Contain("must be between 16 and 128 characters");
    }
}