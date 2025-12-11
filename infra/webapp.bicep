@description('The name of the web application')
param webAppName string

@description('The location to deploy the resources to')
param location string

@description('The connection string for the storage account')
@secure()
param storageConnectionString string

@description('The connection string for Application Insights')
@secure()
param applicationInsightsConnectionString string

@description('The endpoint of the Event Grid Topic')
param eventGridEndpoint string

@description('The Azure AD Tenant ID')
param azureAdTenantId string

@description('The Azure AD Domain')
param azureAdDomain string

@description('The Azure AD Client ID')
param azureAdClientId string

resource appServicePlan 'Microsoft.Web/serverfarms@2022-03-01' = {
  name: 'asp-${webAppName}'
  location: location
  sku: {
    name: 'F1'
    tier: 'Free'
  }
  properties: {
    reserved: true // Required for Linux
  }
}

resource webApp 'Microsoft.Web/sites@2022-03-01' = {
  name: webAppName
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: appServicePlan.id
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|8.0'
      appSettings: [
        {
          name: 'ConnectionStrings__AzureTableStorage'
          value: storageConnectionString
        }
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: applicationInsightsConnectionString
        }
        {
          name: 'ASPNETCORE_ENVIRONMENT'
          value: 'Production'
        }
        {
          name: 'ApplicationInsightsAgent_EXTENSION_VERSION'
          value: '~3'
        }
        {
          name: 'XDT_MicrosoftApplicationInsights_Mode'
          value: 'recommended'
        }
        {
          name: 'EventGrid__Endpoint'
          value: eventGridEndpoint
        }
        {
          name: 'AzureAd__TenantId'
          value: azureAdTenantId
        }
        {
          name: 'AzureAd__Instance'
          value: 'https://login.microsoftonline.com/'
        }
        {
          name: 'AzureAd__Domain'
          value: azureAdDomain
        }
        {
          name: 'AzureAd__ClientId'
          value: azureAdClientId
        }
        {
          name: 'AzureAd__CallbackPath'
          value: '/signin-oidc'
        }
      ]
    }
    httpsOnly: true
  }
}

output webAppUrl string = 'https://${webApp.properties.defaultHostName}'
output webAppName string = webApp.name
output principalId string = webApp.identity.principalId
