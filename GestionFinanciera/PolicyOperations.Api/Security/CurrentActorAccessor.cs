using System.Security.Claims;
using PolicyOperations.Application.Policies;

namespace PolicyOperations.Api.Security;

public sealed class CurrentActorAccessor
{
    public Guid GetOrganizationId(ClaimsPrincipal user)
    {
        var organizationValue = user.FindFirst(PolicyAuthorization.OrganizationIdClaim)?.Value;

        if (!Guid.TryParse(organizationValue, out var organizationId) ||
            organizationId == Guid.Empty)
        {
            throw new InvalidOperationException(
                "The authorization policy did not provide a valid organization identifier.");
        }

        return organizationId;
    }

    public PolicyCommandContext GetCommandContext(
        ClaimsPrincipal user,
        string correlationId)
    {
        var subject = user.FindFirst("sub")?.Value ??
            user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrWhiteSpace(subject))
        {
            throw new InvalidOperationException(
                "The authorization policy did not provide a valid actor subject.");
        }

        return new PolicyCommandContext(
            GetOrganizationId(user),
            subject,
            correlationId);
    }
}
