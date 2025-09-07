using NBomber.CSharp;
using NBomber.Contracts;
using NBomber.Http.CSharp;
using Performance.Tests.Utils;

namespace Performance.Tests.Scenarios;

public static class BaseScenarios
{
    public static ScenarioProps CreateGetProductsScenario(TestConfiguration config, string scenarioName = "get_products")
    {
        using var httpClient = new HttpClient();
        
        return Scenario.Create(scenarioName, async context =>
        {
            var step = await Step.Run("get_products", context, async () =>
            {
                var request = Http.CreateRequest("GET", 
                    $"{config.TestSettings.BaseUrl}{config.ApiEndpoints.Products.List}?page=1&pageSize=20")
                    .WithHeader("Accept", "application/json");
                
                request.AddAuthenticationHeaders(config.Authentication);
                
                var response = await Http.Send(httpClient, request);
                return response;
            });
            
            return Response.Ok();
        })
        .WithoutWarmUp()
        .WithLoadSimulations(
            Simulation.Inject(rate: 10, interval: TimeSpan.FromSeconds(1), during: config.TestSettings.TestDuration)
        );
    }
    
    public static ScenarioProps CreateGetProductByIdScenario(TestConfiguration config, string scenarioName = "get_product_by_id")
    {
        using var httpClient = new HttpClient();
        
        return Scenario.Create(scenarioName, async context =>
        {
            var step = await Step.Run("get_product_by_id", context, async () =>
            {
                var productId = TestDataGenerator.GetRandomProductId(config.TestData.SampleProductIds);
                var endpoint = config.ApiEndpoints.Products.GetById.Replace("{id}", productId);
                
                var request = Http.CreateRequest("GET", $"{config.TestSettings.BaseUrl}{endpoint}")
                    .WithHeader("Accept", "application/json");
                
                request.AddAuthenticationHeaders(config.Authentication);
                
                var response = await Http.Send(httpClient, request);
                return response;
            });
            
            return Response.Ok();
        })
        .WithoutWarmUp()
        .WithLoadSimulations(
            Simulation.Inject(rate: 8, interval: TimeSpan.FromSeconds(1), during: config.TestSettings.TestDuration)
        );
    }
    
    public static ScenarioProps CreatePostProductScenario(TestConfiguration config, string scenarioName = "create_product")
    {
        using var httpClient = new HttpClient();
        
        return Scenario.Create(scenarioName, async context =>
        {
            var step = await Step.Run("create_product", context, async () =>
            {
                var testProduct = TestDataGenerator.CreateTestProduct(config.TestData.CreateProductTemplate);
                
                var request = Http.CreateRequest("POST", $"{config.TestSettings.BaseUrl}{config.ApiEndpoints.Products.Create}")
                    .WithHeader("Accept", "application/json")
                    .WithHeader("Content-Type", "application/json")
                    .WithJsonBody(testProduct);
                
                request.AddAuthenticationHeaders(config.Authentication);
                
                var response = await Http.Send(httpClient, request);
                return response;
            });
            
            return Response.Ok();
        })
        .WithoutWarmUp()
        .WithLoadSimulations(
            Simulation.Inject(rate: 2, interval: TimeSpan.FromSeconds(1), during: config.TestSettings.TestDuration)
        );
    }
    
    public static ScenarioProps CreatePutProductScenario(TestConfiguration config, string scenarioName = "update_product")
    {
        using var httpClient = new HttpClient();
        
        return Scenario.Create(scenarioName, async context =>
        {
            var step = await Step.Run("update_product", context, async () =>
            {
                var productId = TestDataGenerator.GetRandomProductId(config.TestData.SampleProductIds);
                var updateProduct = TestDataGenerator.CreateUpdateProduct(config.TestData.CreateProductTemplate);
                var endpoint = config.ApiEndpoints.Products.Update.Replace("{id}", productId);
                
                var request = Http.CreateRequest("PUT", $"{config.TestSettings.BaseUrl}{endpoint}")
                    .WithHeader("Accept", "application/json")
                    .WithHeader("Content-Type", "application/json")
                    .WithJsonBody(updateProduct);
                
                request.AddAuthenticationHeaders(config.Authentication);
                
                var response = await Http.Send(httpClient, request);
                return response;
            });
            
            return Response.Ok();
        })
        .WithoutWarmUp()
        .WithLoadSimulations(
            Simulation.Inject(rate: 2, interval: TimeSpan.FromSeconds(1), during: config.TestSettings.TestDuration)
        );
    }
}