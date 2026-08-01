using PolicyOperations.Domain.Policies;
using Xunit;

namespace PolicyOperations.Domain.Tests.Policies;

public sealed class PolicyTests
{
    private static readonly Guid OrganizationId =
        Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly DateTimeOffset CreatedAtUtc =
        new(2026, 8, 2, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly CoverageStartDate = new(2026, 9, 1);
    private static readonly DateOnly CoverageEndDate = new(2027, 8, 31);

    [Fact]
    public void CreateWithValidValuesCreatesOrganizationScopedDraftPolicy()
    {
        var policy = CreatePolicy("  policy-001  ", 125_000.25m, " eur ");

        Assert.NotEqual(Guid.Empty, policy.Id);
        Assert.Equal(OrganizationId, policy.OrganizationId);
        Assert.Equal("policy-001", policy.PolicyNumber);
        Assert.Equal("POLICY-001", policy.NormalizedPolicyNumber);
        Assert.Equal(125_000.25m, policy.InsuredAmount);
        Assert.Equal("EUR", policy.Currency);
        Assert.Equal(PolicyStatus.Draft, policy.Status);
        Assert.Equal(CreatedAtUtc, policy.CreatedAtUtc);
        Assert.Empty(policy.Version);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateWithoutPolicyNumberThrowsRuleViolation(string? policyNumber)
    {
        var exception = Assert.Throws<DomainRuleViolationException>(
            () => CreatePolicy(policyNumber));

        Assert.Equal("policy_number_required", exception.Code);
    }

    [Fact]
    public void CreateWithPolicyNumberOverMaximumLengthThrowsRuleViolation()
    {
        var policyNumber = new string('P', Policy.MaxPolicyNumberLength + 1);

        var exception = Assert.Throws<DomainRuleViolationException>(
            () => CreatePolicy(policyNumber));

        Assert.Equal("policy_number_too_long", exception.Code);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("100.001")]
    public void CreateWithInvalidInsuredAmountThrowsRuleViolation(string amountText)
    {
        var insuredAmount = decimal.Parse(
            amountText,
            System.Globalization.CultureInfo.InvariantCulture);

        var exception = Assert.Throws<DomainRuleViolationException>(
            () => CreatePolicy(insuredAmount: insuredAmount));

        Assert.StartsWith("insured_amount_", exception.Code, StringComparison.Ordinal);
    }

    [Fact]
    public void CreateWithNonUtcTimestampThrowsRuleViolation()
    {
        var localTimestamp = new DateTimeOffset(
            2026,
            8,
            2,
            12,
            0,
            0,
            TimeSpan.FromHours(2));

        var exception = Assert.Throws<DomainRuleViolationException>(
            () => Policy.Create(
                OrganizationId,
                "POLICY-001",
                100m,
                "EUR",
                localTimestamp));

        Assert.Equal("created_at_must_be_utc", exception.Code);
    }

    [Fact]
    public void CreateWithMaximumInsuredAmountPreservesExactValue()
    {
        var policy = CreatePolicy(insuredAmount: Policy.MaxInsuredAmount);

        Assert.Equal(Policy.MaxInsuredAmount, policy.InsuredAmount);
    }

    [Fact]
    public void CreateAboveMaximumInsuredAmountThrowsRuleViolation()
    {
        var exception = Assert.Throws<DomainRuleViolationException>(
            () => CreatePolicy(insuredAmount: Policy.MaxInsuredAmount + 0.01m));

        Assert.Equal("insured_amount_invalid", exception.Code);
    }

    [Theory]
    [InlineData(null, "currency_required")]
    [InlineData("", "currency_required")]
    [InlineData("EU", "currency_invalid")]
    [InlineData("EU1", "currency_invalid")]
    [InlineData("EURO", "currency_invalid")]
    public void CreateWithInvalidCurrencyThrowsRuleViolation(
        string? currency,
        string expectedCode)
    {
        var exception = Assert.Throws<DomainRuleViolationException>(
            () => CreatePolicy(currency: currency));

        Assert.Equal(expectedCode, exception.Code);
    }

    [Fact]
    public void ActivateDraftStoresCompletenessAndReturnsAuditTransition()
    {
        var policy = CreatePolicy();

        var transition = policy.Activate(
            " insured-synthetic-001 ",
            CoverageStartDate,
            CoverageEndDate,
            "demo-operator",
            "trace-activate",
            CreatedAtUtc.AddMinutes(1));

        Assert.Equal(PolicyStatus.Active, policy.Status);
        Assert.Equal("insured-synthetic-001", policy.InsuredPartyReference);
        Assert.Equal(CoverageStartDate, policy.CoverageStartDate);
        Assert.Equal(CoverageEndDate, policy.CoverageEndDate);
        Assert.Equal(policy.Id, transition.PolicyId);
        Assert.Equal(OrganizationId, transition.OrganizationId);
        Assert.Equal(PolicyStatus.Draft, transition.FromStatus);
        Assert.Equal(PolicyStatus.Active, transition.ToStatus);
        Assert.Equal("demo-operator", transition.ActorSubject);
        Assert.Null(transition.Reason);
        Assert.Equal("trace-activate", transition.CorrelationId);
    }

    [Fact]
    public void ActivateWithInvalidCoverageDoesNotChangeDraft()
    {
        var policy = CreatePolicy();

        var exception = Assert.Throws<DomainRuleViolationException>(
            () => policy.Activate(
                "insured-synthetic-001",
                CoverageEndDate,
                CoverageStartDate,
                "demo-operator",
                "trace-invalid",
                CreatedAtUtc.AddMinutes(1)));

        Assert.Equal("coverage_period_invalid", exception.Code);
        Assert.Equal(PolicyStatus.Draft, policy.Status);
        Assert.Null(policy.InsuredPartyReference);
    }

    [Fact]
    public void CancelDraftRequiresReasonAndReturnsAuditTransition()
    {
        var policy = CreatePolicy();

        var transition = policy.Cancel(
            " duplicate request ",
            "demo-operator",
            "trace-cancel",
            CreatedAtUtc.AddMinutes(1));

        Assert.Equal(PolicyStatus.Cancelled, policy.Status);
        Assert.Equal(PolicyStatus.Draft, transition.FromStatus);
        Assert.Equal(PolicyStatus.Cancelled, transition.ToStatus);
        Assert.Equal("duplicate request", transition.Reason);
    }

    [Fact]
    public void CancelWithoutReasonDoesNotChangePolicy()
    {
        var policy = CreatePolicy();

        var exception = Assert.Throws<DomainRuleViolationException>(
            () => policy.Cancel(
                " ",
                "demo-operator",
                "trace-cancel",
                CreatedAtUtc.AddMinutes(1)));

        Assert.Equal("cancellation_reason_required", exception.Code);
        Assert.Equal(PolicyStatus.Draft, policy.Status);
    }

    [Fact]
    public void CancelledPolicyCannotReactivate()
    {
        var policy = CreatePolicy();
        _ = policy.Cancel(
            "duplicate request",
            "demo-operator",
            "trace-cancel",
            CreatedAtUtc.AddMinutes(1));

        var exception = Assert.Throws<DomainRuleViolationException>(
            () => policy.Activate(
                "insured-synthetic-001",
                CoverageStartDate,
                CoverageEndDate,
                "demo-operator",
                "trace-reactivate",
                CreatedAtUtc.AddMinutes(2)));

        Assert.Equal("policy_transition_invalid", exception.Code);
        Assert.Equal(PolicyStatus.Cancelled, policy.Status);
    }

    [Fact]
    public void InvalidTransitionActorDoesNotMutateDraft()
    {
        var policy = CreatePolicy();

        var exception = Assert.Throws<DomainRuleViolationException>(
            () => policy.Activate(
                "insured-synthetic-001",
                CoverageStartDate,
                CoverageEndDate,
                " ",
                "trace-activate",
                CreatedAtUtc.AddMinutes(1)));

        Assert.Equal("actor_subject_invalid", exception.Code);
        Assert.Equal(PolicyStatus.Draft, policy.Status);
    }

    private static Policy CreatePolicy(
        string? policyNumber = "POLICY-001",
        decimal insuredAmount = 100m,
        string? currency = "EUR")
    {
        return Policy.Create(
            OrganizationId,
            policyNumber,
            insuredAmount,
            currency,
            CreatedAtUtc);
    }
}
