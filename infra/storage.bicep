@description('The name of the storage account')
param storageAccountName string

@description('The location to deploy the resources to')
param location string

resource storageAccount 'Microsoft.Storage/storageAccounts@2022-09-01' = {
  name: storageAccountName
  location: location
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: {
    accessTier: 'Hot'
  }
}

var keys = listKeys(storageAccount.id, '2019-06-01').keys
output connectionString string = 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};AccountKey=${keys[0].value};EndpointSuffix=${environment().suffixes.storage}'
