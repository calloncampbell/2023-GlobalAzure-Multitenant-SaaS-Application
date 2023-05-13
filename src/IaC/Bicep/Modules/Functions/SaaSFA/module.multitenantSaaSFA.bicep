@description('Environment Type. Ex. lab')
@allowed(['lab', 'stg', 'prd'])
param envType string

@description('Environment slot if not the primary slot. Ex. labb, labd')
param envSlot string = ''

@description('Environment Label')
param envLabel string

@description('Location')
param location string

@description('Location override for app insights due to resource availability')
param appInsightsLocation string = location

var locationShort = loadJsonContent('../../../Shared/locations.json').shortForm[location]

@description('Specifies the function app storage sku name')
param storageSkuName string

@description('The tags for the deployment')
param tags object


// 3-24 characters - alphanumeric only
resource storage 'Microsoft.Storage/storageAccounts@2021-09-01' = if(envSlot == '') {
  kind: 'StorageV2'
  location: location
  name: 'gab${envType}${locationShort}saasfasa'
  sku: {
    name: storageSkuName
  }
  tags: tags
}

resource appServicePlan 'Microsoft.Web/serverfarms@2022-03-01' existing = {
  name: 'gab-${envType}-${locationShort}-saas-asp'
  scope: resourceGroup('gab-${envType}-${locationShort}-saas-rg')
}

var functionAppPrefix = 'gab-${envType}-${locationShort}-saas'

var functionAppConfig = [{
  name: 'AzureWebJobsStorage'
  value: 'DefaultEndpointsProtocol=https;AccountName=${storage.name};EndpointSuffix=${environment().suffixes.storage};AccountKey=${storage.listKeys().keys[0].value}'
}
{
  name: 'WEBSITE_CONTENTAZUREFILECONNECTIONSTRING'
  value: 'DefaultEndpointsProtocol=https;AccountName=${storage.name};EndpointSuffix=${environment().suffixes.storage};AccountKey=${storage.listKeys().keys[0].value}'
}
{
  name: 'WEBSITE_CONTENTSHARE'
  value: toLower('${functionAppPrefix}-fa')
}
{
  name: 'FUNCTIONS_EXTENSION_VERSION'
  value: '~4'
}
{
  name: 'APPINSIGHTS_INSTRUMENTATIONKEY'
  value: applicationInsights.properties.InstrumentationKey
}
{
  name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
  value: applicationInsights.properties.ConnectionString
}
{
  name: 'FUNCTIONS_WORKER_RUNTIME'
  value: 'dotnet'
}
{
  name: 'AzureAppConfiguration.CacheExpirationTimeInSeconds'
  value: '300'
}
{
  name: 'AzureAppConfiguration.EnvironmentLabel'
  value: envLabel
}
{
  name: 'WEBSITE_ENABLE_SYNC_UPDATE_SITE'
  value: 'true'
}]

resource functionApp 'Microsoft.Web/sites@2022-03-01' = if(envSlot == '') {
  name: '${functionAppPrefix}-fa'
  location: location
  tags: tags
  kind: 'functionapp'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: appServicePlan.id
    siteConfig: {
      appSettings: functionAppConfig
      ftpsState: 'FtpsOnly'
      minTlsVersion: '1.2'
    }
    httpsOnly: true
  } 
}

resource slotConfig 'Microsoft.Web/sites/config@2022-03-01' = {
  name: 'slotConfigNames'
  parent: functionApp
  properties: {
    appSettingNames: [
      'AzureAppConfiguration.EnvironmentLabel'
    ]
  }
}

resource applicationInsights 'Microsoft.Insights/components@2020-02-02' = if(envSlot == '') {
  name: 'gab-${envType}-${locationShort}-saas-ai'
  location: appInsightsLocation
  tags: tags
  kind: 'web'
  properties: {
    Application_Type: 'web'
    Request_Source: 'rest'
  }
}

resource functionAppSlot 'Microsoft.Web/sites/slots@2022-03-01' = if(envSlot != '')  {
  name: envSlot == '' ? envType : envSlot // just for the bicep template, otherwise it complains the name is less than 1 character
  parent: functionApp
  location: location
  tags: tags
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: appServicePlan.id
    siteConfig: {
      appSettings: functionAppConfig
      ftpsState: 'FtpsOnly'
      minTlsVersion: '1.2'
    }
    httpsOnly: true
  }
}

output functionAppName string = functionApp.name
output functionAppId string = functionApp.id
output functionAppPrincipalId string = functionApp.identity.principalId
output functionAppTenantId string = functionApp.identity.tenantId

output slotFunctionAppName string = envSlot == '' ? '' : functionAppSlot.name
output slotFunctionAppId string = envSlot == '' ? '' : functionAppSlot.id
output slotFunctionAppPrincipalId string = envSlot == '' ? '' : functionAppSlot.identity.principalId
output slotAppTenantId string = envSlot == '' ? '' : functionAppSlot.identity.tenantId
