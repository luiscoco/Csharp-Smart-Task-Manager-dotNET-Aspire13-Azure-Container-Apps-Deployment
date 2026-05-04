targetScope = 'subscription'

@minLength(1)
@maxLength(64)
@description('Name of the environment that can be used as part of naming resource convention, the name of the resource group for your application will use this name, prefixed with rg-')
param environmentName string

@minLength(1)
@description('The location used for all deployed resources')
param location string

@description('Id of the user or app to assign application roles')
param principalId string = ''

param apiAuthorizationRequiredScope string
param apiAzureAdAudience string
param apiAzureAdClientId string
param apiAzureAdInstance string
param apiAzureAdTenantId string
param smartTaskManagerApiAudience string
param smartTaskManagerApiScopes string
@secure()
param smartTaskManagerSqlConnectionString string
param webAzureAdCallbackPath string
param webAzureAdClientId string
@secure()
param webAzureAdClientSecret string
param webAzureAdInstance string
param webAzureAdSignedOutCallbackPath string
param webAzureAdTenantId string

var tags = {
  'azd-env-name': environmentName
}

resource rg 'Microsoft.Resources/resourceGroups@2022-09-01' = {
  name: 'rg-${environmentName}'
  location: location
  tags: tags
}

module aca_env 'aca-env/aca-env.module.bicep' = {
  name: 'aca-env'
  scope: rg
  params: {
    location: location
    userPrincipalId: principalId
  }
}
output ACA_ENV_AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN string = aca_env.outputs.AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN
output ACA_ENV_AZURE_CONTAINER_APPS_ENVIRONMENT_ID string = aca_env.outputs.AZURE_CONTAINER_APPS_ENVIRONMENT_ID
output ACA_ENV_AZURE_CONTAINER_REGISTRY_ENDPOINT string = aca_env.outputs.AZURE_CONTAINER_REGISTRY_ENDPOINT
output ACA_ENV_AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID string = aca_env.outputs.AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID
output AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN string = aca_env.outputs.AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN
output AZURE_CONTAINER_REGISTRY_ENDPOINT string = aca_env.outputs.AZURE_CONTAINER_REGISTRY_ENDPOINT
