# Azure API Management Deployment Script
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet("dev", "staging", "prod")]
    [string]$Environment,
    
    [Parameter(Mandatory = $true)]
    [string]$ResourceGroupName,
    
    [Parameter(Mandatory = $false)]
    [string]$SubscriptionId,
    
    [Parameter(Mandatory = $false)]
    [string]$Location = "East US 2"
)

# Set error action preference
$ErrorActionPreference = "Stop"

Write-Host "🚀 Starting Azure API Management deployment for environment: $Environment" -ForegroundColor Green

# Set subscription if provided
if ($SubscriptionId) {
    Write-Host "📋 Setting subscription context to: $SubscriptionId" -ForegroundColor Yellow
    az account set --subscription $SubscriptionId
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to set subscription context"
    }
}

# Get current subscription info
$currentSubscription = az account show --query "{subscriptionId: id, name: name}" --output json | ConvertFrom-Json
Write-Host "📋 Current subscription: $($currentSubscription.name) ($($currentSubscription.subscriptionId))" -ForegroundColor Cyan

# Create resource group if it doesn't exist
Write-Host "🏗️ Ensuring resource group exists: $ResourceGroupName" -ForegroundColor Yellow
az group create --name $ResourceGroupName --location $Location --output none
if ($LASTEXITCODE -ne 0) {
    throw "Failed to create or verify resource group"
}

# Validate Bicep template
Write-Host "✅ Validating Bicep template..." -ForegroundColor Yellow
$bicepFile = Join-Path $PSScriptRoot ".." "bicep" "main.bicep"
$parameterFile = Join-Path $PSScriptRoot ".." "bicep" "parameters" "$Environment.bicepparam"

if (-not (Test-Path $bicepFile)) {
    throw "Bicep template not found: $bicepFile"
}

if (-not (Test-Path $parameterFile)) {
    throw "Parameter file not found: $parameterFile"
}

az deployment group validate `
    --resource-group $ResourceGroupName `
    --template-file $bicepFile `
    --parameters $parameterFile `
    --output none

if ($LASTEXITCODE -ne 0) {
    throw "Bicep template validation failed"
}

Write-Host "✅ Template validation successful" -ForegroundColor Green

# Deploy infrastructure
Write-Host "🚀 Deploying Azure API Management infrastructure..." -ForegroundColor Yellow
$deploymentName = "apim-deployment-$(Get-Date -Format 'yyyyMMdd-HHmmss')"

$deploymentResult = az deployment group create `
    --resource-group $ResourceGroupName `
    --template-file $bicepFile `
    --parameters $parameterFile `
    --name $deploymentName `
    --query "{apimServiceUrl: properties.outputs.apimServiceUrl.value, apimManagementUrl: properties.outputs.apimManagementUrl.value, apimGatewayUrl: properties.outputs.apimGatewayUrl.value}" `
    --output json

if ($LASTEXITCODE -ne 0) {
    throw "Deployment failed"
}

$outputs = $deploymentResult | ConvertFrom-Json
Write-Host "✅ Deployment completed successfully!" -ForegroundColor Green

# Display deployment outputs
Write-Host "`n📊 Deployment Outputs:" -ForegroundColor Cyan
Write-Host "  🌐 APIM Service URL:    $($outputs.apimServiceUrl)" -ForegroundColor White
Write-Host "  🔧 APIM Management URL: $($outputs.apimManagementUrl)" -ForegroundColor White
Write-Host "  🚪 APIM Gateway URL:    $($outputs.apimGatewayUrl)" -ForegroundColor White

# Check deployment status
Write-Host "`n🔍 Checking API Management service status..." -ForegroundColor Yellow
$apimName = "apim-integration-gateway-$Environment"

$apimStatus = az apim show --name $apimName --resource-group $ResourceGroupName --query "provisioningState" --output tsv
Write-Host "  📊 APIM Status: $apimStatus" -ForegroundColor $(if ($apimStatus -eq "Succeeded") { "Green" } else { "Yellow" })

if ($apimStatus -eq "Succeeded") {
    Write-Host "`n🎉 API Management deployment completed successfully!" -ForegroundColor Green
    Write-Host "   You can now configure your APIs and policies through the Azure portal or CLI." -ForegroundColor Cyan
} else {
    Write-Host "`n⏳ API Management is still provisioning. This can take 30-45 minutes." -ForegroundColor Yellow
    Write-Host "   Check the Azure portal for provisioning progress." -ForegroundColor Cyan
}

Write-Host "`n🔗 Next Steps:" -ForegroundColor Yellow
Write-Host "  1. Configure your backend Integration Gateway service URL" -ForegroundColor White
Write-Host "  2. Test the API endpoints through the APIM gateway" -ForegroundColor White
Write-Host "  3. Configure additional policies as needed" -ForegroundColor White
Write-Host "  4. Set up monitoring and alerting" -ForegroundColor White