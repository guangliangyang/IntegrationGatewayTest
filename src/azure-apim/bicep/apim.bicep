@description('Azure API Management service deployment')
param apimName string
param location string = resourceGroup().location
param publisherName string
param publisherEmail string
param skuName string = 'Developer'
param skuCapacity int = 1
param backendServiceUrl string
param tags object = {}

// API Management service
resource apim 'Microsoft.ApiManagement/service@2023-05-01-preview' = {
  name: apimName
  location: location
  tags: tags
  sku: {
    name: skuName
    capacity: skuCapacity
  }
  properties: {
    publisherEmail: publisherEmail
    publisherName: publisherName
    notificationSenderEmail: publisherEmail
    hostnameConfigurations: [
      {
        type: 'Proxy'
        hostName: '${apimName}.azure-api.net'
        negotiateClientCertificate: false
        defaultSslBinding: true
        certificateSource: 'BuiltIn'
      }
    ]
    customProperties: {
      'Microsoft.WindowsAzure.ApiManagement.Gateway.Security.Protocols.Tls10': 'False'
      'Microsoft.WindowsAzure.ApiManagement.Gateway.Security.Protocols.Tls11': 'False'
      'Microsoft.WindowsAzure.ApiManagement.Gateway.Security.Protocols.Ssl30': 'False'
      'Microsoft.WindowsAzure.ApiManagement.Gateway.Security.Ciphers.TripleDes168': 'False'
      'Microsoft.WindowsAzure.ApiManagement.Gateway.Security.Backend.Protocols.Tls10': 'False'
      'Microsoft.WindowsAzure.ApiManagement.Gateway.Security.Backend.Protocols.Tls11': 'False'
      'Microsoft.WindowsAzure.ApiManagement.Gateway.Security.Backend.Protocols.Ssl30': 'False'
    }
    virtualNetworkType: 'None'
  }
  identity: {
    type: 'SystemAssigned'
  }
}

// Backend service for Integration Gateway
resource backendService 'Microsoft.ApiManagement/service/backends@2023-05-01-preview' = {
  parent: apim
  name: 'integration-gateway-backend'
  properties: {
    description: 'Integration Gateway Backend Service'
    url: backendServiceUrl
    protocol: 'http'
    circuitBreaker: {
      rules: [
        {
          failureCondition: {
            count: 5
            errorReasons: [
              'Server errors'
            ]
            interval: 'PT1M'
          }
          name: 'IntegrationGatewayCircuitBreaker'
          tripDuration: 'PT1M'
        }
      ]
    }
  }
}

// API definition for Integration Gateway
resource api 'Microsoft.ApiManagement/service/apis@2023-05-01-preview' = {
  parent: apim
  name: 'integration-gateway-api'
  properties: {
    displayName: 'Integration Gateway API'
    apiRevision: '1'
    description: 'Integration Gateway API for ERP and Warehouse systems'
    subscriptionRequired: true
    serviceUrl: backendServiceUrl
    path: 'gateway'
    protocols: [
      'https'
    ]
    authenticationSettings: {
      oAuth2AuthenticationSettings: []
    }
    subscriptionKeyParameterNames: {
      header: 'Ocp-Apim-Subscription-Key'
      query: 'subscription-key'
    }
    apiType: 'http'
    isCurrent: true
  }
}

// Product for Integration Gateway API
resource product 'Microsoft.ApiManagement/service/products@2023-05-01-preview' = {
  parent: apim
  name: 'integration-gateway-product'
  properties: {
    displayName: 'Integration Gateway Product'
    description: 'Product for Integration Gateway APIs with rate limiting and caching'
    subscriptionRequired: true
    approvalRequired: false
    subscriptionsLimit: 100
    state: 'published'
  }
}

// Link API to Product
resource productApi 'Microsoft.ApiManagement/service/products/apis@2023-05-01-preview' = {
  parent: product
  name: 'integration-gateway-api'
  dependsOn: [
    api
  ]
}

// Global policy
resource globalPolicy 'Microsoft.ApiManagement/service/policies@2023-05-01-preview' = {
  parent: apim
  name: 'policy'
  properties: {
    value: loadTextContent('../policies/global/global-policy.xml')
    format: 'xml'
  }
}

