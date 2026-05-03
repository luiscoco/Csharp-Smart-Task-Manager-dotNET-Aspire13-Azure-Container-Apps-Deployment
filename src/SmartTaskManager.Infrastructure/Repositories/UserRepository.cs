using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SmartTaskManager.Application.Abstractions.Persistence;
using SmartTaskManager.Domain.Common;
using SmartTaskManager.Domain.Entities;
using SmartTaskManager.Infrastructure.Persistence;
using SmartTaskManager.Infrastructure.Persistence.Models;

namespace SmartTaskManager.Infrastructure.Repositories;

public sealed class UserRepository : IRepository<User>
{
    private readonly SmartTaskManagerDbContextFactory _dbContextFactory;

    public UserRepository(SmartTaskManagerDbContextFactory dbContextFactory)
    {
        _dbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));
    }

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            return null;
        }

        await using SmartTaskManagerDbContext dbContext = _dbContextFactory.CreateDbContext();
        UserRecord? record = await dbContext.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(user => user.Id == id, cancellationToken);

        return record is null
            ? null
            : MapToDomain(record);
    }

    public async Task<IReadOnlyCollection<User>> ListAsync(CancellationToken cancellationToken = default)
    {
        await using SmartTaskManagerDbContext dbContext = _dbContextFactory.CreateDbContext();

        List<UserRecord> records = await dbContext.Users
            .AsNoTracking()
            .OrderBy(user => user.UserName)
            .ToListAsync(cancellationToken);

        return records
            .Select(MapToDomain)
            .ToList();
    }

    public async Task AddAsync(User entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        await using SmartTaskManagerDbContext dbContext = _dbContextFactory.CreateDbContext();

        bool alreadyExists = await dbContext.Users
            .AnyAsync(user => user.Id == entity.Id, cancellationToken);

        if (alreadyExists)
        {
            throw new DomainException($"A user with id '{entity.Id}' already exists.");
        }

        dbContext.Users.Add(MapToRecord(entity));
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(User entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        await using SmartTaskManagerDbContext dbContext = _dbContextFactory.CreateDbContext();

        UserRecord? record = await dbContext.Users
            .SingleOrDefaultAsync(user => user.Id == entity.Id, cancellationToken);

        if (record is null)
        {
            throw new DomainException($"User with id '{entity.Id}' was not found.");
        }

        record.UserName = entity.UserName;
        record.CreatedOnUtc = NormalizeUtc(entity.CreatedOnUtc);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        EnsureIdentifierProvided(id);

        await using SmartTaskManagerDbContext dbContext = _dbContextFactory.CreateDbContext();

        UserRecord? record = await dbContext.Users
            .SingleOrDefaultAsync(user => user.Id == id, cancellationToken);

        if (record is null)
        {
            throw new DomainException($"User with id '{id}' was not found.");
        }

        dbContext.Users.Remove(record);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static User MapToDomain(UserRecord record)
    {
        return new User(
            record.Id,
            record.UserName,
            createdOnUtc: NormalizeUtc(record.CreatedOnUtc));
    }

    private static UserRecord MapToRecord(User user)
    {
        return new UserRecord
        {
            Id = user.Id,
            UserName = user.UserName,
            CreatedOnUtc = NormalizeUtc(user.CreatedOnUtc)
        };
    }

    private static DateTime NormalizeUtc(DateTime value)
    {
        return value.Kind == DateTimeKind.Utc
            ? value
            : DateTime.SpecifyKind(value, DateTimeKind.Utc);
    }

    private static void EnsureIdentifierProvided(Guid id)
    {
        if (id == Guid.Empty)
        {
            throw new DomainException("A valid identifier is required.");
        }
    }
}
