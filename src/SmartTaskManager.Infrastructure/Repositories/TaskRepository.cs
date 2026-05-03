using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SmartTaskManager.Application.Abstractions.Persistence;
using SmartTaskManager.Domain.Common;
using SmartTaskManager.Domain.Entities;
using SmartTaskManager.Domain.Enums;
using SmartTaskManager.Domain.Records;
using SmartTaskManager.Infrastructure.Persistence;
using SmartTaskManager.Infrastructure.Persistence.Models;

namespace SmartTaskManager.Infrastructure.Repositories;

public sealed class TaskRepository : ITaskRepository
{
    private readonly SmartTaskManagerDbContextFactory _dbContextFactory;

    public TaskRepository(SmartTaskManagerDbContextFactory dbContextFactory)
    {
        _dbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));
    }

    public async Task<BaseTask?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            return null;
        }

        await using SmartTaskManagerDbContext dbContext = _dbContextFactory.CreateDbContext();

        TaskRecord? record = await CreateReadQuery(dbContext.Tasks)
            .SingleOrDefaultAsync(task => task.Id == id, cancellationToken);

        return record is null
            ? null
            : MapToDomain(record);
    }

    public async Task<IReadOnlyCollection<BaseTask>> ListAsync(CancellationToken cancellationToken = default)
    {
        await using SmartTaskManagerDbContext dbContext = _dbContextFactory.CreateDbContext();

        List<TaskRecord> records = await CreateReadQuery(dbContext.Tasks)
            .OrderBy(task => task.DueDate)
            .ThenBy(task => task.Title)
            .ToListAsync(cancellationToken);

        return records
            .Select(MapToDomain)
            .ToList();
    }

    public async Task AddAsync(BaseTask entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        await using SmartTaskManagerDbContext dbContext = _dbContextFactory.CreateDbContext();

        bool alreadyExists = await dbContext.Tasks
            .AnyAsync(task => task.Id == entity.Id, cancellationToken);

        if (alreadyExists)
        {
            throw new DomainException($"A task with id '{entity.Id}' already exists.");
        }

        dbContext.Tasks.Add(MapToRecord(entity));
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(BaseTask entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        await using SmartTaskManagerDbContext dbContext = _dbContextFactory.CreateDbContext();

        TaskRecord? record = await dbContext.Tasks
            .SingleOrDefaultAsync(task => task.Id == entity.Id, cancellationToken);

        if (record is null)
        {
            throw new DomainException($"Task with id '{entity.Id}' was not found.");
        }

        MapTaskValues(entity, record);
        await ReplaceHistoryEntriesAsync(dbContext, entity.Id, entity.History, cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        EnsureIdentifierProvided(id);

        await using SmartTaskManagerDbContext dbContext = _dbContextFactory.CreateDbContext();

        TaskRecord? record = await dbContext.Tasks
            .SingleOrDefaultAsync(task => task.Id == id, cancellationToken);

        if (record is null)
        {
            throw new DomainException($"Task with id '{id}' was not found.");
        }

        dbContext.Tasks.Remove(record);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<IReadOnlyCollection<BaseTask>> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        EnsureIdentifierProvided(userId);

        return QueryTasksAsync(
            task => task.UserId == userId,
            cancellationToken);
    }

    public Task<IReadOnlyCollection<BaseTask>> GetByCategoryAsync(
        Guid userId,
        string categoryName,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(categoryName))
        {
            throw new ArgumentException("Category name is required.", nameof(categoryName));
        }

        EnsureIdentifierProvided(userId);
        string normalizedCategoryName = categoryName.Trim();

        return QueryTasksAsync(
            task =>
                task.UserId == userId &&
                task.CategoryName == normalizedCategoryName,
            cancellationToken);
    }

    private async Task<IReadOnlyCollection<BaseTask>> QueryTasksAsync(
        Expression<Func<TaskRecord, bool>> predicate,
        CancellationToken cancellationToken)
    {
        await using SmartTaskManagerDbContext dbContext = _dbContextFactory.CreateDbContext();

        List<TaskRecord> records = await CreateReadQuery(dbContext.Tasks)
            .Where(predicate)
            .ToListAsync(cancellationToken);

        return records
            .Select(MapToDomain)
            .ToList();
    }

    private static IQueryable<TaskRecord> CreateReadQuery(IQueryable<TaskRecord> query)
    {
        return query
            .AsNoTracking()
            .Include(task => task.HistoryEntries);
    }

    private static TaskRecord MapToRecord(BaseTask task)
    {
        TaskRecord record = new();
        MapTaskValues(task, record);

        foreach (TaskHistoryRecord historyRecord in CreateHistoryRecords(task.Id, task.History))
        {
            record.HistoryEntries.Add(historyRecord);
        }

        return record;
    }

    private static void MapTaskValues(BaseTask task, TaskRecord record)
    {
        record.Id = task.Id;
        record.UserId = task.UserId;
        record.TaskType = task.TaskType;
        record.Title = task.Title;
        record.Description = task.Description;
        record.DueDate = NormalizeUtc(task.DueDate);
        record.Priority = task.Priority;
        record.Status = task.Status;
        record.CategoryId = task.Category.Id;
        record.CategoryName = task.Category.Name;
        record.CategoryDescription = task.Category.Description;
    }

    private static async Task ReplaceHistoryEntriesAsync(
        SmartTaskManagerDbContext dbContext,
        Guid taskId,
        IReadOnlyCollection<HistoryEntry> historyEntries,
        CancellationToken cancellationToken)
    {
        await dbContext.TaskHistoryEntries
            .Where(history => history.TaskId == taskId)
            .ExecuteDeleteAsync(cancellationToken);

        dbContext.TaskHistoryEntries.AddRange(CreateHistoryRecords(taskId, historyEntries));
    }

    private static IReadOnlyCollection<TaskHistoryRecord> CreateHistoryRecords(
        Guid taskId,
        IReadOnlyCollection<HistoryEntry> historyEntries)
    {
        return historyEntries
            .Select((entry, index) => new TaskHistoryRecord
            {
                Id = Guid.NewGuid(),
                TaskId = taskId,
                Sequence = index,
                OccurredOnUtc = NormalizeUtc(entry.OccurredOnUtc),
                Action = entry.Action,
                Details = entry.Details
            })
            .ToList();
    }

    private static BaseTask MapToDomain(TaskRecord record)
    {
        Category category = new(
            record.CategoryId,
            record.CategoryName,
            record.CategoryDescription);

        IReadOnlyCollection<HistoryEntry> historyEntries = record.HistoryEntries
            .OrderBy(history => history.Sequence)
            .ThenBy(history => history.OccurredOnUtc)
            .Select(history => new HistoryEntry(
                NormalizeUtc(history.OccurredOnUtc),
                history.Action,
                history.Details))
            .ToList();

        DateTime dueDate = NormalizeUtc(record.DueDate);

        return record.TaskType switch
        {
            "Personal" => new PersonalTask(
                record.Id,
                record.UserId,
                record.Title,
                record.Description,
                dueDate,
                record.Priority,
                category,
                record.Status,
                historyEntries),
            "Work" => new WorkTask(
                record.Id,
                record.UserId,
                record.Title,
                record.Description,
                dueDate,
                record.Priority,
                category,
                record.Status,
                historyEntries),
            "Learning" => new LearningTask(
                record.Id,
                record.UserId,
                record.Title,
                record.Description,
                dueDate,
                record.Priority,
                category,
                record.Status,
                historyEntries),
            _ => throw new DomainException($"Task type '{record.TaskType}' is not supported.")
        };
    }

    private static DateTime NormalizeUtc(DateTime value)
    {
        return value.Kind == DateTimeKind.Utc
            ? value
            : DateTime.SpecifyKind(value, DateTimeKind.Utc);
    }

    private static void EnsureIdentifierProvided(Guid id)
    {
        if (id == Guid.Empty)
        {
            throw new DomainException("A valid identifier is required.");
        }
    }
}
