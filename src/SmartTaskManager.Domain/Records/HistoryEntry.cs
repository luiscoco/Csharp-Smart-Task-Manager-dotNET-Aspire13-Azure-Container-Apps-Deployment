using System;
using SmartTaskManager.Domain.Common;

namespace SmartTaskManager.Domain.Records;

public sealed record HistoryEntry
{
    public HistoryEntry(DateTime occurredOnUtc, string action, string details)
    {
        if (occurredOnUtc == default)
        {
            throw new DomainException("History entry date is required.");
        }

        if (string.IsNullOrWhiteSpace(action))
        {
            throw new DomainException("History entry action is required.");
        }

        if (string.IsNullOrWhiteSpace(details))
        {
            throw new DomainException("History entry details are required.");
        }

        OccurredOnUtc = occurredOnUtc;
        Action = action.Trim();
        Details = details.Trim();
    }

    public DateTime OccurredOnUtc { get; }

    public string Action { get; }

    public string Details { get; }

    public static HistoryEntry Create(string action, string details)
    {
        return new HistoryEntry(DateTime.UtcNow, action, details);
    }
}
