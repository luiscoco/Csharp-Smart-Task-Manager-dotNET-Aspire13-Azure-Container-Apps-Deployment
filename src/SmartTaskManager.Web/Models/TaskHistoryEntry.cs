namespace SmartTaskManager.Web.Models;

public sealed record TaskHistoryEntry(
    DateTime OccurredOnUtc,
    string Action,
    string Details);
