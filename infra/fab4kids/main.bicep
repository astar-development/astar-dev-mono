param location string = 'uksouth'
param appServicePlanName string = 'ASP-rgastardev-aaac'
param webAppName string = 'fab4kids-site'

resource appServicePlan 'Microsoft.Web/serverfarms@2023-01-01' existing = {
  name: appServicePlanName
}

resource webApp 'Microsoft.Web/sites@2023-01-01' = {
  name: webAppName
  location: location
  tags: {
    'hidden-link: /app-insights-resource-id': resourceId('microsoft.insights/components', 'astar-dev')
  }
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|10.0'
      alwaysOn: true
    }
  }
}

output webAppUrl string = 'https://${webApp.properties.defaultHostName}'
