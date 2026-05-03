namespace SmartTaskManager.Web.Models;

public sealed record TaskDashboardSummary(
    int TotalTasks,
    int PendingTasks,
    int InProgressTasks,
    int CompletedTasks,
    int ArchivedTasks,
    int OverdueTasks);
