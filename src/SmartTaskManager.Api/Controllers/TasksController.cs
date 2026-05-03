using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SmartTaskManager.Api.Contracts;
using SmartTaskManager.Api.Contracts.Requests;
using SmartTaskManager.Api.Contracts.Responses;
using SmartTaskManager.Api.Security;
using SmartTaskManager.Application.DTOs;
using SmartTaskManager.Application.Services;
using SmartTaskManager.Domain.Enums;
using SmartTaskManager.Domain.Records;

namespace SmartTaskManager.Api.Controllers;

/// <summary>
/// Manages task creation, lifecycle actions, history, filters, and dashboard summaries.
/// </summary>
[ApiController]
[Authorize]
[Route("api/users/{userId:guid}/tasks")]
[Produces("application/json")]
public sealed class TasksController : ControllerBase
{
    private readonly TaskService _taskService;

    public TasksController(TaskService taskService)
    {
        _taskService = taskService ?? throw new ArgumentNullException(nameof(taskService));
    }

    /// <summary>
    /// Returns all tasks for the specified user ID, optionally filtered by status or priority.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<TaskResponse>), 200)]
    public async Task<ActionResult<IReadOnlyCollection<TaskResponse>>> ListTasks(
        [FromRoute] Guid userId,
        [FromQuery] SmartTaskManager.Domain.Enums.TaskStatus? status,
        [FromQuery] SmartTaskManager.Domain.Enums.TaskPriority? priority,
        CancellationToken cancellationToken)
    {
        if (status.HasValue || priority.HasValue)
        {
            var criteria = new TaskQueryCriteria(status, priority, false);

            IReadOnlyCollection<TaskSummary> summaries = await _taskService.QueryTasksAsync(
                userId,
                criteria,
                cancellationToken);

            return Ok(TaskResponse.FromApplication(summaries));
        }

        IReadOnlyCollection<TaskSummary> allSummaries = await _taskService.ListTasksAsync(
            userId,
            cancellationToken);

        return Ok(TaskResponse.FromApplication(allSummaries));
    }

