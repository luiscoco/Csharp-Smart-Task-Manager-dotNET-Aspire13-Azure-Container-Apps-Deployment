namespace SmartTaskManager.Web.Models;

public sealed record ApiErrorDetails(
    int StatusCode,
    string Title,
    string Detail,
    string TraceId,
    string Path,
    Dictionary<string, string[]>? Errors);
