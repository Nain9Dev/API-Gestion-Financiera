using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PolicyOperations.Api.Security;
using PolicyOperations.Application.Policies;
using PolicyOperations.Domain.Policies;
using PolicyOperations.Infrastructure.Persistence;
using Xunit;

namespace PolicyOperations.IntegrationTests;

[SuppressMessage(
    "Design",
    "CA1001:Types that own disposable fields should be disposable",
    Justification = "xUnit invokes IAsyncLifetime.DisposeAsync after every test instance.")]
public sealed class PoliciesApiTests : IAsyncLifetime
{
    private const string DatabaseNamePrefix = "PolicyOperationsTests_";
    private static readonly Guid PrimaryOrganizationId =
        Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid OtherOrganizationId =
        Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();

    private WebApplicationFactory<Program>? _factory;
    private HttpClient? _client;
    private string? _databaseName;
    private string? _previousApiConnectionString;

    public async Task InitializeAsync()
    {
        var baseConnectionString = Environment.GetEnvironmentVariable(
            SqlServerFactAttribute.ConnectionStringEnvironmentVariable);

        if (string.IsNullOrWhiteSpace(baseConnectionString))
        {
            return;
        }

        var connectionStringBuilder = new SqlConnectionStringBuilder(baseConnectionString)
        {
            InitialCatalog = DatabaseNamePrefix + Guid.NewGuid().ToString("N")
        };
        _databaseName = connectionStringBuilder.InitialCatalog;

        _previousApiConnectionString = Environment.GetEnvironmentVariable(
            "ConnectionStrings__DefaultConnection");
        Environment.SetEnvironmentVariable(
            "ConnectionStrings__DefaultConnection",
            connectionStringBuilder.ConnectionString);

        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(ConfigureTestAuthentication);

        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PolicyOperationsDbContext>();
        await dbContext.Database.MigrateAsync();

        _client = CreateAuthenticatedClient(
            PrimaryOrganizationId,
            PolicyAuthorization.OperatorRole);
    }

    public async Task DisposeAsync()
    {
        if (_factory is null || string.IsNullOrWhiteSpace(_databaseName))
        {
            return;
        }

        if (!_databaseName.StartsWith(DatabaseNamePrefix, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "Refusing to delete a database outside the integration-test namespace.");
        }

        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PolicyOperationsDbContext>();
        await dbContext.Database.EnsureDeletedAsync();

        _client?.Dispose();
        await _factory.DisposeAsync();
        Environment.SetEnvironmentVariable(
            "ConnectionStrings__DefaultConnection",
            _previousApiConnectionString);
    }

