using SmartTaskManager.Web.Models;

namespace SmartTaskManager.Web.Services;

public sealed class SmartTaskManagerApiException : Exception
{
    public SmartTaskManagerApiException(
        int statusCode,
        string message,
        ApiErrorDetails? error)
        : base(message)
    {
        StatusCode = statusCode;
        Error = error;
    }

    public int StatusCode { get; }

    public ApiErrorDetails? Error { get; }
}
