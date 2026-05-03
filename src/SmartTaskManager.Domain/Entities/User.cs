using System;
using System.Collections.Generic;
using SmartTaskManager.Domain.Common;
using SmartTaskManager.Domain.Enums;

namespace SmartTaskManager.Domain.Entities;

public sealed class User : BaseEntity
{
    private readonly List<BaseTask> _tasks = new();

    public User(string userName)
        : this(Guid.NewGuid(), userName)
    {
    }

    public User(Guid id, string userName, IEnumerable<BaseTask>? tasks = null, DateTime? createdOnUtc = null)
        : base(id)
    {
        UserName = ValidateUserName(userName);
        CreatedOnUtc = createdOnUtc ?? DateTime.UtcNow;

        if (tasks is null)
        {
            return;
        }

        foreach (BaseTask task in tasks)
        {
            AttachTask(task);
        }
    }

    public string UserName { get; private set; }

    public DateTime CreatedOnUtc { get; }

    public IReadOnlyCollection<BaseTask> Tasks => _tasks.AsReadOnly();

    public PersonalTask CreatePersonalTask(
        string title,
        string description,
        DateTime dueDate,
        TaskPriority priority,
        Category? category = null)
    {
        return CreateTask(
            category,
            Category.CreatePersonalDefault,
            taskCategory => new PersonalTask(Id, title, description, dueDate, priority, taskCategory));
    }

    public WorkTask CreateWorkTask(
        string title,
        string description,
        DateTime dueDate,
        TaskPriority priority,
        Category? category = null)
    {
        return CreateTask(
            category,
            Category.CreateWorkDefault,
            taskCategory => new WorkTask(Id, title, description, dueDate, priority, taskCategory));
    }

    public LearningTask CreateLearningTask(
        string title,
        string description,
        DateTime dueDate,
        TaskPriority priority,
        Category? category = null)
    {
        return CreateTask(
            category,
            Category.CreateLearningDefault,
            taskCategory => new LearningTask(Id, title, description, dueDate, priority, taskCategory));
    }

    public void AttachTask(BaseTask task)
    {
        ArgumentNullException.ThrowIfNull(task);

        if (task.UserId != Id)
        {
            throw new DomainException("A task can only be attached to its owning user.");
        }

        if (_tasks.Exists(existingTask => existingTask.Id == task.Id))
        {
            return;
        }

        _tasks.Add(task);
    }

    private TTask CreateTask<TTask>(
        Category? category,
        Func<Category> defaultCategoryFactory,
        Func<Category, TTask> taskFactory)
        where TTask : BaseTask
    {
        Category taskCategory = category ?? defaultCategoryFactory();
        TTask task = taskFactory(taskCategory);
        _tasks.Add(task);
        return task;
    }

    private static string ValidateUserName(string userName)
    {
        if (string.IsNullOrWhiteSpace(userName))
        {
            throw new DomainException("User name is required.");
        }

        return userName.Trim();
    }
}
