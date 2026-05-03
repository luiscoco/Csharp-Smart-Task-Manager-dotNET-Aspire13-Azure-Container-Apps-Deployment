using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SmartTaskManager.Api.Data;
using SmartTaskManager.Api.Middleware;
using SmartTaskManager.Infrastructure.Persistence;

namespace SmartTaskManager.Api.Configuration;

public static class WebApplicationExtensions
{
    public static WebApplication ConfigureSmartTaskManagerPipeline(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "SmartTaskManager API v1");
                options.RoutePrefix = "swagger";
                options.DocumentTitle = "SmartTaskManager API";
            });
        }
        else
        {
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseMiddleware<ApiExceptionMiddleware>();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();

        return app;
    }

    public static async Task InitializeSmartTaskManagerAsync(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        using IServiceScope scope = app.Services.CreateScope();
        IServiceProvider serviceProvider = scope.ServiceProvider;
        IConfiguration configuration = serviceProvider.GetRequiredService<IConfiguration>();
        ILogger logger = serviceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("SmartTaskManager.Startup");

        DatabaseInitializer databaseInitializer = serviceProvider.GetRequiredService<DatabaseInitializer>();
        await databaseInitializer.ApplyMigrationsAsync();

        if (!ShouldSeedSampleData(app.Environment, configuration))
        {
            return;
        }

        SampleDataSeeder sampleDataSeeder = serviceProvider.GetRequiredService<SampleDataSeeder>();
        await sampleDataSeeder.SeedAsync();
        logger.LogInformation("Sample data seeding completed.");
    }

    private static bool ShouldSeedSampleData(IHostEnvironment hostEnvironment, IConfiguration configuration)
    {
        bool sampleDataEnabled = bool.TryParse(configuration["Seeding:EnableSampleData"], out bool value) && value;

        return hostEnvironment.IsDevelopment() && sampleDataEnabled;
    }
}
