using System;
using System.Net.Mime;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using SmartTaskManager.Api.Contracts.Responses;
using SmartTaskManager.Domain.Common;

namespace SmartTaskManager.Api.Middleware;

public sealed class ApiExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiExceptionMiddleware> _logger;

    public ApiExceptionMiddleware(RequestDelegate next, ILogger<ApiExceptionMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task InvokeAsync(HttpContext httpContext)
    {
        try
        {
            await _next(httpContext);
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(httpContext, exception);
        }
    }

    private async Task HandleExceptionAsync(HttpContext httpContext, Exception exception)
    {
        if (httpContext.Response.HasStarted)
        {
            _logger.LogWarning(exception, "The response has already started, so the API error response could not be written.");
            throw exception;
        }

        int statusCode = ResolveStatusCode(exception);

        if (statusCode >= StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(exception, "Unhandled API exception.");
        }
        else
        {
            _logger.LogWarning(exception, "API request failed with status code {StatusCode}.", statusCode);
        }

        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = MediaTypeNames.Application.Json;

        ApiErrorResponse response = CreateErrorResponse(httpContext, exception, statusCode);

        await httpContext.Response.WriteAsJsonAsync(response);
    }

    private static ApiErrorResponse CreateErrorResponse(
        HttpContext httpContext,
        Exception exception,
        int statusCode)
    {
        string path = httpContext.Request.Path.HasValue
            ? httpContext.Request.Path.Value!
            : "/";

        return statusCode switch
        {
            StatusCodes.Status404NotFound => ApiErrorResponse.Create(
                statusCode,
                "Resource not found.",
                exception.Message,
                httpContext.TraceIdentifier,
                path),
            StatusCodes.Status409Conflict => ApiErrorResponse.Create(
                statusCode,
                "Conflict.",
                exception.Message,
                httpContext.TraceIdentifier,
                path),
            StatusCodes.Status403Forbidden => ApiErrorResponse.Create(
                statusCode,
                "Access denied.",
                "You do not have permission to perform this action.",
                httpContext.TraceIdentifier,
                path),
            StatusCodes.Status401Unauthorized => ApiErrorResponse.Create(
                statusCode,
                "Authentication required.",
                "A valid bearer token is required to access this resource.",
                httpContext.TraceIdentifier,
                path),
            StatusCodes.Status500InternalServerError => ApiErrorResponse.Create(
                statusCode,
                "Server error.",
                "An unexpected error occurred while processing the request.",
                httpContext.TraceIdentifier,
                path),
            _ => ApiErrorResponse.Create(
                statusCode,
                "Request failed.",
                exception.Message,
                httpContext.TraceIdentifier,
                path)
        };
    }

    private static int ResolveStatusCode(Exception exception)
    {
        return exception switch
        {
            DomainException domainException => ResolveDomainStatusCode(domainException.Message),
            ArgumentException => StatusCodes.Status400BadRequest,
            UnauthorizedAccessException => StatusCodes.Status403Forbidden,
            _ => StatusCodes.Status500InternalServerError
        };
    }

    private static int ResolveDomainStatusCode(string message)
    {
        string normalizedMessage = message.ToLowerInvariant();

        if (normalizedMessage.Contains("not found"))
        {
            return StatusCodes.Status404NotFound;
        }

        if (normalizedMessage.Contains("already exists"))
        {
            return StatusCodes.Status409Conflict;
        }

        return StatusCodes.Status400BadRequest;
    }
}
