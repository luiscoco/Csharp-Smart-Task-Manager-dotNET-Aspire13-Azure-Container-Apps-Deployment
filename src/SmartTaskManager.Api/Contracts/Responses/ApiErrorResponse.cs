using System;
using System.Collections.Generic;

namespace SmartTaskManager.Api.Contracts.Responses;

public sealed record ApiErrorResponse(
    int StatusCode,
    string Title,
    string Detail,
    string TraceId,
    string Path,
    IReadOnlyDictionary<string, string[]>? Errors = null)
{
    public static ApiErrorResponse Create(
        int statusCode,
        string title,
        string detail,
        string traceId,
        string path)
    {
        return new ApiErrorResponse(
            statusCode,
            title,
            detail,
            traceId,
            path);
    }

    public static ApiErrorResponse Validation(
        string traceId,
        string path,
        IReadOnlyDictionary<string, string[]> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);

        return new ApiErrorResponse(
            StatusCode: 400,
            Title: "Validation failed.",
            Detail: "One or more validation errors occurred.",
            TraceId: traceId,
            Path: path,
            Errors: errors);
    }
}
