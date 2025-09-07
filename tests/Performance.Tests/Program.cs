using NBomber.CSharp;
using NBomber.Contracts;
using Performance.Tests.Utils;
using Performance.Tests.Scenarios;
using Serilog;

namespace Performance.Tests;

class Program
{
    static void Main(string[] args)
    {
        // 配置日志
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .CreateLogger();

        try
        {
            Console.WriteLine("🚀 Integration Gateway Performance Tests");
            Console.WriteLine("=======================================");
            
            // 加载配置
            var config = TestConfiguration.Load();
            Console.WriteLine($"📡 Target URL: {config.TestSettings.BaseUrl}");
            Console.WriteLine($"⏱️ Test Duration: {config.TestSettings.TestDuration}");
            
            // 选择测试模式
            var testMode = GetTestModeFromArgs(args);
            Console.WriteLine($"🎯 Test Mode: {testMode}");
            
            // 运行测试
            RunPerformanceTest(config, testMode);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Performance test failed");
            Environment.Exit(1);
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    private static string GetTestModeFromArgs(string[] args)
    {
        if (args.Length > 0)
        {
            return args[0].ToLower() switch
            {
                "smoke" => "smoke",
                "light" => "light",
                "medium" => "medium", 
                "heavy" => "heavy",
                "stress" => "stress",
                "cache" => "cache",
                "mixed" => "mixed",
                _ => "light"
            };
        }
        
        return "light"; // 默认轻负载测试
    }

    private static void RunPerformanceTest(TestConfiguration config, string testMode)
    {
        var scenarios = new List<Scenario>();
        
        switch (testMode)
        {
            case "smoke":
                Console.WriteLine("🔍 Running Smoke Test...");
                scenarios.AddRange(CreateSmokeTestScenarios(config));
                break;
                
            case "light":
                Console.WriteLine("💡 Running Light Load Test...");
                scenarios.AddRange(CreateLightLoadScenarios(config));
                break;
                
            case "medium":
                Console.WriteLine("⚖️ Running Medium Load Test...");
                scenarios.AddRange(CreateMediumLoadScenarios(config));
                break;
                
            case "heavy":
                Console.WriteLine("💪 Running Heavy Load Test...");
                scenarios.AddRange(CreateHeavyLoadScenarios(config));
                break;
                
            case "stress":
                Console.WriteLine("🔥 Running Stress Test...");
                scenarios.AddRange(CreateStressTestScenarios(config));
                break;
                
            case "cache":
                Console.WriteLine("⚡ Running Cache Performance Test...");
                scenarios.AddRange(CreateCacheTestScenarios(config));
                break;
                
            case "mixed":
                Console.WriteLine("🎭 Running Mixed Workload Test...");
                scenarios.AddRange(CreateMixedWorkloadScenarios(config));
                break;
                
            default:
                Console.WriteLine("💡 Running Default Light Load Test...");
                scenarios.AddRange(CreateLightLoadScenarios(config));
                break;
        }
        
        // 运行NBomber测试
        var stats = NBomberRunner
            .RegisterScenarios(scenarios.ToArray())
            .WithReportFolder(config.TestSettings.ReportOutputPath)
            .WithReportFileName($"performance-test-{testMode}-{DateTime.Now:yyyy-MM-dd-HH-mm}")
            .WithReportFormats(ReportFormat.Html, ReportFormat.Csv, ReportFormat.Txt)
            .Run();
            
        Console.WriteLine("✅ Performance test completed!");
        Console.WriteLine($"📊 Reports saved to: {config.TestSettings.ReportOutputPath}");
    }

    private static IEnumerable<Scenario> CreateSmokeTestScenarios(TestConfiguration config)
    {
        yield return BaseScenarios.CreateGetProductsScenario(config)
            .WithLoadSimulations(Simulation.InjectPerSec(rate: 1, during: TimeSpan.FromMinutes(1)));
            
        yield return BaseScenarios.CreateGetProductByIdScenario(config)
            .WithLoadSimulations(Simulation.InjectPerSec(rate: 1, during: TimeSpan.FromMinutes(1)));
    }

    private static IEnumerable<Scenario> CreateLightLoadScenarios(TestConfiguration config)
    {
        yield return BaseScenarios.CreateGetProductsScenario(config)
            .WithLoadSimulations(Simulation.KeepConstant(copies: 5, during: TimeSpan.FromMinutes(5)));
            
        yield return BaseScenarios.CreateGetProductByIdScenario(config)
            .WithLoadSimulations(Simulation.KeepConstant(copies: 3, during: TimeSpan.FromMinutes(5)));
            
        yield return BaseScenarios.CreatePostProductScenario(config)
            .WithLoadSimulations(Simulation.KeepConstant(copies: 2, during: TimeSpan.FromMinutes(5)));
    }

    private static IEnumerable<Scenario> CreateMediumLoadScenarios(TestConfiguration config)
    {
        yield return BaseScenarios.CreateGetProductsScenario(config)
            .WithLoadSimulations(Simulation.KeepConstant(copies: 25, during: TimeSpan.FromMinutes(10)));
            
        yield return BaseScenarios.CreateGetProductByIdScenario(config)
            .WithLoadSimulations(Simulation.KeepConstant(copies: 15, during: TimeSpan.FromMinutes(10)));
            
        yield return BaseScenarios.CreatePostProductScenario(config)
            .WithLoadSimulations(Simulation.KeepConstant(copies: 5, during: TimeSpan.FromMinutes(10)));
            
        yield return BaseScenarios.CreatePutProductScenario(config)
            .WithLoadSimulations(Simulation.KeepConstant(copies: 5, during: TimeSpan.FromMinutes(10)));
    }

    private static IEnumerable<Scenario> CreateHeavyLoadScenarios(TestConfiguration config)
    {
        yield return BaseScenarios.CreateGetProductsScenario(config)
            .WithLoadSimulations(Simulation.KeepConstant(copies: 50, during: TimeSpan.FromMinutes(15)));
            
        yield return BaseScenarios.CreateGetProductByIdScenario(config)
            .WithLoadSimulations(Simulation.KeepConstant(copies: 30, during: TimeSpan.FromMinutes(15)));
            
        yield return BaseScenarios.CreatePostProductScenario(config)
            .WithLoadSimulations(Simulation.KeepConstant(copies: 10, during: TimeSpan.FromMinutes(15)));
            
        yield return BaseScenarios.CreatePutProductScenario(config)
            .WithLoadSimulations(Simulation.KeepConstant(copies: 10, during: TimeSpan.FromMinutes(15)));
    }

    private static IEnumerable<Scenario> CreateStressTestScenarios(TestConfiguration config)
    {
        // 逐步增加负载的压力测试
        yield return BaseScenarios.CreateGetProductsScenario(config)
            .WithLoadSimulations(
                Simulation.KeepConstant(copies: 10, during: TimeSpan.FromMinutes(2)),
                Simulation.KeepConstant(copies: 50, during: TimeSpan.FromMinutes(5)),
                Simulation.KeepConstant(copies: 100, during: TimeSpan.FromMinutes(8)),
                Simulation.KeepConstant(copies: 150, during: TimeSpan.FromMinutes(5))
            );
    }

    private static IEnumerable<Scenario> CreateCacheTestScenarios(TestConfiguration config)
    {
        yield return ProductsApiScenarios.CreateCacheTestScenario(config)
            .WithLoadSimulations(Simulation.KeepConstant(copies: 10, during: TimeSpan.FromMinutes(10)));
            
        yield return ProductsApiScenarios.CreateSingleProductCacheTestScenario(config)
            .WithLoadSimulations(Simulation.KeepConstant(copies: 8, during: TimeSpan.FromMinutes(10)));
    }

    private static IEnumerable<Scenario> CreateMixedWorkloadScenarios(TestConfiguration config)
    {
        yield return ProductsApiScenarios.CreateMixedWorkloadScenario(config)
            .WithLoadSimulations(Simulation.KeepConstant(copies: 50, during: TimeSpan.FromMinutes(15)));
    }
}