using PolicyOperations.Application.Policies;
using PolicyOperations.Application.Tests.Fakes;
using PolicyOperations.Domain.Policies;
using Xunit;

namespace PolicyOperations.Application.Tests.Policies;

/// <summary>
/// Covers the application rules that do not need a database: deployment currency allow-list,
/// pagination limits, ETag shape, the stale-version pre-check and audit orchestration.
/// </summary>
public sealed class PolicyServiceTests
{
    private static readonly Guid OrganizationId =
        Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid OtherOrganizationId =
        Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly DateTimeOffset Now =
        new(2026, 9, 8, 12, 0, 0, TimeSpan.Zero);
    private static readonly byte[] CurrentVersion = [1, 0, 0, 0, 0, 0, 0, 1];
    private static readonly byte[] StaleVersion = [1, 0, 0, 0, 0, 0, 0, 0];

    private readonly FakePolicyRepository _repository = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly FixedTimeProvider _timeProvider = new(Now);

    [Fact]
    public async Task CreateRejectsCurrencyThatIsNotEnabledForTheDeployment()
    {
        var service = CreateService("EUR");

        var exception = await Assert.ThrowsAsync<RequestValidationException>(
            () => service.CreateAsync(OrganizationId, "SYNTH-0001", 100m, "USD", default));

        Assert.Equal("currency_not_supported", exception.Code);
        Assert.Empty(_repository.Policies);
        Assert.Equal(0, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task CreatePersistsNormalizedPolicyAndSavesOnce()
    {
        var service = CreateService("EUR");

        var response = await service.CreateAsync(
            OrganizationId,
            "  synth-0001  ",
            125_000.00m,
            "eur",
            default);

        Assert.Equal("synth-0001", response.PolicyNumber);
        Assert.Equal("EUR", response.Currency);
        Assert.Equal(PolicyStatus.Draft, response.Status);
        Assert.Equal(Now, response.CreatedAtUtc);
        Assert.Equal("SYNTH-0001", Assert.Single(_repository.Policies).NormalizedPolicyNumber);
        Assert.Equal(1, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task CreateRejectsDuplicateNormalizedNumberInsideTheSameOrganization()
    {
        var service = CreateService("EUR");
        _repository.Seed(Policy.Create(OrganizationId, "SYNTH-0001", 100m, "EUR", Now));

        await Assert.ThrowsAsync<PolicyNumberConflictException>(
            () => service.CreateAsync(OrganizationId, "synth-0001", 100m, "EUR", default));

        Assert.Equal(0, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task CreateAllowsTheSameNumberInAnotherOrganization()
    {
        var service = CreateService("EUR");
        _repository.Seed(Policy.Create(OtherOrganizationId, "SYNTH-0001", 100m, "EUR", Now));

        var response = await service.CreateAsync(
            OrganizationId,
            "SYNTH-0001",
            100m,
            "EUR",
            default);

        Assert.Equal(OrganizationId, response.OrganizationId);
    }

    [Theory]
    [InlineData(0, 20)]
    [InlineData(-1, 20)]
    [InlineData(1, 0)]
    [InlineData(1, PolicyService.MaxPageSize + 1)]
    public async Task ListRejectsPagingOutsideTheSupportedRange(int pageNumber, int pageSize)
    {
        var service = CreateService("EUR");

        await Assert.ThrowsAsync<RequestValidationException>(
            () => service.ListAsync(OrganizationId, pageNumber, pageSize, default));
    }

    [Fact]
    public async Task ListRejectsAPageOffsetThatWouldOverflowTheSqlOffset()
    {
        var service = CreateService("EUR");

        var exception = await Assert.ThrowsAsync<RequestValidationException>(
            () => service.ListAsync(OrganizationId, int.MaxValue, PolicyService.MaxPageSize, default));

        Assert.Equal("validation_failed", exception.Code);
    }

    [Fact]
    public async Task ListReturnsOnlyTheRequestingOrganization()
    {
        var service = CreateService("EUR");
        _repository.Seed(Policy.Create(OrganizationId, "SYNTH-A", 100m, "EUR", Now));
        _repository.Seed(Policy.Create(OtherOrganizationId, "SYNTH-B", 100m, "EUR", Now));

        var page = await service.ListAsync(OrganizationId, 1, 20, default);

        Assert.Equal(1, page.TotalCount);
        Assert.Equal("SYNTH-A", Assert.Single(page.Items).PolicyNumber);
    }

    [Fact]
    public async Task GetByIdTreatsAnotherOrganizationsPolicyAsNotFound()
    {
        var service = CreateService("EUR");
        var foreignPolicy = Policy.Create(OtherOrganizationId, "SYNTH-0001", 100m, "EUR", Now);
        _repository.Seed(foreignPolicy);

        await Assert.ThrowsAsync<PolicyNotFoundException>(
            () => service.GetByIdAsync(OrganizationId, foreignPolicy.Id, default));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    [InlineData(16)]
    public async Task ActivateRejectsAnEtagThatIsNotASqlServerRowVersion(int versionLength)
    {
        var service = CreateService("EUR");
        var policy = SeedActivatablePolicy();

        var exception = await Assert.ThrowsAsync<RequestValidationException>(
            () => service.ActivateAsync(
                CommandContext(),
                policy.Id,
                new byte[versionLength],
                "SYNTH-INSURED-1",
                new DateOnly(2026, 9, 8),
                new DateOnly(2027, 9, 7),
                default));

        Assert.Equal("etag_invalid", exception.Code);
    }

    [Fact]
    public async Task ActivateRejectsAStaleVersionWithoutWritingAnAuditRecord()
    {
        var service = CreateService("EUR");
        var policy = SeedActivatablePolicy();

        await Assert.ThrowsAsync<PolicyConcurrencyException>(
            () => service.ActivateAsync(
                CommandContext(),
                policy.Id,
                StaleVersion,
                "SYNTH-INSURED-1",
                new DateOnly(2026, 9, 8),
                new DateOnly(2027, 9, 7),
                default));

        Assert.Empty(_repository.Transitions);
        Assert.Equal(0, _unitOfWork.SaveCount);
        Assert.Equal(PolicyStatus.Draft, policy.Status);
    }

    [Fact]
    public async Task ActivateWritesOneAuditRecordAndSavesOnce()
    {
        var service = CreateService("EUR");
        var policy = SeedActivatablePolicy();

        var response = await service.ActivateAsync(
            CommandContext(),
            policy.Id,
            CurrentVersion,
            "  SYNTH-INSURED-1  ",
            new DateOnly(2026, 9, 8),
            new DateOnly(2027, 9, 7),
            default);

        var transition = Assert.Single(_repository.Transitions);
        Assert.Equal(PolicyStatus.Active, response.Status);
        Assert.Equal("SYNTH-INSURED-1", response.InsuredPartyReference);
        Assert.Equal(PolicyStatus.Draft, transition.FromStatus);
        Assert.Equal(PolicyStatus.Active, transition.ToStatus);
        Assert.Equal("integration-operator", transition.ActorSubject);
        Assert.Equal("trace-1", transition.CorrelationId);
        Assert.Equal(Now, transition.OccurredAtUtc);
        Assert.Equal(1, _unitOfWork.SaveCount);
        Assert.Equal(CurrentVersion, _repository.LastExpectedVersion);
    }

    [Fact]
    public async Task CancelRecordsTheReasonInTheAuditTrail()
    {
        var service = CreateService("EUR");
        var policy = SeedActivatablePolicy();

        await service.CancelAsync(
            CommandContext(),
            policy.Id,
            CurrentVersion,
            "  Synthetic customer request  ",
            default);

        var transition = Assert.Single(_repository.Transitions);
        Assert.Equal(PolicyStatus.Cancelled, transition.ToStatus);
        Assert.Equal("Synthetic customer request", transition.Reason);
    }

    [Fact]
    public async Task CancelIsTerminal()
    {
        var service = CreateService("EUR");
        var policy = SeedActivatablePolicy();
        await service.CancelAsync(CommandContext(), policy.Id, CurrentVersion, "First", default);

        var exception = await Assert.ThrowsAsync<DomainRuleViolationException>(
            () => service.CancelAsync(CommandContext(), policy.Id, CurrentVersion, "Second", default));

        Assert.Equal("policy_transition_invalid", exception.Code);
        Assert.Single(_repository.Transitions);
    }

    [Fact]
    public async Task ListTransitionsRejectsAPolicyOutsideTheOrganization()
    {
        var service = CreateService("EUR");
        var foreignPolicy = Policy.Create(OtherOrganizationId, "SYNTH-0001", 100m, "EUR", Now);
        _repository.Seed(foreignPolicy);

        await Assert.ThrowsAsync<PolicyNotFoundException>(
            () => service.ListTransitionsAsync(OrganizationId, foreignPolicy.Id, default));
    }

    [Fact]
    public async Task ListTransitionsReturnsOccurrenceOrder()
    {
        var service = CreateService("EUR");
        var policy = SeedActivatablePolicy();
        await service.ActivateAsync(
            CommandContext(),
            policy.Id,
            CurrentVersion,
            "SYNTH-INSURED-1",
            new DateOnly(2026, 9, 8),
            new DateOnly(2027, 9, 7),
            default);
        _timeProvider.Advance(TimeSpan.FromMinutes(1));
        await service.CancelAsync(CommandContext(), policy.Id, CurrentVersion, "Done", default);

        var transitions = await service.ListTransitionsAsync(OrganizationId, policy.Id, default);

        Assert.Collection(
            transitions,
            first => Assert.Equal(PolicyStatus.Active, first.ToStatus),
            second => Assert.Equal(PolicyStatus.Cancelled, second.ToStatus));
    }

    [Fact]
    public async Task ResponseExposesTheRowVersionAsBase64()
    {
        var service = CreateService("EUR");
        var policy = SeedActivatablePolicy();

        var response = await service.GetByIdAsync(OrganizationId, policy.Id, default);

        Assert.Equal(Convert.ToBase64String(CurrentVersion), response.Version);
    }

    private PolicyService CreateService(params string[] supportedCurrencies)
    {
        return new PolicyService(
            _timeProvider,
            new FakeCurrencyCatalog(supportedCurrencies),
            _repository,
            _unitOfWork);
    }

    private Policy SeedActivatablePolicy()
    {
        var policy = Policy
            .Create(OrganizationId, "SYNTH-0001", 125_000m, "EUR", Now)
            .WithVersion(CurrentVersion);
        _repository.Seed(policy);
        return policy;
    }

    private static PolicyCommandContext CommandContext()
    {
        return new PolicyCommandContext(OrganizationId, "integration-operator", "trace-1");
    }
}
