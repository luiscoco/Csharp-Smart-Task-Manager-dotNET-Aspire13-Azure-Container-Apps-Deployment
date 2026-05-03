using System;
using SmartTaskManager.Application.Abstractions.Services;

namespace SmartTaskManager.Infrastructure.Time;

public sealed class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}
