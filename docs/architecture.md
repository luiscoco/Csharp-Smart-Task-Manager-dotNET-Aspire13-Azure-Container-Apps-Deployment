# SmartTaskManager Architecture

## Overview

`SmartTaskManager` is a layered `.NET 10` console application built using a simple Clean Architecture style.

The goal of the architecture is to keep:

- business rules inside the `Domain` layer
- use-case orchestration inside the `Application` layer
- technical details inside the `Infrastructure` layer
- user interaction inside the `UI` layer

This structure makes the project easier to understand, extend, test, and present as a portfolio project.

## High-Level Dependency Flow

The dependency direction is intentionally one-way:

```text
UI -> Application -> Domain
Infrastructure -> Domain
Infrastructure -> Application
```

What this means in practice:

- `UI` knows about `Application` services
- `Application` knows about `Domain` entities and contracts
- `Infrastructure` implements contracts defined by the inner layers
- `Domain` does not depend on the console UI or storage implementation

## Solution Structure

```text
src/
├─ SmartTaskManager.Domain
├─ SmartTaskManager.Application
├─ SmartTaskManager.Infrastructure
└─ SmartTaskManager.UI.Console
```

## Architectural Principles Used

This project is based on a few simple principles:

### 1. Separation of Concerns

Each layer has one clear job.

- `Domain` models the business
- `Application` coordinates operations
- `Infrastructure` handles storage and external services
- `UI` handles menus, prompts, and output

### 2. Encapsulation

Entities protect their own internal state. For example, tasks cannot be freely modified from anywhere in the system. Instead, they expose behavior methods such as:

- `ChangePriority()`
- `MarkAsCompleted()`
- `Archive()`

### 3. Dependency Inversion

The application depends on interfaces, not concrete implementations. For example:

- `TaskService` depends on `ITaskRepository`
- the UI does not know whether tasks are stored in memory, a file, or a database

### 4. Beginner-Friendly Clean Architecture

This is not an overengineered enterprise solution. It uses clean boundaries, but keeps the code readable and educational.

## Layer-by-Layer Explanation

## Domain Layer

### Responsibility

The `Domain` layer contains the core business model.

It answers questions like:

- What is a task?
- What is a user?
- What does it mean to complete a task?
- When is a task overdue?
- What business rules must always be true?

### Main Types

- `User`
- `BaseTask`
- `PersonalTask`
- `WorkTask`
- `LearningTask`
- `Category`
- `HistoryEntry`
- `TaskPriority`
- `TaskStatus`

### Example: Domain Encapsulation

The abstract `BaseTask` entity controls all state changes through methods:

```csharp
public void ChangePriority(TaskPriority newPriority)
{
    EnsureActiveForModification();

    TaskPriority validatedPriority = ValidatePriority(newPriority);
    if (Priority == validatedPriority)
    {
        return;
    }

    TaskPriority previousPriority = Priority;
    Priority = validatedPriority;

    AddHistoryEntry(
        "PriorityChanged",
        BuildPriorityChangedMessage(previousPriority, validatedPriority));
}

public void MarkAsCompleted()
{
    if (Status == TaskStatus.Completed)
    {
        return;
    }

    if (Status == TaskStatus.Archived)
    {
        throw new DomainException("Archived tasks cannot be marked as completed.");
    }

    Status = TaskStatus.Completed;
    AddHistoryEntry("Completed", BuildCompletedMessage());
}
```

Why this matters:

- task state cannot be changed arbitrarily
- business rules stay close to the data they protect
- history tracking happens automatically when state changes

### Example: Inheritance in the Domain

The system uses inheritance to model specialized task types:

```csharp
public sealed class WorkTask : BaseTask
{
    public override string TaskType => "Work";

    protected override string BuildPriorityChangedMessage(
        TaskPriority previousPriority,
        TaskPriority newPriority)
    {
        return $"Work task priority updated from {previousPriority} to {newPriority}.";
    }
}
```

This is a good use of inheritance because:

- all task types share common behavior through `BaseTask`
- derived types can customize messages or future behavior
- the hierarchy remains simple and understandable

### Example: User as Aggregate Root for Task Creation

The `User` entity creates tasks that belong to that user:

```csharp
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
```

