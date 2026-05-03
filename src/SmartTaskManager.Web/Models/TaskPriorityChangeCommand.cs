namespace SmartTaskManager.Web.Models;

public sealed record TaskPriorityChangeCommand(Guid TaskId, TaskPriority Priority);
