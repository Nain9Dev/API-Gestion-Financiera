using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using PolicyOperations.Application.Policies;
using PolicyOperations.Domain.Policies;

namespace PolicyOperations.Api.Errors;

public sealed partial class ApiExceptionHandler : IExceptionHandler
{
    private readonly ILogger<ApiExceptionHandler> _logger;

    public ApiExceptionHandler(ILogger<ApiExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var error = Map(exception);

        if (error.Status >= StatusCodes.Status500InternalServerError)
        {
            LogUnhandledException(
                _logger,
                httpContext.TraceIdentifier,
                exception);
        }
        else
        {
            LogRequestRejected(
                _logger,
                error.Code,
                httpContext.TraceIdentifier);
        }

        var problemDetails = new ProblemDetails
        {
            Type = error.Type,
            Status = error.Status,
            Title = error.Title,
            Detail = error.Detail,
            Instance = httpContext.Request.Path
        };
        problemDetails.Extensions["code"] = error.Code;
        problemDetails.Extensions["traceId"] = httpContext.TraceIdentifier;

        httpContext.Response.StatusCode = error.Status;
        await httpContext.Response.WriteAsJsonAsync(
            problemDetails,
            options: null,
            contentType: "application/problem+json",
            cancellationToken: cancellationToken);

        return true;
    }

    private static ApiError Map(Exception exception)
    {
        return exception switch
        {
            DomainRuleViolationException { Code: "policy_transition_invalid" }
                transitionException => new ApiError(
                "https://tools.ietf.org/html/rfc9110#section-15.5.10",
                StatusCodes.Status409Conflict,
                "Policy transition conflict.",
                transitionException.Message,
                transitionException.Code),
            DomainRuleViolationException domainException => new ApiError(
                "https://tools.ietf.org/html/rfc9110#section-15.5.1",
                StatusCodes.Status400BadRequest,
                "Business rule validation failed.",
                domainException.Message,
                domainException.Code),
            RequestValidationException requestException => new ApiError(
                "https://tools.ietf.org/html/rfc9110#section-15.5.1",
                StatusCodes.Status400BadRequest,
                "Request validation failed.",
                requestException.Message,
                requestException.Code),
            PolicyNotFoundException => new ApiError(
                "https://tools.ietf.org/html/rfc9110#section-15.5.5",
                StatusCodes.Status404NotFound,
                "Policy not found.",
                "The requested policy does not exist.",
                "policy_not_found"),
            PolicyNumberConflictException => new ApiError(
                "https://tools.ietf.org/html/rfc9110#section-15.5.10",
                StatusCodes.Status409Conflict,
                "Policy number conflict.",
                "A policy with the same normalized policy number already exists.",
                "policy_number_conflict"),
            PolicyConcurrencyException => new ApiError(
                "https://tools.ietf.org/html/rfc9110#section-15.5.13",
                StatusCodes.Status412PreconditionFailed,
                "Policy version conflict.",
                "The policy changed after the supplied version was read. Reload it and retry.",
                "concurrency_conflict"),
            PolicyPreconditionRequiredException => new ApiError(
                "https://tools.ietf.org/html/rfc6585#section-3",
                StatusCodes.Status428PreconditionRequired,
                "Precondition required.",
                "The If-Match header is required for this operation.",
                "precondition_required"),
            _ => new ApiError(
                "https://tools.ietf.org/html/rfc9110#section-15.6.1",
                StatusCodes.Status500InternalServerError,
                "Unexpected error.",
                "An unexpected error occurred.",
                "internal_error")
        };
    }

    [LoggerMessage(
        EventId = 1000,
        Level = LogLevel.Error,
        Message = "Unhandled API exception. TraceId: {TraceId}")]
    private static partial void LogUnhandledException(
        ILogger logger,
        string traceId,
        Exception exception);

    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Warning,
        Message = "API request rejected with code {ErrorCode}. TraceId: {TraceId}")]
    private static partial void LogRequestRejected(
        ILogger logger,
        string errorCode,
        string traceId);

    private sealed record ApiError(
        string Type,
        int Status,
        string Title,
        string Detail,
        string Code);
}
