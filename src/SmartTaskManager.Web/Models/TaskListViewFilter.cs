namespace SmartTaskManager.Web.Models;

public sealed class TaskListViewFilter
{
    public TaskStatus? Status { get; set; }

    public TaskPriority? Priority { get; set; }

    public bool HasActiveFilters => Status.HasValue || Priority.HasValue;

    public void Clear()
    {
        Status = null;
        Priority = null;
    }
}
