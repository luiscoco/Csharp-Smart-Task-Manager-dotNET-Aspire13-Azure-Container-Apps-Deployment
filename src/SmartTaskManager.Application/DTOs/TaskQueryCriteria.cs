using SmartTaskManager.Domain.Enums;

namespace SmartTaskManager.Application.DTOs;

public sealed record TaskQueryCriteria(
    TaskStatus? Status,
    TaskPriority? Priority,
    bool Overdue);