// Product policy
resource productPolicy 'Microsoft.ApiManagement/service/products/policies@2023-05-01-preview' = {
  parent: product
  name: 'policy'
  properties: {
    value: loadTextContent('../policies/products/integration-gateway-policy.xml')
    format: 'xml'
  }
}

// API operations
resource getProductsOperation 'Microsoft.ApiManagement/service/apis/operations@2023-05-01-preview' = {
  parent: api
  name: 'get-products'
  properties: {
    displayName: 'Get Products'
    method: 'GET'
    urlTemplate: '/api/v{version}/products'
    description: 'Get paginated list of products'
    parameters: [
      {
        name: 'version'
        in: 'path'
        required: true
        type: 'string'
        values: ['1', '2']
      }
      {
        name: 'page'
        in: 'query'
        required: false
        type: 'integer'
        defaultValue: '1'
      }
      {
        name: 'pageSize'
        in: 'query'
        required: false
        type: 'integer'
        defaultValue: '50'
      }
    ]
  }
}

resource getProductOperation 'Microsoft.ApiManagement/service/apis/operations@2023-05-01-preview' = {
  parent: api
  name: 'get-product'
  properties: {
    displayName: 'Get Product'
    method: 'GET'
    urlTemplate: '/api/v{version}/products/{id}'
    description: 'Get product by ID'
    parameters: [
      {
        name: 'version'
        in: 'path'
        required: true
        type: 'string'
        values: ['1', '2']
      }
      {
        name: 'id'
        in: 'path'
        required: true
        type: 'string'
      }
    ]
  }
}

resource createProductOperation 'Microsoft.ApiManagement/service/apis/operations@2023-05-01-preview' = {
  parent: api
  name: 'create-product'
  properties: {
    displayName: 'Create Product'
    method: 'POST'
    urlTemplate: '/api/v{version}/products'
    description: 'Create a new product'
    parameters: [
      {
        name: 'version'
        in: 'path'
        required: true
        type: 'string'
        values: ['1', '2']
      }
    ]
  }
}

resource updateProductOperation 'Microsoft.ApiManagement/service/apis/operations@2023-05-01-preview' = {
  parent: api
  name: 'update-product'
  properties: {
    displayName: 'Update Product'
    method: 'PUT'
    urlTemplate: '/api/v{version}/products/{id}'
    description: 'Update existing product'
    parameters: [
      {
        name: 'version'
        in: 'path'
        required: true
        type: 'string'
        values: ['1', '2']
      }
      {
        name: 'id'
        in: 'path'
        required: true
        type: 'string'
      }
    ]
  }
}

resource deleteProductOperation 'Microsoft.ApiManagement/service/apis/operations@2023-05-01-preview' = {
  parent: api
  name: 'delete-product'
  properties: {
    displayName: 'Delete Product'
    method: 'DELETE'
    urlTemplate: '/api/v{version}/products/{id}'
    description: 'Delete product by ID'
    parameters: [
      {
        name: 'version'
        in: 'path'
        required: true
        type: 'string'
        values: ['1', '2']
      }
      {
        name: 'id'
        in: 'path'
        required: true
        type: 'string'
      }
    ]
  }
}

// Operation policies with caching
resource getProductsPolicy 'Microsoft.ApiManagement/service/apis/operations/policies@2023-05-01-preview' = {
  parent: getProductsOperation
  name: 'policy'
  properties: {
    value: loadTextContent('../policies/operations/get-products-policy.xml')
    format: 'xml'
  }
}

resource getProductPolicy 'Microsoft.ApiManagement/service/apis/operations/policies@2023-05-01-preview' = {
  parent: getProductOperation
  name: 'policy'
  properties: {
    value: loadTextContent('../policies/operations/get-product-policy.xml')
    format: 'xml'
  }
}

// Named values for configuration
resource backendUrlNamedValue 'Microsoft.ApiManagement/service/namedValues@2023-05-01-preview' = {
  parent: apim
  name: 'backend-url'
  properties: {
    displayName: 'Backend URL'
    value: backendServiceUrl
    secret: false
  }
}

// Outputs
output apimServiceUrl string = 'https://${apim.properties.hostnameConfigurations[0].hostName}'
output apimManagementUrl string = 'https://${apim.name}.management.azure-api.net'
output apimGatewayUrl string = 'https://${apim.properties.gatewayUrl}'
output apimName string = apim.name
output apimResourceId string = apim.id
output principalId string = apim.identity.principalId