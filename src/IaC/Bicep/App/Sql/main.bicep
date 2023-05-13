targetScope = 'subscription'

@description('Environment Type. Ex. lab')
@allowed(['lab', 'stg', 'prd'])
param envType string

@description('Environment slot if not the primary slot. Ex. labb, labd')
param envSlot string

@description('Environment Label. Ex. LabA')
param envLabel string

@description('Location ex: CanadaCentral')
param location string = deployment().location

param date string = utcNow('f')


var tagsJson = loadJsonContent('tags.json')

var tags = union(tagsJson, {
  'Deployment Date Utc': date
},
{
  Environment: envType
})
var locationShort = loadJsonContent('../../Shared/locations.json').shortForm[location]

var resourceGroupName = envSlot == '' ? 'gab-${envType}-${locationShort}-saas-rg' : 'gab-${envSlot}-${locationShort}-saas-rg'

resource rg 'Microsoft.Resources/resourceGroups@2021-04-01' = {
  name: resourceGroupName
  location: location
  tags: tags
}


@description('Properties')
param sqlAdministratorLogin string

@secure()
param sqlAdministratorPassword string

module sqlServer '../../Modules/Sql/Server/module.sqlSaaSServer.bicep' = {
  name: '${deployment().name}-sqlSaaSServer'
  params: {
    envType: envType
    location: location
    envSlot: envSlot
    sqlAdministratorLogin: sqlAdministratorLogin
    sqlAdministratorPassword: sqlAdministratorPassword
    tags: tags    
  }
  scope: rg
}


@description('SqlepInstanceId. Ex. 001, 002, etc.')
param sqlepInstanceId string

@description('SqlepSkuName')
param sqlepSkuName string

@description('SqlepSkuTier')
param sqlepSkuTier string

@description('WqlepSkuFamily')
param sqlepSkuFamily string

@description('WqlepSkuCapacity')
param sqlepSkuCapacity int

module dniSqlElasticPool '../../Modules/Sql/ElasticPool/module.sqlSaaSElasticPool.bicep' = {
  name: '${deployment().name}-sqlSaaSEp'
  params: {
    envType: envType
    location: location
    envSlot: envSlot
    sqlepInstanceId: sqlepInstanceId
    sqlepSkuName: sqlepSkuName
    sqlepSkuTier: sqlepSkuTier
    sqlepSkuFamily: sqlepSkuFamily
    sqlepSkuCapacity: sqlepSkuCapacity 
    databaseServerName: sqlServer.outputs.databaseServerName
    tags: tags
  }
  scope: rg
}


@description('Name of the databases')
param databaseInstanceNames array

@description('SqldbSkuName')
param sqldbSkuName string

@description('SqldbSkuTier')
param sqldbSkuTier string

@description('SqldbSkuCapacity')
param sqldbSkuCapacity int

@description('SqldbPropertyMaxSizeBytes')
param sqldbPropertyMaxSizeBytes int

@description('SqldbPropertyZoneRedundant')
param sqldbPropertyZoneRedundant bool

@description('SqldbPropertyReadScale')
param sqldbPropertyReadScale string

@description('SqldbPropertyRequestedBackupStorageRedundancy')
param sqldbPropertyRequestedBackupStorageRedundancy string


module dniSqlDatabaseTenantCatalog '../../Modules/Sql/Databases/module.sqlSaaSTenantCatalogDatabase.bicep' = {
  name: '${deployment().name}-sqlSaaSDb'
  params: {
    envType: envType
    location: location
    envSlot: envSlot
    tags: tags
    databaseInstanceNames: databaseInstanceNames
    sqldbSkuName: sqldbSkuName
    sqldbSkuTier: sqldbSkuTier
    sqldbSkuCapacity: sqldbSkuCapacity
    sqldbPropertyMaxSizeBytes: sqldbPropertyMaxSizeBytes
    sqldbPropertyZoneRedundant: sqldbPropertyZoneRedundant
    sqldbPropertyReadScale: sqldbPropertyReadScale
    sqldbPropertyRequestedBackupStorageRedundancy: sqldbPropertyRequestedBackupStorageRedundancy
    databaseServerName: sqlServer.outputs.databaseServerName    
    elasticPoolId: dniSqlElasticPool.outputs.elasticPoolId
  }
  scope: rg
}


module sqlDatabaseElasticJob '../../Modules/Sql/Databases/module.sqlSaaSElasticJobDatabase.bicep' = {
  name: '${deployment().name}-sqlSaaSDb2'
  params: {
    envType: envType
    location: location
    envSlot: envSlot
    tags: tags
    sqldbSkuName: sqldbSkuName
    sqldbSkuTier: sqldbSkuTier
    sqldbSkuCapacity: sqldbSkuCapacity
    sqldbPropertyMaxSizeBytes: sqldbPropertyMaxSizeBytes
    sqldbPropertyZoneRedundant: sqldbPropertyZoneRedundant
    sqldbPropertyReadScale: sqldbPropertyReadScale
    sqldbPropertyRequestedBackupStorageRedundancy: sqldbPropertyRequestedBackupStorageRedundancy
    databaseServerName: sqlServer.outputs.databaseServerName    
    elasticPoolId: dniSqlElasticPool.outputs.elasticPoolId
  }
  scope: rg
}


module elasticJobAgent '../../Modules/Sql/ElasticJobAgent/module.sqlSaaSElasticJobAgent.bicep' = {
  name: '${deployment().name}-ElasticJobAgent'
  params: {
    envType: envType
    location: location
    envSlot: envSlot
    tags: tags
    databaseServerName: sqlServer.outputs.databaseServerName
    elasticJobDatabaseId: sqlDatabaseElasticJob.outputs.elasticJobDatabaseId
  }
  scope: rg
}