    /// <summary>
    /// Returns a dashboard summary for the specified user.
    /// </summary>
    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(DashboardSummaryResponse), 200)]
    public async Task<ActionResult<DashboardSummaryResponse>> GetDashboard(
        [FromRoute] Guid userId,
        CancellationToken cancellationToken)
    {
        TaskDashboardSummary dashboard = await _taskService.GetDashboardSummaryAsync(
            userId,
            cancellationToken);

        return Ok(DashboardSummaryResponse.FromApplication(dashboard));
    }

    /// <summary>
    /// Returns a specific task for the specified user.
    /// </summary>
    [HttpGet("{taskId:guid}")]
    [ProducesResponseType(typeof(TaskResponse), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 404)]
    public async Task<ActionResult<TaskResponse>> GetTask(
        [FromRoute] Guid userId,
        [FromRoute] Guid taskId,
        CancellationToken cancellationToken)
    {
        TaskSummary summary = await _taskService.GetTaskAsync(userId, taskId, cancellationToken);
        return Ok(TaskResponse.FromApplication(summary));
    }

    /// <summary>
    /// Returns the full action and status history for a specific task.
    /// </summary>
    [HttpGet("{taskId:guid}/history")]
    [ProducesResponseType(typeof(IReadOnlyCollection<HistoryEntryResponse>), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 404)]
    public async Task<ActionResult<IReadOnlyCollection<HistoryEntryResponse>>> GetTaskHistory(
        [FromRoute] Guid userId,
        [FromRoute] Guid taskId,
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<HistoryEntry> history = await _taskService.GetTaskHistoryAsync(
            userId,
            taskId,
            cancellationToken);

        return Ok(HistoryEntryResponse.FromDomain(history));
    }

    /// <summary>
    /// Creates a new task for the specified user.
    /// Requires Editor or Admin role.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.RequireEditorRole)]
    [ProducesResponseType(typeof(TaskResponse), 201)]
    [ProducesResponseType(typeof(ApiErrorResponse), 400)]
    public async Task<ActionResult<TaskResponse>> CreateTask(
        [FromRoute] Guid userId,
        [FromBody] CreateTaskRequest request,
        CancellationToken cancellationToken)
    {
        TaskSummary summary = await CreateTaskAsync(userId, request, cancellationToken);

        return CreatedAtAction(
            nameof(GetTask),
            new { userId, taskId = summary.Id },
            TaskResponse.FromApplication(summary));
    }

    /// <summary>
    /// Updates the priority of an existing task.
    /// Requires Editor or Admin role.
    /// </summary>
    [HttpPatch("{taskId:guid}/priority")]
    [Authorize(Policy = AuthorizationPolicies.RequireEditorRole)]
    [ProducesResponseType(typeof(TaskResponse), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 400)]
    [ProducesResponseType(typeof(ApiErrorResponse), 404)]
    public async Task<ActionResult<TaskResponse>> UpdatePriority(
        [FromRoute] Guid userId,
        [FromRoute] Guid taskId,
        [FromBody] UpdateTaskPriorityRequest request,
        CancellationToken cancellationToken)
    {
        if (!request.Priority.HasValue)
        {
            return BadRequest(ApiErrorResponse.Create(
                400,
                "TaskUpdateError",
                "Priority is required.",
                HttpContext.TraceIdentifier,
                Request.Path.ToString()));
        }

        TaskSummary summary = await _taskService.UpdateTaskPriorityAsync(
            userId,
            taskId,
            request.Priority.Value,
            cancellationToken);

        return Ok(TaskResponse.FromApplication(summary));
    }

    /// <summary>
    /// Marks a task as completed.
    /// Requires Editor or Admin role.
    /// </summary>
    [HttpPost("{taskId:guid}/complete")]
    [Authorize(Policy = AuthorizationPolicies.RequireEditorRole)]
    [ProducesResponseType(typeof(TaskResponse), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 400)]
    [ProducesResponseType(typeof(ApiErrorResponse), 404)]
    public async Task<ActionResult<TaskResponse>> CompleteTask(
        [FromRoute] Guid userId,
        [FromRoute] Guid taskId,
        CancellationToken cancellationToken)
    {
        TaskSummary summary = await _taskService.MarkTaskAsCompletedAsync(
            userId,
            taskId,
            cancellationToken);

        return Ok(TaskResponse.FromApplication(summary));
    }

    /// <summary>
    /// Archives a task.
    /// Requires Admin role.
    /// </summary>
    [HttpPost("{taskId:guid}/archive")]
    [Authorize(Policy = AuthorizationPolicies.RequireAdminRole)]
    [ProducesResponseType(typeof(TaskResponse), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 400)]
    [ProducesResponseType(typeof(ApiErrorResponse), 404)]
    public async Task<ActionResult<TaskResponse>> ArchiveTask(
        [FromRoute] Guid userId,
        [FromRoute] Guid taskId,
        CancellationToken cancellationToken)
    {
        TaskSummary summary = await _taskService.ArchiveTaskAsync(
            userId,
            taskId,
            cancellationToken);

        return Ok(TaskResponse.FromApplication(summary));
    }

    private Task<TaskSummary> CreateTaskAsync(
        Guid userId,
        CreateTaskRequest request,
        CancellationToken cancellationToken)
    {
        SmartTaskManager.Domain.Enums.TaskPriority effectivePriority = request.Priority ?? SmartTaskManager.Domain.Enums.TaskPriority.Medium;

        return request.TaskType switch
        {
            TaskKind.Work => _taskService.CreateWorkTaskAsync(
                userId,
                request.Title,
                request.Description ?? string.Empty,
                request.DueDate ?? DateTime.UtcNow.AddDays(1),
                effectivePriority,
                request.CategoryName,
                cancellationToken),

            TaskKind.Learning => _taskService.CreateLearningTaskAsync(
                userId,
                request.Title,
                request.Description ?? string.Empty,
                request.DueDate ?? DateTime.UtcNow.AddDays(1),
                effectivePriority,
                request.CategoryName,
                cancellationToken),

            _ => _taskService.CreatePersonalTaskAsync(
                userId,
                request.Title,
                request.Description ?? string.Empty,
                request.DueDate ?? DateTime.UtcNow.AddDays(1),
                effectivePriority,
                request.CategoryName,
                cancellationToken)
        };
    }
}
