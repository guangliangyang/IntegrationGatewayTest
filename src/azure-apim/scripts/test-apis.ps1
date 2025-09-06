# Test script for Azure API Management endpoints
param(
    [Parameter(Mandatory = $true)]
    [string]$ApimGatewayUrl,
    
    [Parameter(Mandatory = $true)]
    [string]$SubscriptionKey,
    
    [Parameter(Mandatory = $false)]
    [string]$AccessToken,
    
    [Parameter(Mandatory = $false)]
    [ValidateSet("1", "2")]
    [string]$ApiVersion = "1"
)

$ErrorActionPreference = "Continue"

Write-Host "🧪 Testing Azure API Management endpoints" -ForegroundColor Green
Write-Host "Gateway URL: $ApimGatewayUrl" -ForegroundColor Cyan
Write-Host "API Version: v$ApiVersion" -ForegroundColor Cyan

# Prepare headers
$headers = @{
    "Ocp-Apim-Subscription-Key" = $SubscriptionKey
    "Content-Type" = "application/json"
}

if ($AccessToken) {
    $headers["Authorization"] = "Bearer $AccessToken"
}

$baseUrl = "$ApimGatewayUrl/gateway/api/v$ApiVersion"

# Test 1: Get Products (List)
Write-Host "`n📋 Test 1: Get Products List" -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod -Uri "$baseUrl/products?page=1&pageSize=10" -Headers $headers -Method Get
    Write-Host "✅ Success: Retrieved products list" -ForegroundColor Green
    Write-Host "   Products count: $($response.products.Count)" -ForegroundColor White
    Write-Host "   Total: $($response.total)" -ForegroundColor White
} catch {
    Write-Host "❌ Failed: $($_.Exception.Message)" -ForegroundColor Red
}

# Test 2: Get Product by ID (using first product from list if available)
Write-Host "`n🔍 Test 2: Get Product by ID" -ForegroundColor Yellow
try {
    if ($response -and $response.products -and $response.products.Count -gt 0) {
        $productId = $response.products[0].id
        $productResponse = Invoke-RestMethod -Uri "$baseUrl/products/$productId" -Headers $headers -Method Get
        Write-Host "✅ Success: Retrieved product $productId" -ForegroundColor Green
        Write-Host "   Product Name: $($productResponse.name)" -ForegroundColor White
        Write-Host "   Price: $($productResponse.price)" -ForegroundColor White
    } else {
        Write-Host "⏭️ Skipped: No products available to test" -ForegroundColor Gray
    }
} catch {
    Write-Host "❌ Failed: $($_.Exception.Message)" -ForegroundColor Red
}

# Test 3: Create Product
Write-Host "`n➕ Test 3: Create Product" -ForegroundColor Yellow
$newProduct = @{
    name = "Test Product - $(Get-Date -Format 'HHmmss')"
    description = "Created via API Management test script"
    price = 99.99
    category = "Testing"
    isActive = $true
} | ConvertTo-Json

try {
    $createResponse = Invoke-RestMethod -Uri "$baseUrl/products" -Headers $headers -Method Post -Body $newProduct
    Write-Host "✅ Success: Created product" -ForegroundColor Green
    Write-Host "   Product ID: $($createResponse.id)" -ForegroundColor White
    Write-Host "   Product Name: $($createResponse.name)" -ForegroundColor White
    $createdProductId = $createResponse.id
} catch {
    Write-Host "❌ Failed: $($_.Exception.Message)" -ForegroundColor Red
    $createdProductId = $null
}

# Test 4: Update Product (if create was successful)
Write-Host "`n✏️ Test 4: Update Product" -ForegroundColor Yellow
if ($createdProductId) {
    $updateProduct = @{
        name = "Updated Test Product - $(Get-Date -Format 'HHmmss')"
        description = "Updated via API Management test script"
        price = 149.99
        category = "Updated Testing"
        isActive = $false
    } | ConvertTo-Json
    
    try {
        $updateResponse = Invoke-RestMethod -Uri "$baseUrl/products/$createdProductId" -Headers $headers -Method Put -Body $updateProduct
        Write-Host "✅ Success: Updated product $createdProductId" -ForegroundColor Green
        Write-Host "   Updated Name: $($updateResponse.name)" -ForegroundColor White
        Write-Host "   Updated Price: $($updateResponse.price)" -ForegroundColor White
    } catch {
        Write-Host "❌ Failed: $($_.Exception.Message)" -ForegroundColor Red
    }
} else {
    Write-Host "⏭️ Skipped: No product was created to update" -ForegroundColor Gray
}

