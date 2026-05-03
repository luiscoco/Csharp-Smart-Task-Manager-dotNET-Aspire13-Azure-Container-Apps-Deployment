@description('Location for the App Service resources.')
param location string = resourceGroup().location

@description('App Service Plan name.')
param planName string

@allowed([
  'B1'
])
@description('SKU for the recommended paid profile. This template is intended for the B1 paid profile.')
param skuName string = 'B1'

@description('Web front-end app name.')
param webAppName string

@description('API app name.')
param apiAppName string

@secure()
@description('App settings for SmartTaskManager.Web.')
param webAppSettings object

@secure()
@description('App settings for SmartTaskManager.Api.')
param apiAppSettings object

resource plan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: planName
  location: location
  sku: {
    name: skuName
    tier: 'Basic'
    size: skuName
    capacity: 1
  }
  kind: 'app'
  properties: {}
}

resource webApp 'Microsoft.Web/sites@2023-12-01' = {
  name: webAppName
  location: location
  kind: 'app'
  properties: {
    serverFarmId: plan.id
    httpsOnly: true
    clientAffinityEnabled: true
  }
}

resource webAppConfig 'Microsoft.Web/sites/config@2023-12-01' = {
  parent: webApp
  name: 'web'
  properties: {
    alwaysOn: true
    webSocketsEnabled: true
    minTlsVersion: '1.2'
    ftpsState: 'FtpsOnly'
  }
}

resource webAppSettingsResource 'Microsoft.Web/sites/config@2023-12-01' = {
  parent: webApp
  name: 'appsettings'
  properties: webAppSettings
}

resource apiApp 'Microsoft.Web/sites@2023-12-01' = {
  name: apiAppName
  location: location
  kind: 'app'
  properties: {
    serverFarmId: plan.id
    httpsOnly: true
    clientAffinityEnabled: false
  }
}

resource apiAppConfig 'Microsoft.Web/sites/config@2023-12-01' = {
  parent: apiApp
  name: 'web'
  properties: {
    alwaysOn: true
    webSocketsEnabled: false
    minTlsVersion: '1.2'
    ftpsState: 'FtpsOnly'
  }
}

resource apiAppSettingsResource 'Microsoft.Web/sites/config@2023-12-01' = {
  parent: apiApp
  name: 'appsettings'
  properties: apiAppSettings
}

output webAppUrl string = 'https://${webApp.properties.defaultHostName}'
output apiAppUrl string = 'https://${apiApp.properties.defaultHostName}'
