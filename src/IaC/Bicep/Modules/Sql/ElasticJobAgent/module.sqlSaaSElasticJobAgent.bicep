@description('Environment Type. Ex. Laba')
@allowed(['lab', 'stg', 'prd'])
param envType string

@description('Environment slot if not the primary slot. Ex. labb, labd')
param envSlot string = ''

@description('Location of the resource')
param location string

@description('Tags')
param tags object

@description('ElasticJobDatabaseId')
param elasticJobDatabaseId string

@description('DatabaseServerName')
param databaseServerName string

var locationLong = loadJsonContent('../../../Shared/locations.json').longForm[location]
var locationShort = loadJsonContent('../../../Shared/locations.json').shortForm[location]

resource databaseServer 'Microsoft.Sql/servers@2022-08-01-preview' existing = {
  name: databaseServerName
}


resource gablabbcncsaasdemo 'Microsoft.Sql/servers/jobAgents@2022-08-01-preview' = {
  sku: {
    name: 'Agent'
    capacity: 100
  }
  properties: {
    databaseId: elasticJobDatabaseId
  }
  location: locationLong
  name: envSlot == '' ? '${databaseServerName}/gab-${envType}-${locationShort}-saas-elastic-job-eja' : '${databaseServerName}/gab-${envSlot}-${locationShort}-saas-elastic-job-eja'
}

