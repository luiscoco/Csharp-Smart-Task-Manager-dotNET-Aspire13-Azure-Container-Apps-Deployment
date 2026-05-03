@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param aca_env_outputs_azure_container_apps_environment_default_domain string

param aca_env_outputs_azure_container_apps_environment_id string

param smarttaskmanager_api_containerimage string

param smarttaskmanager_api_containerport string

@secure()
param smarttaskmanagersqlconnectionstring_value string

param aca_env_outputs_azure_container_registry_endpoint string

param aca_env_outputs_azure_container_registry_managed_identity_id string

resource smarttaskmanager_api 'Microsoft.App/containerApps@2025-02-02-preview' = {
  name: 'smarttaskmanager-api'
  location: location
  properties: {
    configuration: {
      secrets: [
        {
          name: 'connectionstrings--smarttaskmanager'
          value: smarttaskmanagersqlconnectionstring_value
        }
      ]
      activeRevisionsMode: 'Single'
      ingress: {
        external: false
        targetPort: int(smarttaskmanager_api_containerport)
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
          image: smarttaskmanager_api_containerimage
          name: 'smarttaskmanager-api'
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
              value: smarttaskmanager_api_containerport
            }
            {
              name: 'ConnectionStrings__SmartTaskManager'
              secretRef: 'connectionstrings--smarttaskmanager'
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