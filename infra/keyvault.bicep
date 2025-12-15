param location string
param vaultName string
param webAppPrincipalId string
param secretName string = 'SqlConnectionString'
param secretValue string

resource kv 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: vaultName
  location: location
  properties: {
    tenantId: subscription().tenantId
    enableRbacAuthorization: true
    sku: {
      name: 'standard'
      family: 'A'
    }
    enabledForDeployment: true
    enabledForTemplateDeployment: true
    enabledForDiskEncryption: true
    softDeleteRetentionInDays: 7
  }
}

// Grant Web App MI secrets user role
resource kvAccess 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(kv.id, webAppPrincipalId, 'b86a8fe4-44ce-4948-aee5-eccb2c155cd7') // Key Vault Secrets User role id
  scope: kv
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'b86a8fe4-44ce-4948-aee5-eccb2c155cd7')
    principalId: webAppPrincipalId
    principalType: 'ServicePrincipal'
  }
}

resource secret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  name: '${kv.name}/${secretName}'
  properties: {
    value: secretValue
  }
  dependsOn: [kv]
}

output vaultUri string = kv.properties.vaultUri
output secretId string = secret.id
