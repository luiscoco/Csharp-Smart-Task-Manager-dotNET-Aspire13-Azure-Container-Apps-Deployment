namespace SmartTaskManager.Web.Models.Requests;

public sealed record CreateTaskRequest(
    string Title,
    string Description,
    DateTime DueDate,
    TaskPriority Priority,
    string? CategoryName,
    TaskKind TaskType);
