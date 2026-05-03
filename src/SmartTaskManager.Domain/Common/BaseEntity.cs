using System;

namespace SmartTaskManager.Domain.Common;

public abstract class BaseEntity
{
    protected BaseEntity()
        : this(Guid.NewGuid())
    {
    }

    protected BaseEntity(Guid id)
    {
        Id = id == Guid.Empty ? Guid.NewGuid() : id;
    }

    public Guid Id { get; }
}
