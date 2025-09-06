using NBomber.CSharp;
using NBomber.Http.CSharp;
using IntegrationGateway.PerformanceTests.Utils;

namespace IntegrationGateway.PerformanceTests.Scenarios;

public static class ProductsApiScenarios
{
    public static ScenarioProps CreateCacheTestScenario(TestConfiguration config, string scenarioName = "get_products_cached")
    {
        var httpClient = new HttpClient();
        
        // 使用固定的查询参数来测试缓存效果
        var cachedEndpoint = $"{config.TestSettings.BaseUrl}{config.ApiEndpoints.Products.List}?page=1&pageSize=10";
        
        return Scenario.Create(scenarioName, async context =>
        {
            var request = httpClient.CreateJsonRequest(HttpMethod.Get, cachedEndpoint);
            request.AddAuthenticationHeaders(config.Authentication);
            
            using var response = await httpClient.SendAsync(request, context.CancellationToken);
            
            return response.IsSuccessStatusCode 
                ? Response.Ok() 
                : Response.Fail($"Status: {response.StatusCode}");
        })
        .WithLoadSimulations(
            Simulation.InjectPerSec(rate: 20, during: config.TestSettings.TestDuration)
        );
    }
    
    public static ScenarioProps CreateSingleProductCacheTestScenario(TestConfiguration config, string scenarioName = "get_product_by_id_cached")
    {
        var httpClient = new HttpClient();
        
        // 使用固定的产品ID来测试单个产品的缓存效果
        var fixedProductId = config.TestData.SampleProductIds.FirstOrDefault() ?? "product-001";
        var cachedEndpoint = config.ApiEndpoints.Products.GetById.Replace("{id}", fixedProductId);
        
        return Scenario.Create(scenarioName, async context =>
        {
            var request = httpClient.CreateJsonRequest(
                HttpMethod.Get,
                $"{config.TestSettings.BaseUrl}{cachedEndpoint}"
            );
            
            request.AddAuthenticationHeaders(config.Authentication);
            
            using var response = await httpClient.SendAsync(request, context.CancellationToken);
            
            return response.IsSuccessStatusCode 
                ? Response.Ok() 
                : Response.Fail($"Status: {response.StatusCode}");
        })
        .WithLoadSimulations(
            Simulation.InjectPerSec(rate: 15, during: config.TestSettings.TestDuration)
        );
    }
    
    public static ScenarioProps CreateV2ApiScenario(TestConfiguration config, string scenarioName = "get_products_v2")
    {
        var httpClient = new HttpClient();
        
        return Scenario.Create(scenarioName, async context =>
        {
            var request = httpClient.CreateJsonRequest(
                HttpMethod.Get,
                $"{config.TestSettings.BaseUrl}{config.ApiEndpoints.ProductsV2.List}?page=1&pageSize=20"
            );
            
            request.AddAuthenticationHeaders(config.Authentication);
            
            using var response = await httpClient.SendAsync(request, context.CancellationToken);
            
            return response.IsSuccessStatusCode 
                ? Response.Ok() 
                : Response.Fail($"Status: {response.StatusCode}");
        })
        .WithLoadSimulations(
            Simulation.InjectPerSec(rate: 10, during: config.TestSettings.TestDuration)
        );
    }
    
    public static ScenarioProps CreateMixedWorkloadScenario(TestConfiguration config, string scenarioName = "mixed_workload")
    {
        var httpClient = new HttpClient();
        
        return Scenario.Create(scenarioName, async context =>
        {
            // 根据权重随机选择操作类型 (80% 读取, 20% 写入)
            var random = new Random();
            var operation = random.NextDouble();
            
            if (operation < 0.4) // 40% - GET products list
            {
                var request = httpClient.CreateJsonRequest(
                    HttpMethod.Get,
                    $"{config.TestSettings.BaseUrl}{config.ApiEndpoints.Products.List}?page={random.Next(1, 5)}&pageSize=20"
                );
                request.AddAuthenticationHeaders(config.Authentication);
                
                using var response = await httpClient.SendAsync(request, context.CancellationToken);
                return response.IsSuccessStatusCode 
                    ? Response.Ok() 
                    : Response.Fail($"GET List Status: {response.StatusCode}");
            }
            else if (operation < 0.8) // 40% - GET product by ID
            {
                var productId = TestDataGenerator.GetRandomProductId(config.TestData.SampleProductIds);
                var endpoint = config.ApiEndpoints.Products.GetById.Replace("{id}", productId);
                
                var request = httpClient.CreateJsonRequest(
                    HttpMethod.Get,
                    $"{config.TestSettings.BaseUrl}{endpoint}"
                );
                request.AddAuthenticationHeaders(config.Authentication);
                
                using var response = await httpClient.SendAsync(request, context.CancellationToken);
                return response.IsSuccessStatusCode 
                    ? Response.Ok() 
                    : Response.Fail($"GET By ID Status: {response.StatusCode}");
            }
            else if (operation < 0.9) // 10% - CREATE product
            {
                var testProduct = TestDataGenerator.CreateTestProduct(config.TestData.CreateProductTemplate);
                
                var request = httpClient.CreateJsonRequest(
                    HttpMethod.Post,
                    $"{config.TestSettings.BaseUrl}{config.ApiEndpoints.Products.Create}",
                    testProduct
                );
                request.AddAuthenticationHeaders(config.Authentication);
                
                using var response = await httpClient.SendAsync(request, context.CancellationToken);
                return response.IsSuccessStatusCode 
                    ? Response.Ok() 
                    : Response.Fail($"CREATE Status: {response.StatusCode}");
            }
            else // 10% - UPDATE product
            {
                var productId = TestDataGenerator.GetRandomProductId(config.TestData.SampleProductIds);
                var updateProduct = TestDataGenerator.CreateUpdateProduct(config.TestData.CreateProductTemplate);
                var endpoint = config.ApiEndpoints.Products.Update.Replace("{id}", productId);
                
                var request = httpClient.CreateJsonRequest(
                    HttpMethod.Put,
                    $"{config.TestSettings.BaseUrl}{endpoint}",
                    updateProduct
                );
                request.AddAuthenticationHeaders(config.Authentication);
                
                using var response = await httpClient.SendAsync(request, context.CancellationToken);
                return response.IsSuccessStatusCode 
                    ? Response.Ok() 
                    : Response.Fail($"UPDATE Status: {response.StatusCode}");
            }
        })
        .WithLoadSimulations(
            Simulation.InjectPerSec(rate: 25, during: config.TestSettings.TestDuration)
        );
    }
}