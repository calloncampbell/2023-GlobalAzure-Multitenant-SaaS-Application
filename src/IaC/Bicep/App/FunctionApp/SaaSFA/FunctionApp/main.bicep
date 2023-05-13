targetScope = 'subscription'

@description('Environment Type. Ex. lab')
@allowed(['lab', 'stg', 'prd'])
param envType string

@description('Environment slot if not the primary slot. Ex. labb, labd')
#disable-next-line no-unused-params
param envSlot string = ''

@description('Environment Label. Ex. LabA')
param envLabel string

@description('Location ex: WestUs2')
param location string = deployment().location

var tagsJson = loadJsonContent('../tags.json')

param date string = utcNow('f')

var tags = union(tagsJson, {
  'Deployment Date Utc': date
},
{
  Environment: envType
})

var locationShort = loadJsonContent('../../../Shared/locations.json').shortForm[location]

var resourceGroupName = 'tmx-${envType}-${locationShort}-dni-rg'

resource rg 'Microsoft.Resources/resourceGroups@2021-04-01' = {
  name: resourceGroupName
  location: location
  tags: tags
}

var appConfigs = [
  {name: 'tmx-${envType}-wus2-ac', location: 'wus2'}
  {name: 'tmx-${envType}-wcus-ac', location: 'wcus'}
]

module dniFA '../../../Modules/FunctionApps/DniFA/module.dniFA.bicep' = {
  name: deployment().name
  params: {
    envType: envType
    location: location
    appInsightsLocation: location == 'WestCentralUs' ? 'WestUs2' : location
    envLabel: envLabel
    envSlot: envSlot
    tags: tags
    storageSkuName: 'Standard_LRS'
    primaryAppConfigEndpoint: 'https://${appConfigs[0].name}.azconfig.io'
    secondaryAppConfigEndpoint: 'https://${appConfigs[1].name}.azconfig.io'
  }
  scope: rg
}

module appConfigDataReaderRole '../../../Modules/AppConfiguration/module.appConfigRoleAssignments.bicep' = [for appConfig in appConfigs: {
  name: '${deployment().name}-ac-datareader'
  params: {
    appConfigName: appConfig.name
    appId: envSlot == '' ? dniFA.outputs.functionAppId : dniFA.outputs.slotFunctionAppId
    appName: envSlot == '' ? dniFA.outputs.functionAppName : dniFA.outputs.slotFunctionAppName
    appPrincipalId: envSlot == '' ? dniFA.outputs.functionAppPrincipalId : dniFA.outputs.slotFunctionAppPrincipalId
    role: 'App Configuration Data Reader'
  }
  scope: resourceGroup('tmx-lab-${appConfig.location}-ac-rg')
}]

module keyVault '../../../Modules/KeyVault/module.keyVaultAccessPolicy.bicep' = {
  name: '${deployment().name}-kv-secretsuser'
  params: {
    keyVaultName: 'tmx-${envType}-wus2-callstk-kv'
    appPrincipalId: envSlot == '' ? dniFA.outputs.functionAppPrincipalId : dniFA.outputs.slotFunctionAppPrincipalId
    appTenantId: envSlot == '' ? dniFA.outputs.functionAppTenantId : dniFA.outputs.slotAppTenantId
    secretsPermissions: ['get']
  }
  scope: resourceGroup('tmx-${envType}-wus2-callstack-rg')
}
