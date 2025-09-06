using System.Text;
using NBomber.Http;
using Newtonsoft.Json;

namespace IntegrationGateway.PerformanceTests.Utils;

public static class HttpClientExtensions
{
    public static HttpRequestMessage CreateJsonRequest(
        this HttpClient client, 
        HttpMethod method, 
        string uri, 
        object? content = null,
        Dictionary<string, string>? headers = null)
    {
        var request = new HttpRequestMessage(method, uri);
        
        if (content != null)
        {
            var json = JsonConvert.SerializeObject(content);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }
        
        if (headers != null)
        {
            foreach (var header in headers)
            {
                request.Headers.Add(header.Key, header.Value);
            }
        }
        
        return request;
    }
    
    public static void AddAuthenticationHeaders(this HttpRequestMessage request, AuthenticationSettings authSettings)
    {
        if (!authSettings.EnableAuth) return;
        
        if (!string.IsNullOrEmpty(authSettings.BearerToken))
        {
            request.Headers.Add("Authorization", $"Bearer {authSettings.BearerToken}");
        }
        
        if (!string.IsNullOrEmpty(authSettings.SubscriptionKey))
        {
            request.Headers.Add("Ocp-Apim-Subscription-Key", authSettings.SubscriptionKey);
        }
    }
}

public static class TestDataGenerator
{
    private static readonly Random Random = new();
    
    public static string GetRandomProductId(string[] productIds)
    {
        return productIds.Length > 0 
            ? productIds[Random.Next(productIds.Length)]
            : $"product-{Random.Next(1, 1000):D3}";
    }
    
    public static object CreateTestProduct(CreateProductTemplate template)
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var randomId = Random.Next(1000, 9999);
        
        return new
        {
            name = string.Format(template.Name, $"{timestamp}-{randomId}"),
            description = template.Description,
            price = template.Price + (decimal)(Random.NextDouble() * 100),
            category = template.Category,
            isActive = template.IsActive
        };
    }
    
    public static object CreateUpdateProduct(CreateProductTemplate template)
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var randomId = Random.Next(1000, 9999);
        
        return new
        {
            name = string.Format(template.Name, $"Updated-{timestamp}-{randomId}"),
            description = $"Updated {template.Description}",
            price = template.Price + (decimal)(Random.NextDouble() * 200),
            category = template.Category,
            isActive = !template.IsActive
        };
    }
}