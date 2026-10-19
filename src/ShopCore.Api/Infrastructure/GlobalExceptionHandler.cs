using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ShopCore.Api.Exceptions;

namespace ShopCore.Api.Infrastructure;

public sealed class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger,
    IProblemDetailsService problemDetails) : IExceptionHandler
{
    private static readonly Action<ILogger, string, Exception?> UnhandledException =
        LoggerMessage.Define<string>(
            LogLevel.Error,
            new EventId(5000, nameof(UnhandledException)),
            "Unhandled exception. TraceId {TraceId}");

    private static readonly Action<ILogger, int, string, Exception?> RequestFailure =
        LoggerMessage.Define<int, string>(
            LogLevel.Warning,
            new EventId(4000, nameof(RequestFailure)),
            "Request failed with status {StatusCode}. TraceId {TraceId}");

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
        {
            if (!httpContext.Response.HasStarted)
            {
                httpContext.Response.StatusCode = StatusCodes.Status499ClientClosedRequest;
            }

            return true;
        }

        var (status, title) = exception switch
        {
            ValidationException => (StatusCodes.Status400BadRequest, "The request is invalid."),
            BadHttpRequestException badRequest => (badRequest.StatusCode, "The request is malformed."),
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "Authentication is required."),
            ForbiddenException => (StatusCodes.Status403Forbidden, "Access to the resource is forbidden."),
            NotFoundException => (StatusCodes.Status404NotFound, "The requested resource was not found."),
            ConflictException => (StatusCodes.Status409Conflict, "The request conflicts with the current state."),
            UnprocessableEntityException => (StatusCodes.Status422UnprocessableEntity, "The request could not be processed."),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred.")
        };

        if (status >= StatusCodes.Status500InternalServerError)
        {
            UnhandledException(logger, httpContext.TraceIdentifier, exception);
        }
        else
        {
            RequestFailure(logger, status, httpContext.TraceIdentifier, null);
        }

        httpContext.Response.StatusCode = status;

        var problemDetailsModel = new ProblemDetails
        {
            Status = status,
            Title = title,
            Instance = httpContext.Request.Path,
            Extensions =
            {
                ["traceId"] = httpContext.TraceIdentifier
            }
        };

        if (exception is ValidationException { Errors.Count: > 0 } validationException)
        {
            problemDetailsModel.Extensions["errors"] = validationException.Errors;
        }

        var context = new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = problemDetailsModel
        };

        return await problemDetails.TryWriteAsync(context);
    }
}
