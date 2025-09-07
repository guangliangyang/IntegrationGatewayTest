#!/usr/bin/env pwsh
# Integration Gateway Performance Testing Script
# Usage: ./run-performance-tests.ps1 [TestMode] [Options]

param(
    [Parameter(Mandatory = $false)]
    [ValidateSet("smoke", "light", "medium", "heavy", "stress", "cache", "mixed")]
    [string]$TestMode = "light",
    
    [Parameter(Mandatory = $false)]
    [string]$BaseUrl = "https://localhost:7000",
    
    [Parameter(Mandatory = $false)]
    [string]$ReportPath = "./Reports",
    
    [Parameter(Mandatory = $false)]
    [switch]$BuildFirst = $true,
    
    [Parameter(Mandatory = $false)]
    [switch]$OpenReports = $false,
    
    [Parameter(Mandatory = $false)]
    [switch]$Verbose = $false
)

$ErrorActionPreference = "Stop"

# 颜色输出函数
function Write-ColorOutput {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Message,
        [Parameter(Mandatory = $false)]
        [string]$Color = "White"
    )
    Write-Host $Message -ForegroundColor $Color
}

function Write-Header {
    param([string]$Title)
    Write-ColorOutput "`n🚀 $Title" "Green"
    Write-ColorOutput ("=" * (4 + $Title.Length)) "Green"
}

function Write-Info {
    param([string]$Message)
    Write-ColorOutput "💡 $Message" "Cyan"
}

function Write-Success {
    param([string]$Message)
    Write-ColorOutput "✅ $Message" "Green"
}

function Write-Warning {
    param([string]$Message)
    Write-ColorOutput "⚠️ $Message" "Yellow"
}

function Write-Error {
    param([string]$Message)
    Write-ColorOutput "❌ $Message" "Red"
}

