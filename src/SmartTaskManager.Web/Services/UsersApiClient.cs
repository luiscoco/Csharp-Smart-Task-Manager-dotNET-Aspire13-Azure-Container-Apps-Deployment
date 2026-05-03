using SmartTaskManager.Web.Models;
using SmartTaskManager.Web.Models.Requests;

namespace SmartTaskManager.Web.Services;

public sealed class UsersApiClient : ApiClientBase
{
    public UsersApiClient(HttpClient httpClient, SmartTaskManagerApiAccessTokenProvider accessTokenProvider)
        : base(httpClient, accessTokenProvider)
    {
    }

    public async Task<IReadOnlyCollection<UserSummary>> ListUsersAsync(CancellationToken cancellationToken = default)
    {
        return await GetAsync<List<UserSummary>>("api/users", cancellationToken);
    }

    public Task<UserSummary> GetUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return GetAsync<UserSummary>($"api/users/{userId}", cancellationToken);
    }

    public Task<UserSummary> CreateUserAsync(
        CreateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return PostAsync<CreateUserRequest, UserSummary>("api/users", request, cancellationToken);
    }
}
