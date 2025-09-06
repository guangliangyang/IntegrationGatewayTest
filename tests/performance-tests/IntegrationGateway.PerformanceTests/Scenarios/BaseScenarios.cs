using NBomber.CSharp;
using NBomber.Http.CSharp;
using IntegrationGateway.PerformanceTests.Utils;

namespace IntegrationGateway.PerformanceTests.Scenarios;

public static class BaseScenarios
{
    public static ScenarioProps CreateGetProductsScenario(TestConfiguration config, string scenarioName = "get_products")
    {
        var httpClient = new HttpClient();
        
        return Scenario.Create(scenarioName, async context =>
        {
            var request = httpClient.CreateJsonRequest(
                HttpMethod.Get,
                $"{config.TestSettings.BaseUrl}{config.ApiEndpoints.Products.List}?page=1&pageSize=20"
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
    
    public static ScenarioProps CreateGetProductByIdScenario(TestConfiguration config, string scenarioName = "get_product_by_id")
    {
        var httpClient = new HttpClient();
        
        return Scenario.Create(scenarioName, async context =>
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
                : Response.Fail($"Status: {response.StatusCode}");
        })
        .WithLoadSimulations(
            Simulation.InjectPerSec(rate: 8, during: config.TestSettings.TestDuration)
        );
    }
    
    public static ScenarioProps CreatePostProductScenario(TestConfiguration config, string scenarioName = "create_product")
    {
        var httpClient = new HttpClient();
        
        return Scenario.Create(scenarioName, async context =>
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
                : Response.Fail($"Status: {response.StatusCode}");
        })
        .WithLoadSimulations(
            Simulation.InjectPerSec(rate: 2, during: config.TestSettings.TestDuration)
        );
    }
    
    public static ScenarioProps CreatePutProductScenario(TestConfiguration config, string scenarioName = "update_product")
    {
        var httpClient = new HttpClient();
        
        return Scenario.Create(scenarioName, async context =>
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
                : Response.Fail($"Status: {response.StatusCode}");
        })
        .WithLoadSimulations(
            Simulation.InjectPerSec(rate: 2, during: config.TestSettings.TestDuration)
        );
    }
}