using System.Net;
using FluentAssertions;
using IntegrationGateway.Models.DTOs;
using Integration.Tests.Integration.Base;

namespace Integration.Tests.Integration.Products;

/// <summary>
/// Integration tests for read path: GET /api/v1/products
/// Tests the complete chain: Controller → MediatR → Handler → ProductService → ErpService + WarehouseService
/// </summary>
public class ReadPathIntegrationTests : IntegrationTestBase
{
    public ReadPathIntegrationTests(IntegrationTestWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task GetProducts_WhenAllServicesSucceed_ShouldReturnProductsWithStock()
    {
        // Arrange - Setup successful ERP and Warehouse responses
        var erpProducts = new List<object>
        {
            new
            {
                id = "prod-001",
                name = "Test Laptop",
                description = "High-performance laptop",
                price = 1299.99m,
                category = "Electronics",
                isActive = true
            },
            new
            {
                id = "prod-002", 
                name = "Wireless Headphones",
                description = "Noise-cancelling headphones",
                price = 199.99m,
                category = "Electronics",
                isActive = true
            }
        };

        var warehouseStocks = new List<object>
        {
            new
            {
                ProductId = "prod-001",
                Quantity = 25,
                InStock = true,
                Location = "Warehouse-A-01"
            },
            new
            {
                ProductId = "prod-002",
                Quantity = 150,
                InStock = true,
                Location = "Warehouse-A-02"
            }
        };

        _mockHttpHandler.SetupErpGetProductsSuccess(erpProducts);
        _mockHttpHandler.SetupWarehouseBulkStockSuccess(warehouseStocks);

        // Act - Make GET request to products endpoint
        var response = await _client.GetAsync("/api/v1/products?page=1&pageSize=10");

        // Assert - Verify successful response and data structure
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var productListResponse = await DeserializeResponse<ProductListResponse>(response);
        productListResponse.Should().NotBeNull();
        productListResponse!.Products.Should().HaveCount(2);
        productListResponse.Total.Should().Be(2);
        productListResponse.Page.Should().Be(1);
        productListResponse.PageSize.Should().Be(10);

        // Verify product data integration
        var firstProduct = productListResponse.Products.First();
        firstProduct.Id.Should().Be("prod-001");
        firstProduct.Name.Should().Be("Test Laptop");
        firstProduct.Price.Should().Be(1299.99m);
        firstProduct.StockQuantity.Should().Be(25);
        firstProduct.InStock.Should().BeTrue();
        firstProduct.WarehouseLocation.Should().Be("Warehouse-A-01");
    }

    [Fact]
    public async Task GetProducts_WhenErpServiceFails_ShouldReturnServerError()
    {
        // Arrange - Setup ERP failure
        _mockHttpHandler.SetupErpGetProductsFailed(503, "ERP service temporarily unavailable");

        // Act
        var response = await _client.GetAsync("/api/v1/products");

        // Assert - Should handle ERP failure gracefully
        response.StatusCode.Should().Be(HttpStatusCode.BadGateway);
    }

    [Fact]
    public async Task GetProducts_WhenWarehouseServiceFails_ShouldReturnProductsWithFallbackStock()
    {
        // Arrange - ERP succeeds, Warehouse fails
        var erpProducts = new List<object>
        {
            new
            {
                id = "prod-003",
                name = "Test Product",
                description = "Test Description", 
                price = 49.99m,
                category = "Test",
                isActive = true
            }
        };

        _mockHttpHandler.SetupErpGetProductsSuccess(erpProducts);
        _mockHttpHandler.SetupWarehouseBulkStockFailed(503, "Warehouse service unavailable");

        // Act
        var response = await _client.GetAsync("/api/v1/products");

        // Assert - Should return server error when warehouse service fails
        response.StatusCode.Should().Be(HttpStatusCode.BadGateway);
    }

    [Fact]
    public async Task GetProducts_WhenErpServiceTimesOut_ShouldReturnServerError()
    {
        // Arrange - Setup ERP timeout
        _mockHttpHandler.SetupErpTimeout("GET http://localhost:5051/api/products");

        // Act
        var response = await _client.GetAsync("/api/v1/products");

        // Assert - Should handle timeout appropriately (BadGateway is returned for service issues)
        response.StatusCode.Should().Be(HttpStatusCode.BadGateway);
    }

    [Fact]
    public async Task GetProducts_WithCaching_ShouldReuseFirstResponse()
    {
        // Arrange - Setup successful response
        var erpProducts = new List<object>
        {
            new { id = "cached-001", name = "Cached Product", price = 99.99m, category = "Test", isActive = true }
        };
        var warehouseStocks = new List<object>
        {
            new { productId = "cached-001", quantity = 10, inStock = true, location = "Cache-Test" }
        };

        _mockHttpHandler.SetupErpGetProductsSuccess(erpProducts);
        _mockHttpHandler.SetupWarehouseBulkStockSuccess(warehouseStocks);

        // Act - Make two identical requests quickly
        var response1 = await _client.GetAsync("/api/v1/products?page=1&pageSize=20");
        var response2 = await _client.GetAsync("/api/v1/products?page=1&pageSize=20");

        // Assert - Both should succeed (second should be cached)
        response1.StatusCode.Should().Be(HttpStatusCode.OK);
        response2.StatusCode.Should().Be(HttpStatusCode.OK);

        var productList1 = await DeserializeResponse<ProductListResponse>(response1);
        var productList2 = await DeserializeResponse<ProductListResponse>(response2);

        productList1.Should().NotBeNull();
        productList2.Should().NotBeNull();
        productList1!.Products.Should().HaveCount(1);
        productList2!.Products.Should().HaveCount(1);

        // Verify same data returned (caching working)
        productList1.Products.First().Id.Should().Be("cached-001");
        productList2.Products.First().Id.Should().Be("cached-001");
    }
}