This keeps ownership rules inside the domain model instead of scattering them across the application.

## Application Layer

### Responsibility

The `Application` layer coordinates use cases.

It answers questions like:

- How do we create a task for a given user?
- How do we list tasks in a UI-friendly format?
- How do we filter overdue tasks?
- What repositories and services are needed to complete the operation?

It does **not** contain:

- console input/output
- storage implementation details

### Main Types

- `TaskService`
- `UserService`
- `TaskSummary`
- `HighPriorityTaskFilter`
- `StatusTaskFilter`
- `OverdueTaskFilter`
- `IDateTimeProvider`

### Example: Application Orchestration

`TaskService` coordinates work between repositories, domain entities, and notifications:

```csharp
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
```

This method is a good example of application-layer responsibility:

1. load the correct domain object
2. call the domain behavior
3. persist changes
4. notify the outside world
5. return a DTO for the UI

### Example: DTO Mapping

The UI does not work directly with full domain objects when listing tasks. Instead, the application exposes a simpler read model:

```csharp
private TaskSummary CreateSummary(BaseTask task, DateTime? referenceDate = null)
{
    return TaskSummary.FromTask(task, referenceDate ?? _dateTimeProvider.UtcNow);
}
```

This keeps the UI simpler and avoids coupling the presentation layer to the full domain model.

### Example: Reusable Filtering

The application keeps filtering logic in small, focused classes:

```csharp
public sealed class OverdueTaskFilter
{
    public IReadOnlyCollection<BaseTask> Apply(IEnumerable<BaseTask> tasks, DateTime referenceDate)
    {
        ArgumentNullException.ThrowIfNull(tasks);

        return tasks
            .Where(task => task.IsOverdue(referenceDate))
            .ToList();
    }
}
```

This keeps `TaskService` readable and makes filtering behavior reusable.

## Infrastructure Layer

### Responsibility

The `Infrastructure` layer provides concrete implementations for technical concerns.

In this project it includes:

- in-memory repositories
- console notifications
- the system time provider

### Why this layer exists

Without this layer, the application would be directly tied to:

- `List<T>` storage
- console output
- `DateTime.UtcNow`

By isolating those details, the core application remains easier to evolve.

### Example: Generic In-Memory Repository

The base repository is generic and reusable:

```csharp
public class InMemoryRepository<T> : IRepository<T> where T : BaseEntity
{
    private readonly List<T> _entities;
    private readonly object _syncRoot = new();

    public virtual Task<IReadOnlyCollection<T>> ListAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(GetAll());
    }
}
```

This provides:

- storage in a simple list
- basic CRUD support
- thread-safe access through locking

### Example: Specialized Repository

`TaskRepository` builds task-specific queries on top of the generic repository:

```csharp
public Task<IReadOnlyCollection<BaseTask>> GetByUserIdAsync(
    Guid userId,
    CancellationToken cancellationToken = default)
{
    return QueryTasksAsync(
        task => task.UserId == userId,
        cancellationToken);
}
```

This is a good example of extending a generic component with domain-specific queries.

### Example: Notification Implementation

The `Application` layer depends on `INotificationService`, and Infrastructure provides concrete behavior such as console output.

This means the project can later switch to:

- file logging
- email notifications
- desktop popups

without changing the application use cases.

## UI Layer

### Responsibility

The `UI` layer handles:

- menus
- prompts
- navigation flow
- formatted output

It should not contain core business rules.

### Main Types

- `Program`
- `Menu`
- `ConsoleRenderer`
- `DashboardSummary`

### Example: Composition Root

`Program.cs` wires the application together:

```csharp
IDateTimeProvider dateTimeProvider = new SystemDateTimeProvider();
IRepository<User> userRepository = new UserRepository();
ITaskRepository taskRepository = new TaskRepository();

TaskService taskService = CreateTaskService(
    taskRepository,
    userRepository,
    new ConsoleNotificationService(),
    dateTimeProvider,
    highPriorityTaskFilter,
    statusTaskFilter,
    overdueTaskFilter);
```

This file acts as the **composition root**, meaning:

- all concrete implementations are created here
- dependencies are connected here
- business logic is kept out of `Program.cs`

### Example: UI-Oriented Navigation

