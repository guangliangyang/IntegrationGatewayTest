@description('Main template for Azure API Management deployment')
param environment string = 'dev'
param location string = resourceGroup().location
param apimName string
param publisherName string = 'Integration Gateway'
param publisherEmail string

@description('SKU tier for API Management service')
@allowed(['Developer', 'Standard', 'Premium'])
param skuName string = 'Developer'

@description('SKU capacity')
param skuCapacity int = 1

@description('Backend service URL')
param backendServiceUrl string

@description('Enable Application Insights integration')
param enableApplicationInsights bool = true

@description('Tags for all resources')
param tags object = {
  Environment: environment
  Project: 'IntegrationGateway'
  ManagedBy: 'Bicep'
}

// Deploy API Management service
module apimModule 'apim.bicep' = {
  name: 'apim-deployment'
  params: {
    apimName: apimName
    location: location
    publisherName: publisherName
    publisherEmail: publisherEmail
    skuName: skuName
    skuCapacity: skuCapacity
    backendServiceUrl: backendServiceUrl
    tags: tags
  }
}

// Deploy monitoring resources
module monitoringModule 'monitoring.bicep' = if (enableApplicationInsights) {
  name: 'monitoring-deployment'
  params: {
    location: location
    apimName: apimName
    tags: tags
  }
  dependsOn: [
    apimModule
  ]
}

// Outputs
output apimServiceUrl string = apimModule.outputs.apimServiceUrl
output apimManagementUrl string = apimModule.outputs.apimManagementUrl
output apimGatewayUrl string = apimModule.outputs.apimGatewayUrl
output applicationInsightsInstrumentationKey string = enableApplicationInsights ? monitoringModule.outputs.instrumentationKey : ''
output resourceGroupName string = resourceGroup().name