# Test 5: Delete Product (if create was successful)
Write-Host "`n🗑️ Test 5: Delete Product" -ForegroundColor Yellow
if ($createdProductId) {
    try {
        Invoke-RestMethod -Uri "$baseUrl/products/$createdProductId" -Headers $headers -Method Delete
        Write-Host "✅ Success: Deleted product $createdProductId" -ForegroundColor Green
    } catch {
        Write-Host "❌ Failed: $($_.Exception.Message)" -ForegroundColor Red
    }
} else {
    Write-Host "⏭️ Skipped: No product was created to delete" -ForegroundColor Gray
}

# Test 6: Rate Limiting (make rapid requests)
Write-Host "`n⚡ Test 6: Rate Limiting Test" -ForegroundColor Yellow
Write-Host "Making 10 rapid requests to test rate limiting..." -ForegroundColor Cyan
$rateLimitResults = @()

for ($i = 1; $i -le 10; $i++) {
    try {
        $start = Get-Date
        Invoke-RestMethod -Uri "$baseUrl/products?page=1&pageSize=5" -Headers $headers -Method Get | Out-Null
        $end = Get-Date
        $rateLimitResults += @{
            Request = $i
            Success = $true
            ResponseTime = ($end - $start).TotalMilliseconds
        }
        Write-Host "  ✅ Request $i succeeded ($(($end - $start).TotalMilliseconds.ToString('F0'))ms)" -ForegroundColor Green
    } catch {
        $rateLimitResults += @{
            Request = $i
            Success = $false
            Error = $_.Exception.Message
        }
        if ($_.Exception.Message -like "*429*") {
            Write-Host "  ⚠️ Request $i rate limited (429)" -ForegroundColor Yellow
        } else {
            Write-Host "  ❌ Request $i failed: $($_.Exception.Message)" -ForegroundColor Red
        }
    }
    Start-Sleep -Milliseconds 100
}

# Test 7: Invalid Requests (Error Handling)
Write-Host "`n🚫 Test 7: Error Handling" -ForegroundColor Yellow

# Test invalid product ID
try {
    Invoke-RestMethod -Uri "$baseUrl/products/invalid-id-123" -Headers $headers -Method Get
    Write-Host "❌ Unexpected: Should have failed for invalid product ID" -ForegroundColor Red
} catch {
    if ($_.Exception.Message -like "*404*") {
        Write-Host "✅ Success: Correctly returned 404 for invalid product ID" -ForegroundColor Green
    } else {
        Write-Host "⚠️ Warning: Expected 404, got: $($_.Exception.Message)" -ForegroundColor Yellow
    }
}

# Test invalid parameters
try {
    Invoke-RestMethod -Uri "$baseUrl/products?page=0&pageSize=0" -Headers $headers -Method Get
    Write-Host "❌ Unexpected: Should have failed for invalid parameters" -ForegroundColor Red
} catch {
    if ($_.Exception.Message -like "*400*") {
        Write-Host "✅ Success: Correctly returned 400 for invalid parameters" -ForegroundColor Green
    } else {
        Write-Host "⚠️ Warning: Expected 400, got: $($_.Exception.Message)" -ForegroundColor Yellow
    }
}

# Summary
Write-Host "`n📊 Test Summary" -ForegroundColor Cyan
$successfulRequests = ($rateLimitResults | Where-Object { $_.Success }).Count
$failedRequests = ($rateLimitResults | Where-Object { -not $_.Success }).Count
$avgResponseTime = ($rateLimitResults | Where-Object { $_.Success } | Measure-Object -Property ResponseTime -Average).Average

Write-Host "Rate Limiting Test Results:" -ForegroundColor White
Write-Host "  ✅ Successful requests: $successfulRequests/10" -ForegroundColor Green
Write-Host "  ❌ Failed requests: $failedRequests/10" -ForegroundColor Red
if ($avgResponseTime) {
    Write-Host "  ⏱️ Average response time: $($avgResponseTime.ToString('F0'))ms" -ForegroundColor Yellow
}

Write-Host "`n🎉 API Testing completed!" -ForegroundColor Green
Write-Host "Check the Azure portal for additional monitoring data and logs." -ForegroundColor Cyan