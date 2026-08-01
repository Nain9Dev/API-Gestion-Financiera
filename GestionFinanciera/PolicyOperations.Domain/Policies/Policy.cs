namespace PolicyOperations.Domain.Policies;

public sealed class Policy
{
    public const int MaxPolicyNumberLength = 50;
    public const int MaxInsuredPartyReferenceLength = 100;
    public const int MaxCancellationReasonLength = 500;
    public const decimal MaxInsuredAmount = 9_999_999_999_999_999.99m;

    private Policy()
    {
        PolicyNumber = string.Empty;
        NormalizedPolicyNumber = string.Empty;
        Currency = string.Empty;
        Version = [];
    }

    private Policy(
        Guid id,
        Guid organizationId,
        string policyNumber,
        string normalizedPolicyNumber,
        decimal insuredAmount,
        string currency,
        DateTimeOffset createdAtUtc)
    {
        Id = id;
        OrganizationId = organizationId;
        PolicyNumber = policyNumber;
        NormalizedPolicyNumber = normalizedPolicyNumber;
        InsuredAmount = insuredAmount;
        Currency = currency;
        Status = PolicyStatus.Draft;
        CreatedAtUtc = createdAtUtc;
        Version = [];
    }

    public Guid Id { get; private set; }

    public Guid OrganizationId { get; private set; }

    public string PolicyNumber { get; private set; }

    public string NormalizedPolicyNumber { get; private set; }

    public decimal InsuredAmount { get; private set; }

    public string Currency { get; private set; }

    public string? InsuredPartyReference { get; private set; }

    public DateOnly? CoverageStartDate { get; private set; }

    public DateOnly? CoverageEndDate { get; private set; }