# 主脚本开始
try {
    Write-Header "Integration Gateway Performance Tests"
    
    # 显示测试配置
    Write-Info "Test Configuration:"
    Write-Host "  📊 Test Mode: $TestMode" -ForegroundColor White
    Write-Host "  🌐 Base URL: $BaseUrl" -ForegroundColor White  
    Write-Host "  📁 Report Path: $ReportPath" -ForegroundColor White
    Write-Host "  🔨 Build First: $BuildFirst" -ForegroundColor White
    
    # 设置项目路径
    $projectPath = Join-Path $PSScriptRoot ".."
    $projectFile = Join-Path $projectPath "Performance.Tests.csproj"
    
    if (!(Test-Path $projectFile)) {
        Write-Error "Performance test project not found at: $projectFile"
        exit 1
    }
    
    # 验证目标API是否可访问
    Write-Info "Checking API availability..."
    try {
        $healthCheck = Invoke-WebRequest -Uri "$BaseUrl/health" -Method GET -TimeoutSec 10 -UseBasicParsing
        Write-Success "API is accessible (Status: $($healthCheck.StatusCode))"
    }
    catch {
        Write-Warning "Could not reach API health endpoint. Testing may fail if API is not running."
        Write-Host "  Error: $($_.Exception.Message)" -ForegroundColor Gray
    }
    
    # 构建项目
    if ($BuildFirst) {
        Write-Info "Building performance test project..."
        $buildResult = dotnet build $projectFile --configuration Release --verbosity minimal
        if ($LASTEXITCODE -ne 0) {
            Write-Error "Build failed"
            exit 1
        }
        Write-Success "Build completed successfully"
    }
    
    # 创建报告目录
    $fullReportPath = Join-Path $projectPath $ReportPath
    if (!(Test-Path $fullReportPath)) {
        New-Item -ItemType Directory -Path $fullReportPath -Force | Out-Null
        Write-Info "Created report directory: $fullReportPath"
    }
    
    # 更新配置文件中的BaseUrl
    $configFile = Join-Path $projectPath "Config/test-config.json"
    if (Test-Path $configFile) {
        Write-Info "Updating test configuration..."
        $config = Get-Content $configFile -Raw | ConvertFrom-Json
        $config.testSettings.baseUrl = $BaseUrl
        $config.testSettings.reportOutputPath = $ReportPath
        $config | ConvertTo-Json -Depth 10 | Set-Content $configFile
        Write-Success "Configuration updated"
    }
    
    # 运行性能测试
    Write-Header "Running Performance Tests"
    
    $testModeDescription = switch ($TestMode) {
        "smoke" { "Smoke Test - Basic functionality validation" }
        "light" { "Light Load - 10 concurrent users for 5 minutes" }
        "medium" { "Medium Load - 50 concurrent users for 10 minutes" }
        "heavy" { "Heavy Load - 100 concurrent users for 15 minutes" }
        "stress" { "Stress Test - Progressive load increase to find limits" }
        "cache" { "Cache Performance - Testing caching effectiveness" }
        "mixed" { "Mixed Workload - Realistic 80% read / 20% write scenario" }
        default { "Light Load Test" }
    }
    
    Write-Info "Test Description: $testModeDescription"
    Write-Info "Starting test execution..."
    
    # 切换到项目目录并运行测试
    Push-Location $projectPath
    
    try {
        $runArgs = @($TestMode)
        if ($Verbose) {
            $runArgs += "--verbose"
        }
        
        $startTime = Get-Date
        dotnet run --configuration Release -- @runArgs
        $endTime = Get-Date
        $duration = $endTime - $startTime
        
        if ($LASTEXITCODE -eq 0) {
            Write-Success "Performance test completed successfully!"
            Write-Info "Test Duration: $($duration.ToString('hh\:mm\:ss'))"
            
            # 查找生成的报告文件
            $reportFiles = Get-ChildItem -Path $ReportPath -Filter "*.html" | Sort-Object LastWriteTime -Descending
            
            if ($reportFiles.Count -gt 0) {
                $latestReport = $reportFiles[0]
                Write-Success "Latest report: $($latestReport.Name)"
                
                if ($OpenReports) {
                    Write-Info "Opening performance report..."
                    if ($IsWindows) {
                        Start-Process $latestReport.FullName
                    } elseif ($IsMacOS) {
                        Start-Process "open" -ArgumentList $latestReport.FullName
                    } elseif ($IsLinux) {
                        Start-Process "xdg-open" -ArgumentList $latestReport.FullName
                    }
                }
            }
            
            # 显示报告摘要
            Write-Header "Test Results Summary"
            Write-Info "Check the following reports for detailed results:"
            Get-ChildItem -Path $ReportPath -Filter "*performance-test-$TestMode*" | 
                ForEach-Object { Write-Host "  📊 $($_.Name)" -ForegroundColor White }
                
        } else {
            Write-Error "Performance test failed with exit code: $LASTEXITCODE"
            exit $LASTEXITCODE
        }
    }
    finally {
        Pop-Location
    }
    
    Write-Header "Performance Testing Complete"
    Write-Success "All tests have been completed successfully!"
    Write-Info "Reports are available in: $fullReportPath"
}
catch {
    Write-Error "An error occurred during performance testing:"
    Write-Host $_.Exception.Message -ForegroundColor Red
    Write-Host $_.ScriptStackTrace -ForegroundColor Gray
    exit 1
}

# 显示可用的测试模式
Write-Host "`n📋 Available Test Modes:" -ForegroundColor Cyan
Write-Host "  smoke   - Basic functionality validation (1 min)" -ForegroundColor Gray
Write-Host "  light   - Light load testing (5 min)" -ForegroundColor Gray  
Write-Host "  medium  - Medium load testing (10 min)" -ForegroundColor Gray
Write-Host "  heavy   - Heavy load testing (15 min)" -ForegroundColor Gray
Write-Host "  stress  - Stress testing with progressive load (20 min)" -ForegroundColor Gray
Write-Host "  cache   - Cache performance testing (10 min)" -ForegroundColor Gray
Write-Host "  mixed   - Mixed workload testing (15 min)" -ForegroundColor Gray

Write-Host "`n📖 Usage Examples:" -ForegroundColor Cyan
Write-Host "  ./run-performance-tests.ps1 smoke" -ForegroundColor Gray
Write-Host "  ./run-performance-tests.ps1 medium -BaseUrl 'https://your-api.com'" -ForegroundColor Gray
Write-Host "  ./run-performance-tests.ps1 stress -OpenReports" -ForegroundColor Gray