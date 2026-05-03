using SmartTaskManager.Web.Models;
using SmartTaskManager.Web.Models.Requests;
using UiTaskStatus = SmartTaskManager.Web.Models.TaskStatus;

namespace SmartTaskManager.Web.Services;

public sealed class TasksApiClient : ApiClientBase
{
    public TasksApiClient(HttpClient httpClient, SmartTaskManagerApiAccessTokenProvider accessTokenProvider)
        : base(httpClient, accessTokenProvider)
    {
    }

    public Task<TaskDashboardSummary> GetDashboardSummaryAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return GetAsync<TaskDashboardSummary>(
            $"api/users/{userId}/tasks/dashboard",
            cancellationToken);
    }

    public async Task<IReadOnlyCollection<TaskItem>> ListTasksAsync(
        Guid userId,
        TaskQueryFilter? filter = null,
        CancellationToken cancellationToken = default)
    {
        return await GetAsync<List<TaskItem>>(
            BuildTasksUri(userId, filter),
            cancellationToken);
    }

    public async Task<IReadOnlyCollection<TaskItem>> ListTasksAsync(
        Guid userId,
        UiTaskStatus? status,
        TaskPriority? priority,
        CancellationToken cancellationToken = default)
    {
        TaskQueryFilter? apiFilter = status.HasValue
            ? new TaskQueryFilter(Status: status)
            : priority.HasValue
                ? new TaskQueryFilter(Priority: priority)
                : null;

        IReadOnlyCollection<TaskItem> tasks = await ListTasksAsync(
            userId,
            apiFilter,
            cancellationToken);

        return tasks
            .Where(task => !status.HasValue || task.Status == status.Value)
            .Where(task => !priority.HasValue || task.Priority == priority.Value)
            .ToList();
    }

    public Task<TaskItem> GetTaskAsync(
        Guid userId,
        Guid taskId,
        CancellationToken cancellationToken = default)
    {
        return GetAsync<TaskItem>(
            $"api/users/{userId}/tasks/{taskId}",
            cancellationToken);
    }

    public async Task<IReadOnlyCollection<TaskHistoryEntry>> GetTaskHistoryAsync(
        Guid userId,
        Guid taskId,
        CancellationToken cancellationToken = default)
    {
        return await GetAsync<List<TaskHistoryEntry>>(
            $"api/users/{userId}/tasks/{taskId}/history",
            cancellationToken);
    }

    public Task<TaskItem> CreateTaskAsync(
        Guid userId,
        CreateTaskRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return PostAsync<CreateTaskRequest, TaskItem>(
            $"api/users/{userId}/tasks",
            request,
            cancellationToken);
    }

    public Task<TaskItem> UpdateTaskPriorityAsync(
        Guid userId,
        Guid taskId,
        UpdateTaskPriorityRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return PatchAsync<UpdateTaskPriorityRequest, TaskItem>(
            $"api/users/{userId}/tasks/{taskId}/priority",
            request,
            cancellationToken);
    }

    public Task<TaskItem> CompleteTaskAsync(
        Guid userId,
        Guid taskId,
        CancellationToken cancellationToken = default)
    {
        return PatchAsync<TaskItem>(
            $"api/users/{userId}/tasks/{taskId}/complete",
            cancellationToken);
    }

    public Task<TaskItem> ArchiveTaskAsync(
        Guid userId,
        Guid taskId,
        CancellationToken cancellationToken = default)
    {
        return PatchAsync<TaskItem>(
            $"api/users/{userId}/tasks/{taskId}/archive",
            cancellationToken);
    }

    private static string BuildTasksUri(Guid userId, TaskQueryFilter? filter)
    {
        if (filter is null)
        {
            return $"api/users/{userId}/tasks";
        }

        List<string> query = new();

        if (filter.Status.HasValue)
        {
            query.Add($"status={Uri.EscapeDataString(filter.Status.Value.ToString())}");
        }

        if (filter.Priority.HasValue)
        {
            query.Add($"priority={Uri.EscapeDataString(filter.Priority.Value.ToString())}");
        }

        if (filter.Overdue)
        {
            query.Add("overdue=true");
        }

        return query.Count == 0
            ? $"api/users/{userId}/tasks"
            : $"api/users/{userId}/tasks?{string.Join("&", query)}";
    }
}