    public PolicyStatus Status { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public byte[] Version { get; private set; }

    public static Policy Create(
        Guid organizationId,
        string? policyNumber,
        decimal insuredAmount,
        string? currency,
        DateTimeOffset createdAtUtc)
    {
        ValidateOrganizationId(organizationId);
        var trimmedPolicyNumber = ValidatePolicyNumber(policyNumber);
        ValidateInsuredAmount(insuredAmount);
        var normalizedCurrency = ValidateCurrency(currency);
        ValidateUtcTimestamp(createdAtUtc);

        return new Policy(
            Guid.NewGuid(),
            organizationId,
            trimmedPolicyNumber,
            NormalizePolicyNumber(trimmedPolicyNumber),
            insuredAmount,
            normalizedCurrency,
            createdAtUtc);
    }

    public PolicyTransition Activate(
        string? insuredPartyReference,
        DateOnly coverageStartDate,
        DateOnly coverageEndDate,
        string? actorSubject,
        string? correlationId,
        DateTimeOffset occurredAtUtc)
    {
        EnsureStatus(PolicyStatus.Draft, PolicyStatus.Active);
        var validatedReference = ValidateInsuredPartyReference(insuredPartyReference);
        ValidateCoveragePeriod(coverageStartDate, coverageEndDate);

        var previousStatus = Status;
        var transition = PolicyTransition.Create(
            this,
            previousStatus,
            PolicyStatus.Active,
            actorSubject,
            reason: null,
            correlationId,
            occurredAtUtc);

        InsuredPartyReference = validatedReference;
        CoverageStartDate = coverageStartDate;
        CoverageEndDate = coverageEndDate;
        Status = PolicyStatus.Active;

        return transition;
    }

    public PolicyTransition Cancel(
        string? reason,
        string? actorSubject,
        string? correlationId,
        DateTimeOffset occurredAtUtc)
    {
        if (Status is not (PolicyStatus.Draft or PolicyStatus.Active))
        {
            throw CreateTransitionException(Status, PolicyStatus.Cancelled);
        }

        var validatedReason = ValidateCancellationReason(reason);
        var previousStatus = Status;
        var transition = PolicyTransition.Create(
            this,
            previousStatus,
            PolicyStatus.Cancelled,
            actorSubject,
            validatedReason,
            correlationId,
            occurredAtUtc);

        Status = PolicyStatus.Cancelled;
        return transition;
    }

    private static void ValidateOrganizationId(Guid organizationId)
    {
        if (organizationId == Guid.Empty)
        {
            throw new DomainRuleViolationException(
                "organization_id_invalid",
                "Organization identifier must not be empty.");
        }
    }

    private static string ValidatePolicyNumber(string? policyNumber)
    {
        if (string.IsNullOrWhiteSpace(policyNumber))
        {
            throw new DomainRuleViolationException(
                "policy_number_required",
                "Policy number is required.");
        }

        var trimmedPolicyNumber = policyNumber.Trim();

        if (trimmedPolicyNumber.Length > MaxPolicyNumberLength)
        {
            throw new DomainRuleViolationException(
                "policy_number_too_long",
                $"Policy number cannot exceed {MaxPolicyNumberLength} characters.");
        }

        return trimmedPolicyNumber;
    }

    private static void ValidateInsuredAmount(decimal insuredAmount)
    {
        if (insuredAmount <= 0 || insuredAmount > MaxInsuredAmount)
        {
            throw new DomainRuleViolationException(
                "insured_amount_invalid",
                "Insured amount is outside the supported range.");
        }

        if (decimal.Round(insuredAmount, 2, MidpointRounding.ToEven) != insuredAmount)
        {
            throw new DomainRuleViolationException(
                "insured_amount_precision_invalid",
                "Insured amount cannot contain more than two decimal places.");
        }
    }

    private static string ValidateCurrency(string? currency)
    {
        if (string.IsNullOrWhiteSpace(currency))
        {
            throw new DomainRuleViolationException(
                "currency_required",
                "Currency is required.");
        }

        var normalizedCurrency = currency.Trim().ToUpperInvariant();

        if (normalizedCurrency.Length != 3 ||
            normalizedCurrency.Any(character => character is < 'A' or > 'Z'))
        {
            throw new DomainRuleViolationException(
                "currency_invalid",
                "Currency must use a three-letter ISO 4217 alphabetic code.");
        }

        return normalizedCurrency;
    }

    private static string ValidateInsuredPartyReference(string? insuredPartyReference)
    {
        if (string.IsNullOrWhiteSpace(insuredPartyReference))
        {
            throw new DomainRuleViolationException(
                "insured_party_reference_required",
                "Insured party reference is required for activation.");
        }

        var trimmedReference = insuredPartyReference.Trim();

        if (trimmedReference.Length > MaxInsuredPartyReferenceLength)
        {
            throw new DomainRuleViolationException(
                "insured_party_reference_too_long",
                $"Insured party reference cannot exceed {MaxInsuredPartyReferenceLength} characters.");
        }

        return trimmedReference;
    }

    private static void ValidateCoveragePeriod(DateOnly startDate, DateOnly endDate)
    {
        if (endDate < startDate)
        {
            throw new DomainRuleViolationException(
                "coverage_period_invalid",
                "Coverage end date cannot be earlier than the start date.");
        }
    }

    private static string ValidateCancellationReason(string? reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new DomainRuleViolationException(
                "cancellation_reason_required",
                "Cancellation reason is required.");
        }

        var trimmedReason = reason.Trim();

        if (trimmedReason.Length > MaxCancellationReasonLength)
        {
            throw new DomainRuleViolationException(
                "cancellation_reason_too_long",
                $"Cancellation reason cannot exceed {MaxCancellationReasonLength} characters.");
        }

        return trimmedReason;
    }

    private static void ValidateUtcTimestamp(DateTimeOffset timestamp)
    {
        if (timestamp.Offset != TimeSpan.Zero)
        {
            throw new DomainRuleViolationException(
                "created_at_must_be_utc",
                "Created timestamp must use the UTC offset.");
        }
    }

    private void EnsureStatus(PolicyStatus expectedStatus, PolicyStatus targetStatus)
    {
        if (Status != expectedStatus)
        {
            throw CreateTransitionException(Status, targetStatus);
        }
    }

    private static DomainRuleViolationException CreateTransitionException(
        PolicyStatus currentStatus,
        PolicyStatus targetStatus)
    {
        return new DomainRuleViolationException(
            "policy_transition_invalid",
            $"Policy cannot transition from {currentStatus} to {targetStatus}.");
    }

    private static string NormalizePolicyNumber(string policyNumber)
    {
        return policyNumber.ToUpperInvariant();
    }
}
