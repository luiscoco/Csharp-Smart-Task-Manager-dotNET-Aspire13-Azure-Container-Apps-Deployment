using Azure.Provisioning.AppContainers;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddAzureContainerAppEnvironment("aca-env");

var api = builder.AddProject<Projects.SmartTaskManager_Api>("smarttaskmanager-api", launchProfileName: "https")
    .PublishAsAzureContainerApp((_, app) => ConfigureSingleReplica(app));

var web = builder.AddProject<Projects.SmartTaskManager_Web>("smarttaskmanager-web", launchProfileName: "https")
    .WithExternalHttpEndpoints()
    .WithReference(api)
    .WithEnvironment("SmartTaskManagerApi__BaseUrl", api.GetEndpoint("https"))
    .WaitFor(api)
    .PublishAsAzureContainerApp((_, app) => ConfigureSingleReplica(app));

if (builder.ExecutionContext.IsPublishMode)
{
    var sqlConnectionString = builder.AddParameter("smartTaskManagerSqlConnectionString", secret: true);
    var webClientSecret = builder.AddParameter("webAzureAdClientSecret", secret: true);
    var webAzureAdInstance = builder.AddParameter("webAzureAdInstance");
    var webAzureAdTenantId = builder.AddParameter("webAzureAdTenantId");
    var webAzureAdClientId = builder.AddParameter("webAzureAdClientId");
    var webAzureAdCallbackPath = builder.AddParameter("webAzureAdCallbackPath");
    var webAzureAdSignedOutCallbackPath = builder.AddParameter("webAzureAdSignedOutCallbackPath");
    var smartTaskManagerApiAudience = builder.AddParameter("smartTaskManagerApiAudience");
    var smartTaskManagerApiScopes = builder.AddParameter("smartTaskManagerApiScopes");
    var apiAzureAdInstance = builder.AddParameter("apiAzureAdInstance");
    var apiAzureAdTenantId = builder.AddParameter("apiAzureAdTenantId");
    var apiAzureAdClientId = builder.AddParameter("apiAzureAdClientId");
    var apiAzureAdAudience = builder.AddParameter("apiAzureAdAudience");
    var apiAuthorizationRequiredScope = builder.AddParameter("apiAuthorizationRequiredScope");

    api
        .WithEnvironment("ASPNETCORE_FORWARDEDHEADERS_ENABLED", "true")
        .WithEnvironment("ConnectionStrings__SmartTaskManager", sqlConnectionString)
        .WithEnvironment("AzureAd__Instance", apiAzureAdInstance)
        .WithEnvironment("AzureAd__TenantId", apiAzureAdTenantId)
        .WithEnvironment("AzureAd__ClientId", apiAzureAdClientId)
        .WithEnvironment("AzureAd__Audience", apiAzureAdAudience)
        .WithEnvironment("Authorization__RequiredScope", apiAuthorizationRequiredScope)
        .WithEnvironment("Database__EnableEfLogging", "false")
        .WithEnvironment("Database__EnableDetailedErrors", "false")
        .WithEnvironment("Database__EnableSensitiveDataLogging", "false")
        .WithEnvironment("Seeding__EnableSampleData", "false");

    web
        .WithEnvironment("ASPNETCORE_FORWARDEDHEADERS_ENABLED", "true")
        .WithEnvironment("AzureAd__Instance", webAzureAdInstance)
        .WithEnvironment("AzureAd__TenantId", webAzureAdTenantId)
        .WithEnvironment("AzureAd__ClientId", webAzureAdClientId)
        .WithEnvironment("AzureAd__ClientSecret", webClientSecret)
        .WithEnvironment("AzureAd__CallbackPath", webAzureAdCallbackPath)
        .WithEnvironment("AzureAd__SignedOutCallbackPath", webAzureAdSignedOutCallbackPath)
        .WithEnvironment("SmartTaskManagerApi__Audience", smartTaskManagerApiAudience)
        .WithEnvironment("SmartTaskManagerApi__Scopes", smartTaskManagerApiScopes);
}

builder.Build().Run();

static void ConfigureSingleReplica(ContainerApp app)
{
    app.Template.Scale = new ContainerAppScale
    {
        MinReplicas = 1,
        MaxReplicas = 1
    };
}
