using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace PolicyOperations.IntegrationTests;

public sealed class TestAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "IntegrationTest";
    public const string SubjectHeader = "X-Test-Subject";
    public const string OrganizationHeader = "X-Test-Organization";
    public const string RoleHeader = "X-Test-Role";

    public TestAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var subject = Request.Headers[SubjectHeader].ToString();
        var organizationId = Request.Headers[OrganizationHeader].ToString();
        var role = Request.Headers[RoleHeader].ToString();

        if (string.IsNullOrWhiteSpace(subject))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, subject),
            new Claim("organization_id", organizationId),
            new Claim(ClaimTypes.Role, role)
        };
        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    public static void Authenticate(
        HttpClient client,
        Guid organizationId,
        string role,
        string subject = "integration-operator")
    {
        client.DefaultRequestHeaders.Add(SubjectHeader, subject);
        client.DefaultRequestHeaders.Add(OrganizationHeader, organizationId.ToString());
        client.DefaultRequestHeaders.Add(RoleHeader, role);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(SchemeName, "synthetic-test-ticket");
    }
}
