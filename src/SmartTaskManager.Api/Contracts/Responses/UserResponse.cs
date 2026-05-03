using System;
using System.Collections.Generic;
using System.Linq;
using SmartTaskManager.Domain.Entities;

namespace SmartTaskManager.Api.Contracts.Responses;

public sealed record UserResponse(
    Guid Id,
    string UserName,
    DateTime CreatedOnUtc)
{
    public static UserResponse FromDomain(User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        return new UserResponse(
            user.Id,
            user.UserName,
            user.CreatedOnUtc);
    }

    public static IReadOnlyCollection<UserResponse> FromDomain(IEnumerable<User> users)
    {
        ArgumentNullException.ThrowIfNull(users);

        return users
            .Select(FromDomain)
            .ToList();
    }
}
