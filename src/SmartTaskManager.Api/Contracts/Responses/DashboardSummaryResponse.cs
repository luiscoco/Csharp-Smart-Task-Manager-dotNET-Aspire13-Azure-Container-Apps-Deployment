using SmartTaskManager.Application.DTOs;

namespace SmartTaskManager.Api.Contracts.Responses;

public sealed record DashboardSummaryResponse(
    int TotalTasks,
    int PendingTasks,
    int InProgressTasks,
    int CompletedTasks,
    int ArchivedTasks,
    int OverdueTasks)
{
    public static DashboardSummaryResponse FromApplication(TaskDashboardSummary summary)
    {
        return new DashboardSummaryResponse(
            summary.TotalTasks,
            summary.PendingTasks,
            summary.InProgressTasks,
            summary.CompletedTasks,
            summary.ArchivedTasks,
            summary.OverdueTasks);
    }
}
