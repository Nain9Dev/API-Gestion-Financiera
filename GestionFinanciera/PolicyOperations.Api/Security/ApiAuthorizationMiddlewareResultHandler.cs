using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Mvc;

namespace PolicyOperations.Api.Security;

public sealed class ApiAuthorizationMiddlewareResultHandler : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler _defaultHandler = new();

    public async Task HandleAsync(
        RequestDelegate next,
        HttpContext context,
        AuthorizationPolicy policy,
        PolicyAuthorizationResult authorizeResult)
    {
        if (authorizeResult.Challenged)
        {
            await WriteProblemAsync(
                context,
                StatusCodes.Status401Unauthorized,
                "https://tools.ietf.org/html/rfc9110#section-15.5.2",
                "Authentication required.",
                "A valid bearer token is required.",
                "authentication_required");
            return;
        }

        if (authorizeResult.Forbidden)
        {
            await WriteProblemAsync(
                context,
                StatusCodes.Status403Forbidden,
                "https://tools.ietf.org/html/rfc9110#section-15.5.4",
                "Access forbidden.",
                "The authenticated actor is not allowed to perform this operation.",
                "forbidden");
            return;
        }

        await _defaultHandler.HandleAsync(next, context, policy, authorizeResult);
    }

    private static Task WriteProblemAsync(
        HttpContext context,
        int status,
        string type,
        string title,
        string detail,
        string code)
    {
        var problemDetails = new ProblemDetails
        {
            Type = type,
            Status = status,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path
        };
        problemDetails.Extensions["code"] = code;
        problemDetails.Extensions["traceId"] = context.TraceIdentifier;

        context.Response.StatusCode = status;
        return context.Response.WriteAsJsonAsync(
            problemDetails,
            options: null,
            contentType: "application/problem+json");
    }
}
