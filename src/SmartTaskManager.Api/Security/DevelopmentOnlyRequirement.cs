using Microsoft.AspNetCore.Authorization;

namespace SmartTaskManager.Api.Security;

public sealed class DevelopmentOnlyRequirement : IAuthorizationRequirement
{
}
