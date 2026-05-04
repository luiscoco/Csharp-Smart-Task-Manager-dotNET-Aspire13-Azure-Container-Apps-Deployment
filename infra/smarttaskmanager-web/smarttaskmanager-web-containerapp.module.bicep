@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param aca_env_outputs_azure_container_apps_environment_default_domain string

param aca_env_outputs_azure_container_apps_environment_id string

param smarttaskmanager_web_containerimage string

param smarttaskmanager_web_containerport string

param webazureadinstance_value string

param webazureadtenantid_value string

param webazureadclientid_value string

@secure()
param webazureadclientsecret_value string

param webazureadcallbackpath_value string

param webazureadsignedoutcallbackpath_value string

param smarttaskmanagerapiaudience_value string

param smarttaskmanagerapiscopes_value string

param aca_env_outputs_azure_container_registry_endpoint string

param aca_env_outputs_azure_container_registry_managed_identity_id string

resource smarttaskmanager_web 'Microsoft.App/containerApps@2025-02-02-preview' = {
  name: 'smarttaskmanager-web'
  location: location
  properties: {
    configuration: {
      secrets: [
        {
          name: 'azuread--clientsecret'
          value: webazureadclientsecret_value
        }
      ]
      activeRevisionsMode: 'Single'
      ingress: {
        external: true
        targetPort: int(smarttaskmanager_web_containerport)
        transport: 'http'
      }
      registries: [
        {
          server: aca_env_outputs_azure_container_registry_endpoint
          identity: aca_env_outputs_azure_container_registry_managed_identity_id
        }
      ]
      runtime: {
        dotnet: {
          autoConfigureDataProtection: true
        }
      }
    }
    environmentId: aca_env_outputs_azure_container_apps_environment_id
    template: {
      containers: [
        {
          image: smarttaskmanager_web_containerimage
          name: 'smarttaskmanager-web'
          env: [
            {
              name: 'OTEL_DOTNET_EXPERIMENTAL_OTLP_EMIT_EXCEPTION_LOG_ATTRIBUTES'
              value: 'true'
            }
            {
              name: 'OTEL_DOTNET_EXPERIMENTAL_OTLP_EMIT_EVENT_LOG_ATTRIBUTES'
              value: 'true'
            }
            {
              name: 'OTEL_DOTNET_EXPERIMENTAL_OTLP_RETRY'
              value: 'in_memory'
            }
            {
              name: 'ASPNETCORE_FORWARDEDHEADERS_ENABLED'
              value: 'true'
            }
            {
              name: 'HTTP_PORTS'
              value: smarttaskmanager_web_containerport
            }
            {
              name: 'SMARTTASKMANAGER-API_HTTP'
              value: 'http://smarttaskmanager-api.internal.${aca_env_outputs_azure_container_apps_environment_default_domain}'
            }
            {
              name: 'services__smarttaskmanager-api__http__0'
              value: 'http://smarttaskmanager-api.internal.${aca_env_outputs_azure_container_apps_environment_default_domain}'
            }
            {
              name: 'SMARTTASKMANAGER-API_HTTPS'
              value: 'https://smarttaskmanager-api.internal.${aca_env_outputs_azure_container_apps_environment_default_domain}'
            }
            {
              name: 'services__smarttaskmanager-api__https__0'
              value: 'https://smarttaskmanager-api.internal.${aca_env_outputs_azure_container_apps_environment_default_domain}'
            }
            {
              name: 'SmartTaskManagerApi__BaseUrl'
              value: 'https://smarttaskmanager-api.internal.${aca_env_outputs_azure_container_apps_environment_default_domain}'
            }
            {
              name: 'AzureAd__Instance'
              value: webazureadinstance_value
            }
            {
              name: 'AzureAd__TenantId'
              value: webazureadtenantid_value
            }
            {
              name: 'AzureAd__ClientId'
              value: webazureadclientid_value
            }
            {
              name: 'AzureAd__ClientSecret'
              secretRef: 'azuread--clientsecret'
            }
            {
              name: 'AzureAd__CallbackPath'
              value: webazureadcallbackpath_value
            }
            {
              name: 'AzureAd__SignedOutCallbackPath'
              value: webazureadsignedoutcallbackpath_value
            }
            {
              name: 'SmartTaskManagerApi__Audience'
              value: smarttaskmanagerapiaudience_value
            }
            {
              name: 'SmartTaskManagerApi__Scopes'
              value: smarttaskmanagerapiscopes_value
            }
          ]
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 1
      }
    }
  }
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${aca_env_outputs_azure_container_registry_managed_identity_id}': { }
    }
  }
}