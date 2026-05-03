using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SmartTaskManager.Application.Abstractions.Persistence;
using SmartTaskManager.Domain.Common;
using SmartTaskManager.Domain.Entities;

namespace SmartTaskManager.Application.Services;

public sealed class UserService
{
    private readonly IRepository<User> _userRepository;

    public UserService(IRepository<User> userRepository)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
    }

    public async Task<User> CreateUserAsync(string userName, CancellationToken cancellationToken = default)
    {
        string normalizedUserName = ValidateUserName(userName);
        await EnsureUserNameIsAvailableAsync(normalizedUserName, cancellationToken);

        User user = new(normalizedUserName);
        await _userRepository.AddAsync(user, cancellationToken);

        return user;
    }

    public async Task<User> GetUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        EnsureIdentifierProvided(userId, "User id is required.");

        User? user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            throw new DomainException("User not found.");
        }

        return user;
    }

    public async Task<User> GetUserByNameAsync(string userName, CancellationToken cancellationToken = default)
    {
        string normalizedUserName = ValidateUserName(userName);
        IReadOnlyCollection<User> users = await _userRepository.ListAsync(cancellationToken);

        User? user = users.FirstOrDefault(existingUser =>
            string.Equals(existingUser.UserName, normalizedUserName, StringComparison.OrdinalIgnoreCase));

        if (user is null)
        {
            throw new DomainException("User not found.");
        }

        return user;
    }

    public async Task<IReadOnlyCollection<User>> ListUsersAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<User> users = await _userRepository.ListAsync(cancellationToken);

        return users
            .OrderBy(user => user.UserName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public async Task<bool> UserExistsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        EnsureIdentifierProvided(userId, "User id is required.");
        return await _userRepository.GetByIdAsync(userId, cancellationToken) is not null;
    }

    private async Task EnsureUserNameIsAvailableAsync(string userName, CancellationToken cancellationToken)
    {
        IReadOnlyCollection<User> users = await _userRepository.ListAsync(cancellationToken);
        bool exists = users.Any(existingUser =>
            string.Equals(existingUser.UserName, userName, StringComparison.OrdinalIgnoreCase));

        if (exists)
        {
            throw new DomainException("A user with the same name already exists.");
        }
    }

    private static string ValidateUserName(string userName)
    {
        if (string.IsNullOrWhiteSpace(userName))
        {
            throw new DomainException("User name is required.");
        }

        return userName.Trim();
    }

    private static void EnsureIdentifierProvided(Guid id, string message)
    {
        if (id == Guid.Empty)
        {
            throw new DomainException(message);
        }
    }
}
