using '../main.bicep'

param environment = 'prod'
param apimName = 'apim-integration-gateway-prod'
param publisherName = 'Integration Gateway Team'
param publisherEmail = 'admin@company.com'
param skuName = 'Premium'
param skuCapacity = 2
param backendServiceUrl = 'https://integration-gateway-prod.azurewebsites.net'
param enableApplicationInsights = true

param tags = {
  Environment: 'prod'
  Project: 'IntegrationGateway'
  ManagedBy: 'Bicep'
  Owner: 'DevTeam'
}