The menu handles user interaction but delegates real work to services:

```csharp
private async Task CreateTaskAsync()
{
    User? user = await GetWorkingUserAsync();
    if (user is null)
    {
        return;
    }

    TaskInput taskInput = ReadTaskInput();
    TaskSummary task = await CreateTaskForUserAsync(user, taskInput);

    _renderer.RenderTasks(new List<TaskSummary> { task }, "Created Task");
    Pause();
}
```

The UI:

- reads input
- calls application services
- renders the result

The UI does **not** directly mutate domain objects or query infrastructure collections.

### Example: Dashboard Summary

The portfolio-oriented dashboard is a UI concern built from application DTOs:

```csharp
public sealed record DashboardSummary(
    int UserCount,
    int TotalTasks,
    int PendingTasks,
    int CompletedTasks,
    int ArchivedTasks,
    int OverdueTasks)
{
    public static DashboardSummary FromTasks(int userCount, IEnumerable<TaskSummary> tasks)
    {
        TaskSummary[] taskArray = tasks.ToArray();

        return new DashboardSummary(
            userCount,
            taskArray.Length,
            taskArray.Count(task => task.Status == TaskStatus.Pending),
            taskArray.Count(task => task.Status == TaskStatus.Completed),
            taskArray.Count(task => task.Status == TaskStatus.Archived),
            taskArray.Count(task => task.IsOverdue));
    }
}
```

This is presentation-oriented aggregation, so it belongs in the UI layer.

## End-to-End Request Flow

The best way to understand the architecture is to follow one use case from start to finish.

### Example: Mark a Task as Completed

#### 1. UI

The user selects the menu option and picks a task.

`Menu` calls:

```csharp
await _taskService.MarkTaskAsCompletedAsync(user.Id, task.Id);
```

#### 2. Application

`TaskService` loads the task, invokes the domain behavior, saves it, and notifies:

```csharp
BaseTask task = await LoadTaskForUserAsync(userId, taskId, cancellationToken);
task.MarkAsCompleted();

await _taskRepository.UpdateAsync(task, cancellationToken);
_notificationService.NotifyTaskCompleted(task);
```

#### 3. Domain

The domain entity enforces the business rule:

```csharp
public void MarkAsCompleted()
{
    if (Status == TaskStatus.Completed)
    {
        return;
    }

    if (Status == TaskStatus.Archived)
    {
        throw new DomainException("Archived tasks cannot be marked as completed.");
    }

    Status = TaskStatus.Completed;
    AddHistoryEntry("Completed", BuildCompletedMessage());
}
```

#### 4. Infrastructure

The repository updates the in-memory store.

#### 5. UI

The renderer prints the updated task and the user sees confirmation.

## Why This Architecture Works Well

### It is maintainable

If you want to replace in-memory storage with a database, most of the system does not change.

### It is testable

The business logic is not buried inside console code.

### It is educational

Each layer shows a clear role:

- entity behavior
- service orchestration
- repository implementation
- presentation logic

### It is portfolio-ready

It demonstrates:

- OOP
- layering
- contracts and implementations
- state transitions
- user-focused console design

## Tradeoffs and Simplifications

This project is intentionally simple in a few areas:

- dependency injection is done manually in `Program.cs`
- storage is in memory only
- repository contracts live close to the domain model for simplicity
- the console UI is synchronous in feel, even though the service layer uses async APIs

These are reasonable choices for a learning and portfolio project.

## Possible Future Improvements

The current architecture supports several natural upgrades:

### 1. Replace In-Memory Storage

Swap `UserRepository` and `TaskRepository` with:

- `EF Core`
- `SQLite`
- `SQL Server`

### 2. Add Another Presentation Layer

Reuse the same `Domain` and `Application` layers in:

- ASP.NET Core Web API
- Blazor
- WPF
- WinUI

### 3. Add Dependency Injection Container

Move object wiring from `Program.cs` into a DI container such as the built-in .NET host.

## Summary

SmartTaskManager uses a practical layered architecture where:

- the `Domain` protects business rules
- the `Application` coordinates use cases
- the `Infrastructure` implements technical details
- the `UI` delivers the console experience

This design keeps the code readable, modular, and ready for future growth while still being simple enough for learning and demonstration purposes.
