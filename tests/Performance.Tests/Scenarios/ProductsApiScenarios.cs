using NBomber.CSharp;
using NBomber.Contracts;
using NBomber.Http.CSharp;
using Performance.Tests.Utils;

namespace Performance.Tests.Scenarios;

public static class ProductsApiScenarios
{
    public static ScenarioProps CreateCacheTestScenario(TestConfiguration config, string scenarioName = "get_products_cached")
    {
        using var httpClient = new HttpClient();
        
        return Scenario.Create(scenarioName, async context =>
        {
            var step = await Step.Run("get_products_cached", context, async () =>
            {
                var request = Http.CreateRequest("GET", 
                    $"{config.TestSettings.BaseUrl}{config.ApiEndpoints.Products.List}?page=1&pageSize=10")
                    .WithHeader("Accept", "application/json");
                
                request.AddAuthenticationHeaders(config.Authentication);
                
                var response = await Http.Send(httpClient, request);
                return response;
            });
            
            return Response.Ok();
        })
        .WithoutWarmUp()
        .WithLoadSimulations(
            Simulation.Inject(rate: 20, interval: TimeSpan.FromSeconds(1), during: config.TestSettings.TestDuration)
        );
    }
    
    public static ScenarioProps CreateSingleProductCacheTestScenario(TestConfiguration config, string scenarioName = "get_product_by_id_cached")
    {
        using var httpClient = new HttpClient();
        var fixedProductId = config.TestData.SampleProductIds.FirstOrDefault() ?? "product-001";
        
        return Scenario.Create(scenarioName, async context =>
        {
            var step = await Step.Run("get_product_by_id_cached", context, async () =>
            {
                var endpoint = config.ApiEndpoints.Products.GetById.Replace("{id}", fixedProductId);
                
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
            Simulation.Inject(rate: 15, interval: TimeSpan.FromSeconds(1), during: config.TestSettings.TestDuration)
        );
    }
    
    public static ScenarioProps CreateV2ApiScenario(TestConfiguration config, string scenarioName = "get_products_v2")
    {
        using var httpClient = new HttpClient();
        
        return Scenario.Create(scenarioName, async context =>
        {
            var step = await Step.Run("get_products_v2", context, async () =>
            {
                var request = Http.CreateRequest("GET", 
                    $"{config.TestSettings.BaseUrl}{config.ApiEndpoints.ProductsV2.List}?page=1&pageSize=20")
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
    
    public static ScenarioProps CreateMixedWorkloadScenario(TestConfiguration config, string scenarioName = "mixed_workload")
    {
        using var httpClient = new HttpClient();
        
        return Scenario.Create(scenarioName, async context =>
        {
            var random = new Random();
            var operation = random.NextDouble();
            
            if (operation < 0.4) // 40% - GET products list
            {
                var step = await Step.Run("get_products_list", context, async () =>
                {
                    var request = Http.CreateRequest("GET", 
                        $"{config.TestSettings.BaseUrl}{config.ApiEndpoints.Products.List}?page={random.Next(1, 5)}&pageSize=20")
                        .WithHeader("Accept", "application/json");
                    
                    request.AddAuthenticationHeaders(config.Authentication);
                    
                    var response = await Http.Send(httpClient, request);
                    return response;
                });
            }
            else if (operation < 0.8) // 40% - GET product by ID
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
            }
            else if (operation < 0.9) // 10% - CREATE product
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
            }
            else // 10% - UPDATE product
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
            }
            
            return Response.Ok();
        })
        .WithoutWarmUp()
        .WithLoadSimulations(
            Simulation.Inject(rate: 25, interval: TimeSpan.FromSeconds(1), during: config.TestSettings.TestDuration)
        );
    }
}