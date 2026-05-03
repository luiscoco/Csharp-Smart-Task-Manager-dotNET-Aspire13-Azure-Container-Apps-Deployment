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

    api
        .WithEnvironment("ASPNETCORE_FORWARDEDHEADERS_ENABLED", "true")
        .WithEnvironment("ConnectionStrings__SmartTaskManager", sqlConnectionString);

    web
        .WithEnvironment("ASPNETCORE_FORWARDEDHEADERS_ENABLED", "true")
        .WithEnvironment("AzureAd__ClientSecret", webClientSecret);
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
