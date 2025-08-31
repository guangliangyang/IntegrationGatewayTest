using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using IntegrationGateway.Models.External;
using IntegrationGateway.Services.Interfaces;
using IntegrationGateway.Models.Exceptions;
using IntegrationGateway.Services.Configuration;

namespace IntegrationGateway.Services.Implementation;

public class WarehouseService : IWarehouseService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<WarehouseService> _logger;
    private readonly WarehouseServiceOptions _options;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public WarehouseService(IHttpClientFactory httpClientFactory, ILogger<WarehouseService> logger, IOptions<WarehouseServiceOptions> options)
    {
        _httpClient = httpClientFactory.CreateClient("WarehouseClient");
        _logger = logger;
        _options = options.Value;
    }


    public async Task<WarehouseResponse<WarehouseStock>> GetStockAsync(string productId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(productId))
            throw new ValidationException("Product ID cannot be null or empty");
            
        return await ExecuteWithFallbackAsync<WarehouseStock>(
            async () =>
            {
                _logger.LogDebug("Getting stock from Warehouse: {ProductId}", productId);
                return await _httpClient.GetAsync($"/api/stock/{productId}", cancellationToken);
            },
            async response =>
            {
                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                var apiResponse = JsonSerializer.Deserialize<ApiResponse<WarehouseStock>>(json, JsonOptions);
                
                if (apiResponse?.Success == true && apiResponse.Data != null)
                {
                    _logger.LogDebug("Successfully retrieved stock from Warehouse: {ProductId}, Quantity: {Quantity}", 
                        productId, apiResponse.Data.Quantity);
                    return apiResponse.Data;
                }
                
                var errorMsg = apiResponse?.ErrorMessage ?? "Unknown error from Warehouse";
                _logger.LogError("Warehouse API returned error for stock {ProductId}: {ErrorMessage}", productId, errorMsg);
                throw new ExternalServiceException("Warehouse", errorMsg);
            },
            () => CreateDefaultStock(productId),
            $"getting stock for {productId}"
        );
    }

    public async Task<WarehouseResponse<BulkStockResponse>> GetBulkStockAsync(List<string> productIds, CancellationToken cancellationToken = default)
    {
        if (productIds == null)
            throw new ValidationException("Product IDs list cannot be null");
        if (productIds.Count == 0)
            return CreateSuccessResponse(new BulkStockResponse { Stocks = new List<WarehouseStock>() });
        if (productIds.Count > 1000)
            throw new ValidationException("Cannot request more than 1000 products at once");
            
        return await ExecuteWithFallbackAsync<BulkStockResponse>(
            async () =>
            {
                _logger.LogDebug("Getting bulk stock from Warehouse: {ProductCount} products", productIds.Count);
                
                // Use POST for large requests to avoid URL length limits
                if (productIds.Count > 50)
                {
                    return await PostBulkStockRequest(productIds, cancellationToken);
                }
                
                // Use GET for smaller requests
                var queryString = string.Join("&", productIds.Select(id => $"productIds={Uri.EscapeDataString(id)}"));
                return await _httpClient.GetAsync($"/api/stock/bulk?{queryString}", cancellationToken);
            },
            async response =>
            {
                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                var apiResponse = JsonSerializer.Deserialize<ApiResponse<BulkStockResponse>>(json, JsonOptions);
                
                if (apiResponse?.Success == true && apiResponse.Data != null)
                {
                    _logger.LogDebug("Successfully retrieved bulk stock from Warehouse: {FoundCount} found, {NotFoundCount} not found", 
                        apiResponse.Data.Stocks.Count, apiResponse.Data.NotFound.Count);
                    return apiResponse.Data;
                }
                
                var errorMsg = apiResponse?.ErrorMessage ?? "Unknown error from Warehouse";
                _logger.LogError("Warehouse API returned error for bulk stock request: {ErrorMessage}", errorMsg);
                throw new ExternalServiceException("Warehouse", errorMsg);
            },
            () => new BulkStockResponse 
            { 
                Stocks = productIds.Select(CreateDefaultStock).ToList() 
            },
            "getting bulk stock"
        );
    }

    private async Task<HttpResponseMessage> PostBulkStockRequest(List<string> productIds, CancellationToken cancellationToken)
    {
        var requestBody = new { ProductIds = productIds };
        var json = JsonSerializer.Serialize(requestBody, JsonOptions);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        return await _httpClient.PostAsync("/api/stock/bulk", content, cancellationToken);
    }

    private static WarehouseStock CreateDefaultStock(string productId)
    {
        return new WarehouseStock
        {
            ProductId = productId,
            Quantity = 0,
            InStock = false,
            LastUpdated = DateTime.UtcNow
        };
    }

    private static WarehouseResponse<T> CreateSuccessResponse<T>(T data)
    {
        return new WarehouseResponse<T>
        {
            Success = true,
            Data = data,
            RequestId = System.Diagnostics.Activity.Current?.Id
        };
    }

    private async Task<WarehouseResponse<T>> ExecuteWithFallbackAsync<T>(
        Func<Task<HttpResponseMessage>> httpOperation,
        Func<HttpResponseMessage, Task<T>> successHandler,
        Func<T> fallbackHandler,
        string operationDescription)
    {
        var requestId = System.Diagnostics.Activity.Current?.Id;
        
        try
        {
            using var response = await httpOperation();

            if (response.IsSuccessStatusCode)
            {
                var data = await successHandler(response);
                return new WarehouseResponse<T>
                {
                    Success = true,
                    Data = data,
                    RequestId = requestId
                };
            }

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogDebug("Resource not found in Warehouse for {Operation}. Using fallback.", operationDescription);
                
                // Return fallback data for NotFound (warehouse-specific business logic)
                return new WarehouseResponse<T>
                {
                    Success = true,
                    Data = fallbackHandler(),
                    RequestId = requestId
                };
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            var errorMessage = $"Warehouse service error: {response.StatusCode}";
            
            _logger.LogError("Warehouse service error {Operation}: {StatusCode} - {Content}", 
                operationDescription, response.StatusCode, errorContent);
            
            return new WarehouseResponse<T>
            {
                Success = false,
                ErrorMessage = errorMessage,
                RequestId = requestId
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP request failed while {Operation}. Using fallback.", operationDescription);
            
            // Warehouse service graceful degradation - return fallback data
            return new WarehouseResponse<T>
            {
                Success = true,
                Data = fallbackHandler(),
                RequestId = requestId
            };
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            _logger.LogError(ex, "Request timeout while {Operation} - configured timeout: {TimeoutSeconds}s. Using fallback.", 
                operationDescription, _options.TimeoutSeconds);
            
            // Warehouse service graceful degradation - return fallback data
            return new WarehouseResponse<T>
            {
                Success = true,
                Data = fallbackHandler(),
                RequestId = requestId
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while {Operation}. Using fallback.", operationDescription);
            
            // Warehouse service graceful degradation - return fallback data
            return new WarehouseResponse<T>
            {
                Success = true,
                Data = fallbackHandler(),
                RequestId = requestId
            };
        }
    }
}