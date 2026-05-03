using SmartTaskManager.Web.Models;

namespace SmartTaskManager.Web.Services;

public sealed class UserSession
{
    public event Action? Changed;

    public UserSummary? CurrentUser { get; private set; }

    public bool HasActiveUser => CurrentUser is not null;

    public void Start(UserSummary user)
    {
        CurrentUser = user;
        Changed?.Invoke();
    }

    public void Clear()
    {
        CurrentUser = null;
        Changed?.Invoke();
    }
}
