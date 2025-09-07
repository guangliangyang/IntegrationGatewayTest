using System.Net;
using System.Text;
using System.Text.Json;

namespace Integration.Tests.Integration.Base;

/// <summary>
/// Mock HTTP message handler for controlled HTTP responses in integration tests
/// </summary>
public class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly Dictionary<string, Func<HttpRequestMessage, HttpResponseMessage>> _responses = new();
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var key = $"{request.Method} {request.RequestUri}";
        
        if (_responses.TryGetValue(key, out var responseFunc))
        {
            var response = responseFunc(request);
            return Task.FromResult(response);
        }

        // Try to match with path only (ignoring query parameters) for bulk requests
        if (request.RequestUri != null)
        {
            var pathOnlyKey = $"{request.Method} {request.RequestUri.Scheme}://{request.RequestUri.Authority}{request.RequestUri.AbsolutePath}";
            if (_responses.TryGetValue(pathOnlyKey, out var pathResponseFunc))
            {
                var response = pathResponseFunc(request);
                return Task.FromResult(response);
            }
        }

        // Default response for unmatched requests
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent("{\"success\":false,\"errorMessage\":\"Endpoint not mocked\"}", 
                Encoding.UTF8, "application/json")
        });
    }

    // ERP Mock Setup Methods
    public void SetupErpGetProductsSuccess(List<object> products)
    {
        var response = new
        {
            success = true,
            data = products,
            requestId = Guid.NewGuid().ToString()
        };

        _responses["GET http://localhost:5051/api/products"] = _ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(response, _jsonOptions), 
                    Encoding.UTF8, "application/json")
            };
    }

    public void SetupErpGetProductsFailed(int statusCode = 503, string errorMessage = "ERP service unavailable")
    {
        var response = new
        {
            success = false,
            errorMessage = errorMessage,
            requestId = Guid.NewGuid().ToString()
        };

        _responses["GET http://localhost:5051/api/products"] = _ =>
            new HttpResponseMessage((HttpStatusCode)statusCode)
            {
                Content = new StringContent(JsonSerializer.Serialize(response, _jsonOptions), 
                    Encoding.UTF8, "application/json")
            };
    }

    public void SetupErpCreateProductSuccess(object product)
    {
        var response = new
        {
            success = true,
            data = product,
            requestId = Guid.NewGuid().ToString()
        };

        _responses["POST http://localhost:5051/api/products"] = _ =>
            new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = new StringContent(JsonSerializer.Serialize(response, _jsonOptions), 
                    Encoding.UTF8, "application/json")
            };
    }

    public void SetupErpCreateProductFailed(int statusCode = 400, string errorMessage = "Validation failed")
    {
        var response = new
        {
            success = false,
            errorMessage = errorMessage,
            requestId = Guid.NewGuid().ToString()
        };

        _responses["POST http://localhost:5051/api/products"] = _ =>
            new HttpResponseMessage((HttpStatusCode)statusCode)
            {
                Content = new StringContent(JsonSerializer.Serialize(response, _jsonOptions), 
                    Encoding.UTF8, "application/json")
            };
    }

    public void SetupErpTimeout(string endpoint)
    {
        _responses[endpoint] = _ => throw new TaskCanceledException("Request timeout", 
            new TimeoutException("The operation has timed out"));
    }

    // Warehouse Mock Setup Methods
    public void SetupWarehouseBulkStockSuccess(List<object> stocks, List<string> notFound = null)
    {
        var response = new
        {
            success = true,
            data = new
            {
                stocks = stocks ?? new List<object>(),
                notFound = notFound ?? new List<string>()
            },
            requestId = Guid.NewGuid().ToString()
        };

        _responses["GET http://localhost:5052/api/stock/bulk"] = _ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(response, _jsonOptions), 
                    Encoding.UTF8, "application/json")
            };
    }

    public void SetupWarehouseBulkStockFailed(int statusCode = 503, string errorMessage = "Warehouse service unavailable")
    {
        var response = new
        {
            success = false,
            errorMessage = errorMessage,
            requestId = Guid.NewGuid().ToString()
        };

        _responses["GET http://localhost:5052/api/stock/bulk"] = _ =>
            new HttpResponseMessage((HttpStatusCode)statusCode)
            {
                Content = new StringContent(JsonSerializer.Serialize(response, _jsonOptions), 
                    Encoding.UTF8, "application/json")
            };
    }

    // Generic Setup Methods
    public void SetupResponse(string method, string url, HttpStatusCode statusCode, object content = null)
    {
        var key = $"{method.ToUpper()} {url}";
        
        _responses[key] = _ =>
        {
            var response = new HttpResponseMessage(statusCode);
            if (content != null)
            {
                response.Content = new StringContent(JsonSerializer.Serialize(content, _jsonOptions), 
                    Encoding.UTF8, "application/json");
            }
            return response;
        };
    }

    public void ClearAll()
    {
        _responses.Clear();
    }
}