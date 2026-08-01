using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace PolicyOperations.Api.Security;

public static class PolicyAuthorization
{
    public const string ReadPolicy = "PolicyRead";
    public const string WritePolicy = "PolicyWrite";
    public const string ReaderRole = "PolicyReader";
    public const string OperatorRole = "PolicyOperator";
    public const string OrganizationIdClaim = "organization_id";

    public static void Configure(AuthorizationOptions options)
    {
        options.FallbackPolicy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .Build();

        options.AddPolicy(
            ReadPolicy,
            policy => policy
                .RequireAuthenticatedUser()
                .RequireAssertion(context =>
                    HasValidIdentityContext(context.User) &&
                    (HasRole(context.User, ReaderRole) ||
                     HasRole(context.User, OperatorRole))));

        options.AddPolicy(
            WritePolicy,
            policy => policy
                .RequireAuthenticatedUser()
                .RequireAssertion(context =>
                    HasValidIdentityContext(context.User) &&
                    HasRole(context.User, OperatorRole)));
    }

    private static bool HasValidIdentityContext(ClaimsPrincipal user)
    {
        var subject = user.FindFirst("sub")?.Value ??
            user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var organizationId = user.FindFirst(OrganizationIdClaim)?.Value;

        return !string.IsNullOrWhiteSpace(subject) &&
            Guid.TryParse(organizationId, out var parsedOrganizationId) &&
            parsedOrganizationId != Guid.Empty;
    }

    private static bool HasRole(ClaimsPrincipal user, string role)
    {
        return user.IsInRole(role) ||
            user.HasClaim("role", role) ||
            user.HasClaim(ClaimTypes.Role, role);
    }
}
