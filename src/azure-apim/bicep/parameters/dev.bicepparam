using '../main.bicep'

param environment = 'dev'
param apimName = 'apim-integration-gateway-dev'
param publisherName = 'Integration Gateway Team'
param publisherEmail = 'admin@company.com'
param skuName = 'Developer'
param skuCapacity = 1
param backendServiceUrl = 'https://localhost:7000' // Local development URL
param enableApplicationInsights = true

param tags = {
  Environment: 'dev'
  Project: 'IntegrationGateway'
  ManagedBy: 'Bicep'
  Owner: 'DevTeam'
}