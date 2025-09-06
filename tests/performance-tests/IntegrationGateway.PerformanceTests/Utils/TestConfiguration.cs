using Microsoft.Extensions.Configuration;

namespace IntegrationGateway.PerformanceTests.Utils;

public class TestConfiguration
{
    public TestSettings TestSettings { get; set; } = new();
    public ApiEndpoints ApiEndpoints { get; set; } = new();
    public TestData TestData { get; set; } = new();
    public AuthenticationSettings Authentication { get; set; } = new();

    public static TestConfiguration Load(string configPath = "Config/test-config.json")
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile(configPath, optional: false, reloadOnChange: true)
            .Build();

        var config = new TestConfiguration();
        configuration.Bind(config);
        return config;
    }
}

public class TestSettings
{
    public string BaseUrl { get; set; } = "https://localhost:7000";
    public TimeSpan TestDuration { get; set; } = TimeSpan.FromMinutes(5);
    public string ReportOutputPath { get; set; } = "./Reports";
    public bool EnableDetailedReports { get; set; } = true;
    public bool EnableHtmlReport { get; set; } = true;
    public bool EnableCsvReport { get; set; } = true;
}

public class ApiEndpoints
{
    public ProductEndpoints Products { get; set; } = new();
    public ProductEndpoints ProductsV2 { get; set; } = new();
}

public class ProductEndpoints
{
    public string List { get; set; } = "/api/v1/products";
    public string GetById { get; set; } = "/api/v1/products/{id}";
    public string Create { get; set; } = "/api/v1/products";
    public string Update { get; set; } = "/api/v1/products/{id}";
    public string Delete { get; set; } = "/api/v1/products/{id}";
}

public class TestData
{
    public string[] SampleProductIds { get; set; } = Array.Empty<string>();
    public CreateProductTemplate CreateProductTemplate { get; set; } = new();
}

public class CreateProductTemplate
{
    public string Name { get; set; } = "Test Product {0}";
    public string Description { get; set; } = "Test product";
    public decimal Price { get; set; } = 99.99m;
    public string Category { get; set; } = "Testing";
    public bool IsActive { get; set; } = true;
}

public class AuthenticationSettings
{
    public bool EnableAuth { get; set; } = false;
    public string BearerToken { get; set; } = string.Empty;
    public string SubscriptionKey { get; set; } = string.Empty;
}