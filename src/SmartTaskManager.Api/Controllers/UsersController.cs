using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SmartTaskManager.Api.Contracts.Requests;
using SmartTaskManager.Api.Contracts.Responses;
using SmartTaskManager.Api.Security;
using SmartTaskManager.Application.Services;
using SmartTaskManager.Domain.Entities;

namespace SmartTaskManager.Api.Controllers;

/// <summary>
/// Manages user creation and user lookups.
/// </summary>
[ApiController]
[Authorize]
[Route("api/users")]
[Produces("application/json")]
public sealed class UsersController : ControllerBase
{
    private readonly UserService _userService;

    public UsersController(UserService userService)
    {
        _userService = userService ?? throw new ArgumentNullException(nameof(userService));
    }

    /// <summary>
    /// Creates a new local user profile.
    /// Requires Admin role.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.RequireAdminRole)]
    [ProducesResponseType(typeof(UserResponse), 201)]
    [ProducesResponseType(typeof(ApiErrorResponse), 400)]
    public async Task<ActionResult<UserResponse>> CreateUser(
        [FromBody] CreateUserRequest request,
        CancellationToken cancellationToken)
    {
        User user = await _userService.CreateUserAsync(
            request.UserName.Trim(),
            cancellationToken);

        return CreatedAtAction(
            nameof(GetUser),
            new { userId = user.Id },
            UserResponse.FromDomain(user));
    }

    /// <summary>
    /// Returns all available local user profiles.
    /// Accessible to any authenticated user to facilitate UI profile switching.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<UserResponse>), 200)]
    public async Task<ActionResult<IReadOnlyCollection<UserResponse>>> ListUsers(
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<User> users = await _userService.ListUsersAsync(cancellationToken);
        return Ok(UserResponse.FromDomain(users));
    }

    /// <summary>
    /// Returns a specific user profile.
    /// Accessible to any authenticated user.
    /// </summary>
    [HttpGet("{userId:guid}")]
    [ProducesResponseType(typeof(UserResponse), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 404)]
    public async Task<ActionResult<UserResponse>> GetUser(
        [FromRoute] Guid userId,
        CancellationToken cancellationToken)
    {
        User user = await _userService.GetUserAsync(userId, cancellationToken);
        return Ok(UserResponse.FromDomain(user));
    }
}
