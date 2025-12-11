targetScope = 'subscription'

@description('The name of the resource group to deploy to')
param resourceGroupName string = 'rg-mypim-dev'

@description('The location to deploy the resources to')
param location string = 'westeurope'

@description('The name of the web application')
param webAppName string = 'app-mypim-${uniqueString(subscription().id, resourceGroupName)}'

@description('The name of the storage account')
param storageAccountName string = 'stmypim${uniqueString(subscription().id, resourceGroupName)}'

@description('The name of the log analytics workspace')
param logAnalyticsWorkspaceName string = 'log-mypim-${uniqueString(subscription().id, resourceGroupName)}'

@description('The name of the application insights component')
param applicationInsightsName string = 'appi-mypim-${uniqueString(subscription().id, resourceGroupName)}'

@description('The URL of the Event Grid Viewer (optional)')
@secure()
param eventGridViewerUrl string = ''

@description('The Principal ID of the user to assign EventGrid Data Sender role to (optional)')
param userPrincipalId string = ''

@description('The Azure AD Tenant ID')
param azureAdTenantId string

@description('The Azure AD Domain')
param azureAdDomain string

@description('The Azure AD Instance')
param azureAdInstance string

@description('The Azure AD Client ID')
param azureAdClientId string

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



module monitoring 'monitoring.bicep' = {
  scope: rg
  name: 'monitoringDeploy'
  params: {
    location: location
    logAnalyticsWorkspaceName: logAnalyticsWorkspaceName
    applicationInsightsName: applicationInsightsName
  }
}

module webapp 'webapp.bicep' = {
  scope: rg
  name: 'webappDeploy'
  params: {
    location: location
    webAppName: webAppName
    storageConnectionString: storage.outputs.connectionString
    applicationInsightsConnectionString: monitoring.outputs.applicationInsightsConnectionString
    eventGridEndpoint: eventgrid.outputs.topicEndpoint
    azureAdTenantId: azureAdTenantId
    azureAdDomain: azureAdDomain
    azureAdClientId: azureAdClientId
    azureAdInstance: azureAdInstance
  }
}

output webAppUrl string = webapp.outputs.webAppUrl
output webAppName string = webapp.outputs.webAppName

module roleAssignment 'roleAssignment.bicep' = {
  scope: rg
  name: 'roleAssignmentResult'
  params: {
    principalId: webapp.outputs.principalId
    roleDefinitionId: '18d7d88d-d35e-4fb5-a5c3-7773c20a72d9' // User Access Administrator
    principalType: 'ServicePrincipal'
  }
}

module eventgrid 'eventgrid.bicep' = {
  scope: rg
  name: 'eventgridDeploy'
  params: {
    location: location
    topicName: 'eg-mypim-${uniqueString(subscription().id, resourceGroupName)}'
    viewerUrl: eventGridViewerUrl
  }
}

module eventGridRoleAssignment 'roleAssignment.bicep' = {
  scope: rg
  name: 'eventGridRoleAssignment'
  params: {
    principalId: webapp.outputs.principalId
    roleDefinitionId: 'd5a91429-5739-47e2-a06b-3470a27159e7' // EventGrid Data Sender
    principalType: 'ServicePrincipal'
  }
}

module userEventGridRoleAssignment 'roleAssignment.bicep' = if (!empty(userPrincipalId)) {
  scope: rg
  name: 'userEventGridRoleAssignment'
  params: {
    principalId: userPrincipalId
    roleDefinitionId: 'd5a91429-5739-47e2-a06b-3470a27159e7' // EventGrid Data Sender
    principalType: 'User'
  }
}
