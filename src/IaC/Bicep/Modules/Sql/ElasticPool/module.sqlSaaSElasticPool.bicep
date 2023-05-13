@description('Environment Type. Ex. Laba')
@allowed(['lab', 'stg', 'prd'])
param envType string

@description('Environment slot if not the primary slot. Ex. labb, labd')
param envSlot string = ''

@description('Location of the resource')
param location string

@description('Tags')
param tags object

@description('SqlepInstanceId')
param sqlepInstanceId string = '001'

@description('SqlepSkuName')
param sqlepSkuName string = 'GP_Gen5'

@description('SqlepSkuTier')
param sqlepSkuTier string = 'GeneralPurpose'

@description('WqlepSkuFamily')
param sqlepSkuFamily string = 'Gen5'

@description('WqlepSkuCapacity')
param sqlepSkuCapacity int = 2

@description('DatabaseServerName')
param databaseServerName string

var locationLong = loadJsonContent('../../../Shared/locations.json').longForm[location]
var locationShort = loadJsonContent('../../../Shared/locations.json').shortForm[location]

resource databaseServer 'Microsoft.Sql/servers@2022-08-01-preview' existing = {
  name: databaseServerName
}

resource elasticPool 'Microsoft.Sql/servers/elasticPools@2022-08-01-preview' = {
  name: envSlot == '' ? 'gab-${envType}-${locationShort}-saas-sqlep-${sqlepInstanceId}' : 'gab-${envSlot}-${locationShort}-saas-sqlep-${sqlepInstanceId}'  
  location: locationLong
  parent: databaseServer
  sku: {
    name: sqlepSkuName
    tier: sqlepSkuTier
    family: sqlepSkuFamily
    capacity: sqlepSkuCapacity    
  }
  properties: {
    maxSizeBytes: 75161927680
    perDatabaseSettings: {
      minCapacity: json('0.0000')
      maxCapacity: json('2.0000')
    }
    zoneRedundant: false
    licenseType: 'LicenseIncluded'
    availabilityZone: 'NoPreference'
  }
  tags: tags
}

output elasticPoolName string = elasticPool.name
output elasticPoolId string = elasticPool.id
