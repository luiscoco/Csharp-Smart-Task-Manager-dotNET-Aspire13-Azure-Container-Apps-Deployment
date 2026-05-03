namespace SmartTaskManager.Web.Models;

public sealed record UserSummary(
    Guid Id,
    string UserName,
    DateTime CreatedOnUtc);
