using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Hosting;

namespace SmartTaskManager.Api.Security;

public sealed class DevelopmentOnlyAuthorizationHandler
    : AuthorizationHandler<DevelopmentOnlyRequirement>
{
    private readonly IHostEnvironment _hostEnvironment;

    public DevelopmentOnlyAuthorizationHandler(IHostEnvironment hostEnvironment)
    {
        _hostEnvironment = hostEnvironment;
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        DevelopmentOnlyRequirement requirement)
    {
        if (_hostEnvironment.IsDevelopment())
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
