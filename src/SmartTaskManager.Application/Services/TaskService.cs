using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SmartTaskManager.Application.Abstractions.Notifications;
using SmartTaskManager.Application.Abstractions.Persistence;
using SmartTaskManager.Application.Abstractions.Services;
using SmartTaskManager.Application.DTOs;
using SmartTaskManager.Application.Filters;
using SmartTaskManager.Domain.Common;
using SmartTaskManager.Domain.Entities;
using SmartTaskManager.Domain.Enums;
using SmartTaskManager.Domain.Records;
using DomainTaskStatus = SmartTaskManager.Domain.Enums.TaskStatus;

namespace SmartTaskManager.Application.Services;

public sealed class TaskService
{
    private readonly ITaskRepository _taskRepository;
    private readonly IRepository<User> _userRepository;
    private readonly INotificationService _notificationService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly HighPriorityTaskFilter _highPriorityTaskFilter;
    private readonly StatusTaskFilter _statusTaskFilter;
    private readonly OverdueTaskFilter _overdueTaskFilter;

    public TaskService(
        ITaskRepository taskRepository,
        IRepository<User> userRepository,
        INotificationService notificationService,
        IDateTimeProvider dateTimeProvider,
        HighPriorityTaskFilter highPriorityTaskFilter,
        StatusTaskFilter statusTaskFilter,
        OverdueTaskFilter overdueTaskFilter)
    {
        _taskRepository = taskRepository ?? throw new ArgumentNullException(nameof(taskRepository));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
        _dateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));
        _highPriorityTaskFilter = highPriorityTaskFilter ?? throw new ArgumentNullException(nameof(highPriorityTaskFilter));
        _statusTaskFilter = statusTaskFilter ?? throw new ArgumentNullException(nameof(statusTaskFilter));
        _overdueTaskFilter = overdueTaskFilter ?? throw new ArgumentNullException(nameof(overdueTaskFilter));
    }

    public Task<TaskSummary> CreatePersonalTaskAsync(
        Guid userId,
        string title,
        string description,
        DateTime dueDate,
        TaskPriority priority,
        string? categoryName = null,
        CancellationToken cancellationToken = default)
    {
        return CreateTaskAsync(
            userId,
            user => user.CreatePersonalTask(title, description, dueDate, priority, CreateCategory(categoryName)),
            cancellationToken);
    }

    public Task<TaskSummary> CreateWorkTaskAsync(
        Guid userId,
        string title,
        string description,
        DateTime dueDate,
        TaskPriority priority,
        string? categoryName = null,
        CancellationToken cancellationToken = default)
    {
        return CreateTaskAsync(
            userId,
            user => user.CreateWorkTask(title, description, dueDate, priority, CreateCategory(categoryName)),
            cancellationToken);
    }

    public Task<TaskSummary> CreateLearningTaskAsync(
        Guid userId,
        string title,
        string description,
        DateTime dueDate,
        TaskPriority priority,
        string? categoryName = null,
        CancellationToken cancellationToken = default)
    {
        return CreateTaskAsync(
            userId,
            user => user.CreateLearningTask(title, description, dueDate, priority, CreateCategory(categoryName)),
            cancellationToken);
    }

    public async Task<IReadOnlyCollection<TaskSummary>> ListTasksAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<BaseTask> tasks = await LoadTasksForUserAsync(userId, cancellationToken);
        return CreateSummaries(tasks);
    }

    public async Task<IReadOnlyCollection<TaskSummary>> QueryTasksAsync(
        Guid userId,
        TaskQueryCriteria criteria,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(criteria);

        IReadOnlyCollection<BaseTask> tasks = await LoadTasksForUserAsync(userId, cancellationToken);
        return FilterTasks(tasks, criteria);
    }

    public async Task<IReadOnlyCollection<TaskSummary>> FilterTasksByStatusAsync(
        Guid userId,
        DomainTaskStatus status,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<BaseTask> tasks = await LoadTasksForUserAsync(userId, cancellationToken);
        IReadOnlyCollection<BaseTask> filteredTasks = _statusTaskFilter.Apply(tasks, status);

        return CreateSummaries(filteredTasks);
    }

    public async Task<IReadOnlyCollection<TaskSummary>> FilterTasksByPriorityAsync(
        Guid userId,
        TaskPriority priority,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<BaseTask> tasks = await LoadTasksForUserAsync(userId, cancellationToken);
        IReadOnlyCollection<BaseTask> filteredTasks = ApplyPriorityFilter(tasks, priority);

        return CreateSummaries(filteredTasks);
    }

    public async Task<IReadOnlyCollection<TaskSummary>> ListHighPriorityTasksAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<BaseTask> tasks = await LoadTasksForUserAsync(userId, cancellationToken);
        IReadOnlyCollection<BaseTask> filteredTasks = _highPriorityTaskFilter.Apply(tasks);

        return CreateSummaries(filteredTasks);
    }

    public async Task<IReadOnlyCollection<TaskSummary>> GetOverdueTasksAsync(
        Guid userId,
        DateTime? referenceDate = null,
        CancellationToken cancellationToken = default)
    {
        DateTime effectiveReferenceDate = referenceDate ?? _dateTimeProvider.UtcNow;
        IReadOnlyCollection<BaseTask> tasks = await LoadTasksForUserAsync(userId, cancellationToken);
        IReadOnlyCollection<BaseTask> filteredTasks = _overdueTaskFilter.Apply(tasks, effectiveReferenceDate);

        return CreateSummaries(filteredTasks, effectiveReferenceDate);
    }

    public async Task<TaskSummary> GetTaskAsync(
        Guid userId,
        Guid taskId,
        CancellationToken cancellationToken = default)
    {
        BaseTask task = await LoadTaskForUserAsync(userId, taskId, cancellationToken);
        return CreateSummary(task);
    }

    public async Task<TaskDashboardSummary> GetDashboardSummaryAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<BaseTask> tasks = await LoadTasksForUserAsync(userId, cancellationToken);
        return TaskDashboardSummary.FromTasks(tasks, _dateTimeProvider.UtcNow);
    }

    public async Task<TaskSummary> UpdateTaskPriorityAsync(
        Guid userId,
        Guid taskId,
        TaskPriority newPriority,
        CancellationToken cancellationToken = default)
    {
        BaseTask task = await LoadTaskForUserAsync(userId, taskId, cancellationToken);
        task.ChangePriority(newPriority);

        await _taskRepository.UpdateAsync(task, cancellationToken);
        _notificationService.Notify($"Task '{task.Title}' priority updated to {task.Priority}.");

        return CreateSummary(task);
    }

    public async Task<TaskSummary> MarkTaskAsCompletedAsync(
        Guid userId,
        Guid taskId,
        CancellationToken cancellationToken = default)
    {
        BaseTask task = await LoadTaskForUserAsync(userId, taskId, cancellationToken);
        task.MarkAsCompleted();

        await _taskRepository.UpdateAsync(task, cancellationToken);
        _notificationService.NotifyTaskCompleted(task);

        return CreateSummary(task);
    }

    public async Task<TaskSummary> ArchiveTaskAsync(
        Guid userId,
        Guid taskId,
        CancellationToken cancellationToken = default)
    {
        BaseTask task = await LoadTaskForUserAsync(userId, taskId, cancellationToken);
        task.Archive();

        await _taskRepository.UpdateAsync(task, cancellationToken);
        _notificationService.NotifyTaskArchived(task);

        return CreateSummary(task);
    }

    public async Task<IReadOnlyCollection<HistoryEntry>> GetTaskHistoryAsync(
        Guid userId,
        Guid taskId,
        CancellationToken cancellationToken = default)
    {
        BaseTask task = await LoadTaskForUserAsync(userId, taskId, cancellationToken);

        return task.History
            .OrderBy(entry => entry.OccurredOnUtc)
            .ToList();
    }

    private async Task<TaskSummary> CreateTaskAsync(
        Guid userId,
        Func<User, BaseTask> taskFactory,
        CancellationToken cancellationToken)
    {
        User user = await LoadUserAsync(userId, cancellationToken);
        BaseTask task = taskFactory(user);

        await _taskRepository.AddAsync(task, cancellationToken);

        _notificationService.Notify($"Task '{task.Title}' created successfully.");

        return CreateSummary(task);
    }

    private async Task<IReadOnlyCollection<BaseTask>> LoadTasksForUserAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        await LoadUserAsync(userId, cancellationToken);
        return await _taskRepository.GetByUserIdAsync(userId, cancellationToken);
    }

    private async Task<BaseTask> LoadTaskForUserAsync(
        Guid userId,
        Guid taskId,
        CancellationToken cancellationToken)
    {
        EnsureIdentifierProvided(taskId, "Task id is required.");
        await LoadUserAsync(userId, cancellationToken);

        BaseTask? task = await _taskRepository.GetByIdAsync(taskId, cancellationToken);
        if (task is null || task.UserId != userId)
        {
            throw new DomainException("Task not found.");
        }

        return task;
    }

    private async Task<User> LoadUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        EnsureIdentifierProvided(userId, "User id is required.");

        User? user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            throw new DomainException("User not found.");
        }

        return user;
    }

    private TaskSummary CreateSummary(BaseTask task, DateTime? referenceDate = null)
    {
        return TaskSummary.FromTask(task, referenceDate ?? _dateTimeProvider.UtcNow);
    }

    private IReadOnlyCollection<TaskSummary> CreateSummaries(
        IEnumerable<BaseTask> tasks,
        DateTime? referenceDate = null)
    {
        DateTime effectiveReferenceDate = referenceDate ?? _dateTimeProvider.UtcNow;

        return SortTasks(tasks)
            .Select(task => CreateSummary(task, effectiveReferenceDate))
            .ToList();
    }

    private IReadOnlyCollection<TaskSummary> FilterTasks(
        IReadOnlyCollection<BaseTask> tasks,
        TaskQueryCriteria criteria)
    {
        if (criteria.Status.HasValue)
        {
            IReadOnlyCollection<BaseTask> filteredTasks = _statusTaskFilter.Apply(tasks, criteria.Status.Value);
            return CreateSummaries(filteredTasks);
        }

        if (criteria.Priority.HasValue)
        {
            IReadOnlyCollection<BaseTask> filteredTasks = ApplyPriorityFilter(tasks, criteria.Priority.Value);
            return CreateSummaries(filteredTasks);
        }

        if (criteria.Overdue)
        {
            DateTime referenceDate = _dateTimeProvider.UtcNow;
            IReadOnlyCollection<BaseTask> filteredTasks = _overdueTaskFilter.Apply(tasks, referenceDate);
            return CreateSummaries(filteredTasks, referenceDate);
        }

        return CreateSummaries(tasks);
    }

    private static IReadOnlyCollection<BaseTask> ApplyPriorityFilter(
        IEnumerable<BaseTask> tasks,
        TaskPriority priority)
    {
        return tasks
            .Where(task => task.Priority == priority)
            .ToList();
    }

    private static IEnumerable<BaseTask> SortTasks(IEnumerable<BaseTask> tasks)
    {
        return tasks
            .OrderBy(task => task.DueDate)
            .ThenByDescending(task => task.Priority)
            .ThenBy(task => task.Title, StringComparer.OrdinalIgnoreCase);
    }

    private static Category? CreateCategory(string? categoryName)
    {
        return string.IsNullOrWhiteSpace(categoryName)
            ? null
            : new Category(categoryName);
    }

    private static void EnsureIdentifierProvided(Guid id, string message)
    {
        if (id == Guid.Empty)
        {
            throw new DomainException(message);
        }
    }
}
