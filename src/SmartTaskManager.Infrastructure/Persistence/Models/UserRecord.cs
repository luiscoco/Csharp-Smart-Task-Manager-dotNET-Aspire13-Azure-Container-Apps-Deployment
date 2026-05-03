using System;

namespace SmartTaskManager.Infrastructure.Persistence.Models;

public sealed class UserRecord
{
    public Guid Id { get; set; }

    public string UserName { get; set; } = string.Empty;

    public DateTime CreatedOnUtc { get; set; }
}
