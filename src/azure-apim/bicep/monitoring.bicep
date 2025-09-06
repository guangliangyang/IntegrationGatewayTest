@description('Monitoring resources for API Management')
param location string = resourceGroup().location
param apimName string
param tags object = {}

// Log Analytics Workspace
resource logAnalyticsWorkspace 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: '${apimName}-logs'
  location: location
  tags: tags
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: 30
    features: {
      searchVersion: 1
      legacy: 0
      enableLogAccessUsingOnlyResourcePermissions: true
    }
  }
}

// Application Insights
resource applicationInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: '${apimName}-insights'
  location: location
  tags: tags
  kind: 'web'
  properties: {
    Application_Type: 'web'
    Flow_Type: 'Bluefield'
    Request_Source: 'rest'
    RetentionInDays: 90
    WorkspaceResourceId: logAnalyticsWorkspace.id
    IngestionMode: 'LogAnalytics'
    publicNetworkAccessForIngestion: 'Enabled'
    publicNetworkAccessForQuery: 'Enabled'
  }
}

// Get reference to existing APIM service
resource apim 'Microsoft.ApiManagement/service@2023-05-01-preview' existing = {
  name: apimName
}

// Logger for Application Insights
resource apimLogger 'Microsoft.ApiManagement/service/loggers@2023-05-01-preview' = {
  parent: apim
  name: 'applicationinsights'
  properties: {
    loggerType: 'applicationInsights'
    credentials: {
      instrumentationKey: applicationInsights.properties.InstrumentationKey
    }
    isBuffered: true
    resourceId: applicationInsights.id
  }
}

// Diagnostic settings for API Management
resource apimDiagnostics 'Microsoft.ApiManagement/service/diagnostics@2023-05-01-preview' = {
  parent: apim
  name: 'applicationinsights'
  properties: {
    alwaysLog: 'allErrors'
    httpCorrelationProtocol: 'W3C'
    verbosity: 'information'
    logClientIp: true
    loggerId: apimLogger.id
    sampling: {
      samplingType: 'fixed'
      percentage: 100
    }
    frontend: {
      request: {
        headers: ['Accept', 'Accept-Charset', 'Accept-Encoding', 'Accept-Language', 'Authorization', 'Connection', 'Content-Length', 'Content-Type', 'Cookie', 'Host', 'Origin', 'Pragma', 'Referer', 'User-Agent', 'X-Forwarded-For', 'X-Forwarded-Host', 'X-Forwarded-Proto']
        body: {
          bytes: 8192
        }
      }
      response: {
        headers: ['Cache-Control', 'Content-Encoding', 'Content-Length', 'Content-Type', 'Date', 'ETag', 'Expires', 'Last-Modified', 'Server', 'Vary', 'X-Powered-By']
        body: {
          bytes: 8192
        }
      }
    }
    backend: {
      request: {
        headers: ['Accept', 'Accept-Charset', 'Accept-Encoding', 'Accept-Language', 'Authorization', 'Connection', 'Content-Length', 'Content-Type', 'Host', 'Origin', 'Pragma', 'Referer', 'User-Agent', 'X-Forwarded-For', 'X-Forwarded-Host', 'X-Forwarded-Proto']
        body: {
          bytes: 8192
        }
      }
      response: {
        headers: ['Cache-Control', 'Content-Encoding', 'Content-Length', 'Content-Type', 'Date', 'ETag', 'Expires', 'Last-Modified', 'Server', 'Vary', 'X-Powered-By']
        body: {
          bytes: 8192
        }
      }
    }
  }
}

// Alerts for API Management
resource responseTimeAlert 'Microsoft.Insights/metricAlerts@2018-03-01' = {
  name: '${apimName}-high-response-time'
  location: 'global'
  tags: tags
  properties: {
    description: 'Alert when API response time exceeds threshold'
    severity: 2
    enabled: true
    scopes: [
      apim.id
    ]
    evaluationFrequency: 'PT1M'
    windowSize: 'PT5M'
    criteria: {
      'odata.type': 'Microsoft.Azure.Monitor.SingleResourceMultipleMetricCriteria'
      allOf: [
        {
          threshold: 5000
          name: 'Metric1'
          metricNamespace: 'Microsoft.ApiManagement/service'
          metricName: 'Duration'
          operator: 'GreaterThan'
          timeAggregation: 'Average'
          criterionType: 'StaticThresholdCriterion'
        }
      ]
    }
    actions: []
  }
}

resource errorRateAlert 'Microsoft.Insights/metricAlerts@2018-03-01' = {
  name: '${apimName}-high-error-rate'
  location: 'global'
  tags: tags
  properties: {
    description: 'Alert when API error rate exceeds threshold'
    severity: 1
    enabled: true
    scopes: [
      apim.id
    ]
    evaluationFrequency: 'PT1M'
    windowSize: 'PT5M'
    criteria: {
      'odata.type': 'Microsoft.Azure.Monitor.SingleResourceMultipleMetricCriteria'
      allOf: [
        {
          threshold: 10
          name: 'Metric1'
          metricNamespace: 'Microsoft.ApiManagement/service'
          metricName: 'UnauthorizedRequests'
          operator: 'GreaterThan'
          timeAggregation: 'Total'
          criterionType: 'StaticThresholdCriterion'
        }
      ]
    }
    actions: []
  }
}

// Outputs
output instrumentationKey string = applicationInsights.properties.InstrumentationKey
output connectionString string = applicationInsights.properties.ConnectionString
output workspaceId string = logAnalyticsWorkspace.id
output applicationInsightsId string = applicationInsights.id