@description('Environment Type. Ex. Laba')
@allowed(['lab', 'stg', 'prd'])
param envType string

@description('Environment slot if not the primary slot. Ex. labb, labd')
param envSlot string = ''

@description('Location of the resource')
param location string

@description('Tags')
param tags object

@description('Properties')
param sqlAdministratorLogin string

@secure()
param sqlAdministratorPassword string


var locationLong = loadJsonContent('../../../Shared/locations.json').longForm[location]
var locationShort = loadJsonContent('../../../Shared/locations.json').shortForm[location]


resource databaseServer 'Microsoft.Sql/servers@2022-08-01-preview' = {
  name: envSlot == '' ? 'gab-${envType}-${locationShort}-saas-sql' : 'gab-${envSlot}-${locationShort}-saas-sql'
  location: locationLong
  properties: {
    administratorLogin: sqlAdministratorLogin
    administratorLoginPassword: sqlAdministratorPassword
    version: '12.0'
    publicNetworkAccess: 'Enabled'    
    administrators: {
      administratorType: 'ActiveDirectory'
      principalType: 'User'
      login: 'callon.campbell@cloudmavericks.ca'
      sid: '08d58037-7c0a-4171-9ae2-1ce5798f3c5f'
      tenantId: 'a342abaf-b775-48e9-afe5-2611d1c585dd'
      azureADOnlyAuthentication: false
    }
    restrictOutboundNetworkAccess: 'Disabled'
  }
  tags: tags
}

output databaseServerName string = databaseServer.name


