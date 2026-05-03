namespace SmartTaskManager.Web.Configuration;

public static class AuthorizationPolicies
{
    /// <summary>
    /// Requires the user to be have the 'Admin' app role.
    /// Used for sensitive operations like user management and archiving.
    /// </summary>
    public const string RequireAdminRole = "RequireAdminRole";

    /// <summary>
    /// Requires the user to have either 'Admin' or 'Editor' app role.
    /// Used for creating and updating tasks.
    /// </summary>
    public const string RequireEditorRole = "RequireEditorRole";
}
