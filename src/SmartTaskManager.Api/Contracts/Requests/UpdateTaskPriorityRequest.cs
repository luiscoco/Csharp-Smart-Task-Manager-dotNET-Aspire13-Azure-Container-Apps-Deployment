using System.ComponentModel.DataAnnotations;
using SmartTaskManager.Domain.Enums;

namespace SmartTaskManager.Api.Contracts.Requests;

public sealed class UpdateTaskPriorityRequest
{
    [Required]
    [EnumDataType(typeof(TaskPriority))]
    public TaskPriority? Priority { get; init; }
}
