namespace SmartTaskManager.Web.Models;

public sealed record TaskQueryFilter(
    TaskStatus? Status = null,
    TaskPriority? Priority = null,
    bool Overdue = false);
