using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using SmartTaskManager.Api.Configuration;
using SmartTaskManager.Infrastructure.DependencyInjection;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
IConfiguration configuration = builder.Configuration;

builder.Services
    .AddSmartTaskManagerApiPresentation()
    .AddSmartTaskManagerApiSecurity(configuration)
    .AddSmartTaskManagerUseCaseServices()
    .AddSmartTaskManagerApiRuntimeServices()
    .AddSmartTaskManagerInfrastructure();

WebApplication app = builder.Build();

app.MapDefaultEndpoints();
app.ConfigureSmartTaskManagerPipeline();
await app.InitializeSmartTaskManagerAsync();
await app.RunAsync();
