using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;

namespace Integration.Tests.Integration.Base;

/// <summary>
/// Base class for integration tests with WebApplicationFactory setup
/// </summary>
public class IntegrationTestBase : IClassFixture<IntegrationTestWebApplicationFactory>
{
    protected readonly HttpClient _client;
    protected readonly IntegrationTestWebApplicationFactory _factory;
    protected readonly MockHttpMessageHandler _mockHttpHandler;

    protected IntegrationTestBase(IntegrationTestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _mockHttpHandler = factory.MockHttpHandler;
        
        // Set up authentication for tests
        SetupAuthenticationAsync().Wait();
    }
    
    private async Task SetupAuthenticationAsync()
    {
        try
        {
            // Generate JWT token directly for testing
            var token = GenerateJwtToken("testuser");
            if (!string.IsNullOrEmpty(token))
            {
                _client.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }
        }
        catch (Exception ex)
        {
            // If authentication setup fails, tests will need to handle auth explicitly
            throw new InvalidOperationException($"Failed to setup test authentication: {ex.Message}", ex);
        }
        
        await Task.CompletedTask;
    }

    /// <summary>
    /// Generate JWT token for testing - mirrors DevAuthController.GetToken logic
    /// </summary>
    private string GenerateJwtToken(string username = "testuser")
    {
        // Read JWT configuration from the same sources as the application
        var secretKey = Environment.GetEnvironmentVariable("Jwt__SecretKey") ?? 
                       throw new InvalidOperationException("JWT SecretKey not found in environment variables");
        var issuer = _factory.Services.GetRequiredService<IConfiguration>()["Jwt:Issuer"];
        var audience = _factory.Services.GetRequiredService<IConfiguration>()["Jwt:Audience"];

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, username),
            new Claim(ClaimTypes.NameIdentifier, "ef72e7f9-e03c-4796-b98f-a66093fd6402"),
            new Claim("sub", username)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(60),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    protected StringContent CreateJsonContent(object obj)
    {
        var json = JsonSerializer.Serialize(obj, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        return new StringContent(json, Encoding.UTF8, "application/json");
    }

    protected async Task<T?> DeserializeResponse<T>(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(content, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        });
    }
}

/// <summary>
/// Custom WebApplicationFactory for integration tests
/// </summary>
public class IntegrationTestWebApplicationFactory : WebApplicationFactory<Program>
{
    public MockHttpMessageHandler MockHttpHandler { get; } = new();

    private void LoadEnvironmentVariables()
    {
        // Try multiple potential locations for .env file
        var potentialPaths = new[]
        {
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".env")),
            Path.Combine(Directory.GetCurrentDirectory(), ".env"),
            Path.Combine(AppContext.BaseDirectory, ".env"),
        };

        string envPath = null;
        foreach (var path in potentialPaths)
        {
            if (File.Exists(path))
            {
                envPath = path;
                break;
            }
        }

        if (envPath != null)
        {
            foreach (var line in File.ReadAllLines(envPath))
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
                    continue;

                var parts = line.Split('=', 2);
                if (parts.Length == 2)
                {
                    var key = parts[0].Trim();
                    var value = parts[1].Trim().Trim('"');
                    Environment.SetEnvironmentVariable(key, value);
                }
            }
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Load .env file for test environment variables
        LoadEnvironmentVariables();
        
        // Set environment variable BEFORE any configuration loading
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Test");
        
        // Set test environment
        builder.UseEnvironment("Test");

        builder.ConfigureAppConfiguration((context, config) =>
        {
            // Clear existing configuration sources to avoid conflicts
            config.Sources.Clear();
            
            // Add test configuration with validated JWT settings from tests directory
            var testConfigPath = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.Test.json");
            config.AddJsonFile(testConfigPath, optional: false, reloadOnChange: false);
            
            // Add environment variables (will override appsettings values)
            config.AddEnvironmentVariables();
            
            // Environment variables are loaded and will override appsettings values
        });

        builder.ConfigureServices(services =>
        {
            // Replace HttpClientFactory with our mocked version
            services.RemoveAll<IHttpClientFactory>();
            services.AddSingleton<IHttpClientFactory>(_ => new MockHttpClientFactory(MockHttpHandler));

            // Minimize logging noise in tests
            services.AddLogging();
        });
    }
}

/// <summary>
/// Mock HttpClientFactory that returns HttpClient with our MockHttpMessageHandler
/// </summary>
public class MockHttpClientFactory : IHttpClientFactory
{
    private readonly MockHttpMessageHandler _handler;

    public MockHttpClientFactory(MockHttpMessageHandler handler)
    {
        _handler = handler;
    }

    public HttpClient CreateClient(string name)
    {
        var client = new HttpClient(_handler, disposeHandler: false);
        
        // Configure based on client name
        switch (name)
        {
            case "ErpClient":
                client.BaseAddress = new Uri("http://localhost:5051");
                break;
            case "WarehouseClient":
                client.BaseAddress = new Uri("http://localhost:5052");
                break;
        }

        return client;
    }
}