    [SqlServerFact]
    public async Task UnauthenticatedPolicyRequestReturnsProblemDetails()
    {
        using var anonymousClient = Factory.CreateClient();

        var response = await anonymousClient.GetAsync("/api/v1/policies");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("authentication_required", await ReadProblemCodeAsync(response));
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [SqlServerFact]
    public async Task ReaderCannotCreatePolicy()
    {
        using var readerClient = CreateAuthenticatedClient(
            PrimaryOrganizationId,
            PolicyAuthorization.ReaderRole,
            "integration-reader");

        var response = await readerClient.PostAsJsonAsync(
            "/api/v1/policies",
            CreateRequest("SYNTH-READER-DENIED"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("forbidden", await ReadProblemCodeAsync(response));
    }

    [SqlServerFact]
    public async Task CreateThenGetReturnsScopedDraftWithEtag()
    {
        var createResponse = await Client.PostAsJsonAsync(
            "/api/v1/policies",
            CreateRequest("SYNTH-API-001", 125_000.25m));

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var createdPolicy = await ReadPolicyAsync(createResponse);
        Assert.Equal(PrimaryOrganizationId, createdPolicy.OrganizationId);
        Assert.Equal(PolicyStatus.Draft, createdPolicy.Status);
        Assert.Equal("SYNTH-API-001", createdPolicy.PolicyNumber);
        Assert.Equal("EUR", createdPolicy.Currency);
        Assert.False(string.IsNullOrWhiteSpace(createdPolicy.Version));
        Assert.Equal(
            $"/api/v1/policies/{createdPolicy.Id}",
            createResponse.Headers.Location?.AbsolutePath);
        Assert.Equal(PolicyEtag.Format(createdPolicy.Version), RequireEtag(createResponse));

        var getResponse = await Client.GetAsync($"/api/v1/policies/{createdPolicy.Id}");

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var persistedPolicy = await ReadPolicyAsync(getResponse);
        Assert.Equal(createdPolicy, persistedPolicy);
        Assert.Equal(RequireEtag(createResponse), RequireEtag(getResponse));
    }

    [SqlServerFact]
    public async Task PolicyNumberAndReadsAreIsolatedByOrganization()
    {
        var firstResponse = await Client.PostAsJsonAsync(
            "/api/v1/policies",
            CreateRequest("SYNTH-SHARED-NUMBER"));
        firstResponse.EnsureSuccessStatusCode();
        var primaryPolicy = await ReadPolicyAsync(firstResponse);

        using var otherClient = CreateAuthenticatedClient(
            OtherOrganizationId,
            PolicyAuthorization.OperatorRole,
            "other-operator");
        var otherCreateResponse = await otherClient.PostAsJsonAsync(
            "/api/v1/policies",
            CreateRequest(" synth-shared-number "));

        Assert.Equal(HttpStatusCode.Created, otherCreateResponse.StatusCode);
        var otherPolicy = await ReadPolicyAsync(otherCreateResponse);
        Assert.Equal(OtherOrganizationId, otherPolicy.OrganizationId);

        var crossOrganizationGet = await otherClient.GetAsync(
            $"/api/v1/policies/{primaryPolicy.Id}");
        Assert.Equal(HttpStatusCode.NotFound, crossOrganizationGet.StatusCode);

        var otherListResponse = await otherClient.GetAsync("/api/v1/policies");
        var otherPage = await otherListResponse.Content
            .ReadFromJsonAsync<PagedResult<PolicyResponse>>(JsonOptions);
        Assert.NotNull(otherPage);
        Assert.Single(otherPage.Items);
        Assert.Equal(otherPolicy.Id, otherPage.Items[0].Id);
    }

    [SqlServerFact]
    public async Task DuplicateNormalizedNumberWithinOrganizationReturnsConflict()
    {
        var firstResponse = await Client.PostAsJsonAsync(
            "/api/v1/policies",
            CreateRequest("SYNTH-DUPLICATE"));
        firstResponse.EnsureSuccessStatusCode();

        var duplicateResponse = await Client.PostAsJsonAsync(
            "/api/v1/policies",
            CreateRequest(" synth-duplicate ", 200m));

        Assert.Equal(HttpStatusCode.Conflict, duplicateResponse.StatusCode);
        Assert.Equal(
            "policy_number_conflict",
            await ReadProblemCodeAsync(duplicateResponse));
    }

    [SqlServerFact]
    public async Task CreateWithMaximumAmountPersistsExactValue()
    {
        var response = await Client.PostAsJsonAsync(
            "/api/v1/policies",
            CreateRequest("SYNTH-MAX-AMOUNT", Policy.MaxInsuredAmount));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var createdPolicy = await ReadPolicyAsync(response);
        Assert.Equal(Policy.MaxInsuredAmount, createdPolicy.InsuredAmount);
    }

    [SqlServerFact]
    public async Task UnsupportedCurrencyReturnsSafeValidationProblem()
    {
        var response = await Client.PostAsJsonAsync(
            "/api/v1/policies",
            CreateRequest("SYNTH-CURRENCY", currency: "JPY"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("currency_not_supported", await ReadProblemCodeAsync(response));
    }

    [SqlServerFact]
    public async Task CreateWithInvalidAmountReturnsSafeValidationProblem()
    {
        var response = await Client.PostAsJsonAsync(
            "/api/v1/policies",
            CreateRequest("SYNTH-INVALID", 0m));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("insured_amount_invalid", await ReadProblemCodeAsync(response));
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var content = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("stack", content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("SqlException", content, StringComparison.OrdinalIgnoreCase);
    }

    [SqlServerFact]
    public async Task GetMissingPolicyReturnsNotFoundProblem()
    {
        var response = await Client.GetAsync($"/api/v1/policies/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("policy_not_found", await ReadProblemCodeAsync(response));
    }

    [SqlServerFact]
    public async Task ListReturnsBoundedPageAndTotalCount()
    {
        await CreatePolicyAsync("SYNTH-LIST-002");
        await CreatePolicyAsync("SYNTH-LIST-001");

        var response = await Client.GetAsync(
            "/api/v1/policies?pageNumber=1&pageSize=1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = await response.Content.ReadFromJsonAsync<PagedResult<PolicyResponse>>(JsonOptions);
        Assert.NotNull(page);
        Assert.Single(page.Items);
        Assert.Equal(2, page.TotalCount);
        Assert.Equal("SYNTH-LIST-001", page.Items[0].PolicyNumber);
    }

    [SqlServerFact]
    public async Task ListWithOverflowingPageReturnsValidationProblem()
    {
        var response = await Client.GetAsync(
            "/api/v1/policies?pageNumber=2147483647&pageSize=100");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("validation_failed", await ReadProblemCodeAsync(response));
    }

    [SqlServerFact]
    public async Task ActivateThenCancelUsesConcurrencyAndAppendsAudit()
    {
        var createResponse = await Client.PostAsJsonAsync(
            "/api/v1/policies",
            CreateRequest("SYNTH-LIFECYCLE"));
        var createdPolicy = await ReadPolicyAsync(createResponse);
        var draftEtag = RequireEtag(createResponse);

        var activateResponse = await SendCommandAsync(
            $"/api/v1/policies/{createdPolicy.Id}/activate",
            new
            {
                insuredPartyReference = "SYNTH-INSURED-001",
                coverageStartDate = "2026-09-01",
                coverageEndDate = "2027-08-31"
            },
            draftEtag);

        Assert.Equal(HttpStatusCode.OK, activateResponse.StatusCode);
        var activePolicy = await ReadPolicyAsync(activateResponse);
        var activeEtag = RequireEtag(activateResponse);
        Assert.Equal(PolicyStatus.Active, activePolicy.Status);
        Assert.Equal("SYNTH-INSURED-001", activePolicy.InsuredPartyReference);
        Assert.NotEqual(draftEtag, activeEtag);

        var staleCancelResponse = await SendCommandAsync(
            $"/api/v1/policies/{createdPolicy.Id}/cancel",
            new { reason = "stale request" },
            draftEtag);
        Assert.Equal(HttpStatusCode.PreconditionFailed, staleCancelResponse.StatusCode);
        Assert.Equal("concurrency_conflict", await ReadProblemCodeAsync(staleCancelResponse));

        var cancelResponse = await SendCommandAsync(
            $"/api/v1/policies/{createdPolicy.Id}/cancel",
            new { reason = " Synthetic customer request " },
            activeEtag);
        Assert.Equal(HttpStatusCode.OK, cancelResponse.StatusCode);
        var cancelledPolicy = await ReadPolicyAsync(cancelResponse);
        Assert.Equal(PolicyStatus.Cancelled, cancelledPolicy.Status);

        var transitionsResponse = await Client.GetAsync(
            $"/api/v1/policies/{createdPolicy.Id}/transitions");
        transitionsResponse.EnsureSuccessStatusCode();
        var transitions = await transitionsResponse.Content
            .ReadFromJsonAsync<PolicyTransitionResponse[]>(JsonOptions);
        Assert.NotNull(transitions);
        Assert.Equal(2, transitions.Length);
        Assert.Equal(PolicyStatus.Draft, transitions[0].FromStatus);
        Assert.Equal(PolicyStatus.Active, transitions[0].ToStatus);
        Assert.Equal("integration-operator", transitions[0].ActorSubject);
        Assert.Equal(PolicyStatus.Active, transitions[1].FromStatus);
        Assert.Equal(PolicyStatus.Cancelled, transitions[1].ToStatus);
        Assert.Equal("Synthetic customer request", transitions[1].Reason);
    }

    [SqlServerFact]
    public async Task ActivationRequiresEtagAndCompleteData()
    {
        var createResponse = await Client.PostAsJsonAsync(
            "/api/v1/policies",
            CreateRequest("SYNTH-INCOMPLETE"));
        var createdPolicy = await ReadPolicyAsync(createResponse);

        var missingEtagResponse = await Client.PostAsJsonAsync(
            $"/api/v1/policies/{createdPolicy.Id}/activate",
            new
            {
                insuredPartyReference = "SYNTH-INSURED-002",
                coverageStartDate = "2026-09-01",
                coverageEndDate = "2027-08-31"
            });
        Assert.Equal((HttpStatusCode)428, missingEtagResponse.StatusCode);
        Assert.Equal("precondition_required", await ReadProblemCodeAsync(missingEtagResponse));

        var incompleteResponse = await SendCommandAsync(
            $"/api/v1/policies/{createdPolicy.Id}/activate",
            new
            {
                insuredPartyReference = " ",
                coverageStartDate = "2026-09-01",
                coverageEndDate = "2027-08-31"
            },
            RequireEtag(createResponse));
        Assert.Equal(HttpStatusCode.BadRequest, incompleteResponse.StatusCode);
        Assert.Equal(
            "insured_party_reference_required",
            await ReadProblemCodeAsync(incompleteResponse));

        var persistedResponse = await Client.GetAsync($"/api/v1/policies/{createdPolicy.Id}");
        var persistedPolicy = await ReadPolicyAsync(persistedResponse);
        Assert.Equal(PolicyStatus.Draft, persistedPolicy.Status);

        var transitionsResponse = await Client.GetAsync(
            $"/api/v1/policies/{createdPolicy.Id}/transitions");
        var transitions = await transitionsResponse.Content
            .ReadFromJsonAsync<PolicyTransitionResponse[]>(JsonOptions);
        Assert.NotNull(transitions);
        Assert.Empty(transitions);
    }

    [SqlServerFact]
    public async Task SwaggerExplainsQuotedEtagAndDocumentsJsonResponses()
    {
        using var anonymousClient = Factory.CreateClient();
        using var response = await anonymousClient.GetAsync("/swagger/v1/swagger.json");

        response.EnsureSuccessStatusCode();
        await using var contentStream = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(contentStream);
        var activateOperation = document.RootElement
            .GetProperty("paths")
            .GetProperty("/api/v1/policies/{policyId}/activate")
            .GetProperty("post");
        var ifMatch = activateOperation
            .GetProperty("parameters")
            .EnumerateArray()
            .Single(parameter => parameter.GetProperty("name").GetString() == "If-Match");

        Assert.True(ifMatch.GetProperty("required").GetBoolean());
        Assert.Equal("\"AAAAAAAAAAE=\"", ifMatch.GetProperty("example").GetString());
        Assert.Contains(
            "including quotation marks",
            ifMatch.GetProperty("description").GetString(),
            StringComparison.Ordinal);
        Assert.True(activateOperation
            .GetProperty("responses")
            .GetProperty("200")
            .GetProperty("content")
            .TryGetProperty("application/json", out _));
        Assert.True(activateOperation
            .GetProperty("responses")
            .GetProperty("400")
            .GetProperty("content")
            .TryGetProperty("application/problem+json", out _));
    }

    private HttpClient Client =>
        _client ?? throw new InvalidOperationException("The test client is not initialized.");

    private WebApplicationFactory<Program> Factory =>
        _factory ?? throw new InvalidOperationException("The test host is not initialized.");

    private static void ConfigureTestAuthentication(IWebHostBuilder webHostBuilder)
    {
        webHostBuilder.ConfigureTestServices(services =>
        {
            services
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = TestAuthenticationHandler.SchemeName;
                    options.DefaultChallengeScheme = TestAuthenticationHandler.SchemeName;
                    options.DefaultForbidScheme = TestAuthenticationHandler.SchemeName;
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(
                    TestAuthenticationHandler.SchemeName,
                    _ => { });
        });
    }

    private HttpClient CreateAuthenticatedClient(
        Guid organizationId,
        string role,
        string subject = "integration-operator")
    {
        var client = Factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });
        TestAuthenticationHandler.Authenticate(client, organizationId, role, subject);
        return client;
    }

    private async Task CreatePolicyAsync(string policyNumber)
    {
        var response = await Client.PostAsJsonAsync(
            "/api/v1/policies",
            CreateRequest(policyNumber));
        response.EnsureSuccessStatusCode();
    }

    private async Task<HttpResponseMessage> SendCommandAsync(
        string requestUri,
        object body,
        string etag)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, requestUri)
        {
            Content = JsonContent.Create(body)
        };
        request.Headers.TryAddWithoutValidation("If-Match", etag);
        return await Client.SendAsync(request);
    }

    private static object CreateRequest(
        string policyNumber,
        decimal insuredAmount = 100m,
        string currency = "EUR")
    {
        return new { policyNumber, insuredAmount, currency };
    }

    private static string RequireEtag(HttpResponseMessage response)
    {
        return response.Headers.ETag?.ToString() ??
            throw new InvalidOperationException("The response did not contain an ETag.");
    }

    private static async Task<PolicyResponse> ReadPolicyAsync(HttpResponseMessage response)
    {
        return await response.Content.ReadFromJsonAsync<PolicyResponse>(JsonOptions) ??
            throw new InvalidOperationException("The response did not contain a policy.");
    }

    private static async Task<string?> ReadProblemCodeAsync(HttpResponseMessage response)
    {
        await using var contentStream = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(contentStream);
        return document.RootElement.GetProperty("code").GetString();
    }

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
