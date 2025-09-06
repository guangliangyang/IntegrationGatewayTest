using '../main.bicep'

param environment = 'staging'
param apimName = 'apim-integration-gateway-staging'
param publisherName = 'Integration Gateway Team'
param publisherEmail = 'admin@company.com'
param skuName = 'Standard'
param skuCapacity = 1
param backendServiceUrl = 'https://integration-gateway-staging.azurewebsites.net'
param enableApplicationInsights = true

param tags = {
  Environment: 'staging'
  Project: 'IntegrationGateway'
  ManagedBy: 'Bicep'
  Owner: 'DevTeam'
}