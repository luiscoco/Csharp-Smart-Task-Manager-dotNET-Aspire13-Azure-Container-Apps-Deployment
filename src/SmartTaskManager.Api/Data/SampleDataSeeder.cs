using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SmartTaskManager.Application.Abstractions.Services;
using SmartTaskManager.Application.DTOs;
using SmartTaskManager.Application.Services;
using SmartTaskManager.Domain.Entities;
using SmartTaskManager.Domain.Enums;

namespace SmartTaskManager.Api.Data;

public sealed class SampleDataSeeder
{
    private readonly UserService _userService;
    private readonly TaskService _taskService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public SampleDataSeeder(
        UserService userService,
        TaskService taskService,
        IDateTimeProvider dateTimeProvider)
    {
        _userService = userService ?? throw new ArgumentNullException(nameof(userService));
        _taskService = taskService ?? throw new ArgumentNullException(nameof(taskService));
        _dateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<User> existingUsers = await _userService.ListUsersAsync(cancellationToken);
        if (existingUsers.Count > 0)
        {
            return;
        }

        User alice = await _userService.CreateUserAsync("Alice", cancellationToken);
        User bob = await _userService.CreateUserAsync("Bob", cancellationToken);
        User carla = await _userService.CreateUserAsync("Carla", cancellationToken);

        await _taskService.CreatePersonalTaskAsync(
            alice.Id,
            "Buy groceries",
            "Milk, fruit, and bread.",
            CreateUtcDueDate(1),
            TaskPriority.Medium,
            "Home",
            cancellationToken);

        await _taskService.CreateWorkTaskAsync(
            alice.Id,
            "Prepare sprint review",
            "Finalize demo notes for the team.",
            CreateUtcDueDate(-2),
            TaskPriority.High,
            "Work",
            cancellationToken);

        TaskSummary aliceLinqPractice = await _taskService.CreateLearningTaskAsync(
            alice.Id,
            "Complete LINQ exercises",
            "Work through sequence and projection exercises.",
            CreateUtcDueDate(-1),
            TaskPriority.Medium,
            "Study",
            cancellationToken);

        await _taskService.CreateWorkTaskAsync(
            bob.Id,
            "Submit expense report",
            "Upload receipts before the finance deadline.",
            CreateUtcDueDate(0),
            TaskPriority.Critical,
            "Finance",
            cancellationToken);

        TaskSummary bobGymMembership = await _taskService.CreatePersonalTaskAsync(
            bob.Id,
            "Renew gym membership",
            "Decide whether to renew for another 3 months.",
            CreateUtcDueDate(-7),
            TaskPriority.Low,
            "Personal Admin",
            cancellationToken);

        await _taskService.CreateLearningTaskAsync(
            bob.Id,
            "Watch async/await course",
            "Finish the module on cancellation tokens and task coordination.",
            CreateUtcDueDate(4),
            TaskPriority.Low,
            "Study",
            cancellationToken);

        await _taskService.CreateWorkTaskAsync(
            carla.Id,
            "Prepare portfolio walkthrough",
            "Outline the architecture story for the demo interview.",
            CreateUtcDueDate(2),
            TaskPriority.High,
            "Portfolio",
            cancellationToken);

        TaskSummary carlaApiTutorial = await _taskService.CreateLearningTaskAsync(
            carla.Id,
            "Finish API tutorial",
            "Wrap up the REST API chapter and notes.",
            CreateUtcDueDate(-3),
            TaskPriority.Medium,
            "Study",
            cancellationToken);

        TaskSummary carlaTravelArchive = await _taskService.CreatePersonalTaskAsync(
            carla.Id,
            "Archive old travel plan",
            "Keep the notes but remove the task from the active list.",
            CreateUtcDueDate(-10),
            TaskPriority.Low,
            "Travel",
            cancellationToken);

        await _taskService.MarkTaskAsCompletedAsync(alice.Id, aliceLinqPractice.Id, cancellationToken);
        await _taskService.ArchiveTaskAsync(bob.Id, bobGymMembership.Id, cancellationToken);
        await _taskService.MarkTaskAsCompletedAsync(carla.Id, carlaApiTutorial.Id, cancellationToken);
        await _taskService.ArchiveTaskAsync(carla.Id, carlaTravelArchive.Id, cancellationToken);
    }

    private DateTime CreateUtcDueDate(int daysOffset)
    {
        DateTime date = _dateTimeProvider.UtcNow.Date.AddDays(daysOffset);
        return DateTime.SpecifyKind(date.AddHours(23).AddMinutes(59), DateTimeKind.Utc);
    }
}
