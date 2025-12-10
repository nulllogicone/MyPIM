targetScope = 'subscription'

@description('The name of the resource group to deploy to')
param resourceGroupName string = 'rg-mypim-dev'

@description('The location to deploy the resources to')
param location string = 'westeurope'

@description('The name of the web application')
param webAppName string = 'app-mypim-${uniqueString(subscription().id, resourceGroupName)}'

@description('The name of the storage account')
param storageAccountName string = 'stmypim${uniqueString(subscription().id, resourceGroupName)}'

resource rg 'Microsoft.Resources/resourceGroups@2022-09-01' = {
  name: resourceGroupName
  location: location
}

module storage 'storage.bicep' = {
  scope: rg
  name: 'storageDeploy'
  params: {
    location: location
    storageAccountName: storageAccountName
  }
}

module webapp 'webapp.bicep' = {
  scope: rg
  name: 'webappDeploy'
  params: {
    location: location
    webAppName: webAppName
    storageConnectionString: storage.outputs.connectionString
  }
}

output webAppUrl string = webapp.outputs.webAppUrl
output webAppName string = webapp.outputs.webAppName
