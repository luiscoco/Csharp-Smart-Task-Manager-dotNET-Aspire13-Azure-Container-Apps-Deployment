namespace SmartTaskManager.Web.Services;

public sealed class SmartTaskManagerApiAccessTokenException : Exception
{
    public SmartTaskManagerApiAccessTokenException(string message)
        : base(message)
    {
    }

    public SmartTaskManagerApiAccessTokenException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
