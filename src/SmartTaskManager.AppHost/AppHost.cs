var builder = DistributedApplication.CreateBuilder(args);

var api = builder.AddProject<Projects.SmartTaskManager_Api>("smarttaskmanager-api", launchProfileName: "https");

builder.AddProject<Projects.SmartTaskManager_Web>("smarttaskmanager-web", launchProfileName: "https")
    .WithReference(api)
    .WithEnvironment("SmartTaskManagerApi__BaseUrl", api.GetEndpoint("https"))
    .WaitFor(api);

builder.Build().Run();
