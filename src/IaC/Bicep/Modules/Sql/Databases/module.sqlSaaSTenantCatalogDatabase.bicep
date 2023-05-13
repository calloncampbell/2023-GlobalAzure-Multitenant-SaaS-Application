@description('Environment Type. Ex. Laba')
@allowed(['lab', 'stg', 'prd'])
param envType string

@description('Environment slot if not the primary slot. Ex. labb, labd')
param envSlot string = ''

@description('Location of the resource')
param location string

@description('Tags')
param tags object

@description('Name of the databases')
param databaseInstanceNames array

@description('SqldbSkuName')
param sqldbSkuName string = 'ElasticPool'

@description('SqldbSkuTier')
param sqldbSkuTier string = 'GeneralPurpose'

@description('SqldbSkuCapacity')
param sqldbSkuCapacity int = 0

@description('SqldbPropertyMaxSizeBytes')
param sqldbPropertyMaxSizeBytes int = 268435456000

@description('SqldbPropertyZoneRedundant')
param sqldbPropertyZoneRedundant bool = false

@description('SqldbPropertyReadScale')
param sqldbPropertyReadScale string = 'Disabled'

@description('SqldbPropertyRequestedBackupStorageRedundancy')
param sqldbPropertyRequestedBackupStorageRedundancy string = 'Local'

@description('DatabaseServerName')
param databaseServerName string

@description('ElasticPoolId')
param elasticPoolId string

@description('Properties')

var locationShort = loadJsonContent('../../../Shared/locations.json').shortForm[location]


resource databaseServer 'Microsoft.Sql/servers@2022-08-01-preview' existing = {
  name: databaseServerName
}


resource databases 'Microsoft.Sql/servers/databases@2022-08-01-preview' = [for name in databaseInstanceNames: {
  name: envSlot == '' ? '${databaseServerName}/gab-${envType}-${locationShort}-saas-${name}-sqldb' : '${databaseServerName}/gab-${envSlot}-${locationShort}-saas-${name}-sqldb'
  location: location
  sku: {
    name: sqldbSkuName
    tier: sqldbSkuTier
    capacity: sqldbSkuCapacity
  }
  properties: {
    collation: 'SQL_Latin1_General_CP1_CI_AS'
    maxSizeBytes: sqldbPropertyMaxSizeBytes
    elasticPoolId: elasticPoolId
    catalogCollation: 'SQL_Latin1_General_CP1_CI_AS'
    zoneRedundant: sqldbPropertyZoneRedundant
    readScale: sqldbPropertyReadScale
    requestedBackupStorageRedundancy: sqldbPropertyRequestedBackupStorageRedundancy
    isLedgerOn: false
    availabilityZone: 'NoPreference'    
  }  
  tags: tags  
}]

