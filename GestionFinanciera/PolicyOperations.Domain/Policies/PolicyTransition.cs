namespace PolicyOperations.Domain.Policies;

public sealed class PolicyTransition
{
    public const int MaxActorSubjectLength = 200;
    public const int MaxCorrelationIdLength = 100;

    private PolicyTransition()
    {
        ActorSubject = string.Empty;
        CorrelationId = string.Empty;
    }

    private PolicyTransition(
        Guid id,
        Guid policyId,
        Guid organizationId,
        PolicyStatus fromStatus,
        PolicyStatus toStatus,
        DateTimeOffset occurredAtUtc,
        string actorSubject,
        string? reason,
        string correlationId)
    {
        Id = id;
        PolicyId = policyId;
        OrganizationId = organizationId;
        FromStatus = fromStatus;
        ToStatus = toStatus;
        OccurredAtUtc = occurredAtUtc;
        ActorSubject = actorSubject;
        Reason = reason;
        CorrelationId = correlationId;
    }

    public Guid Id { get; private set; }

    public Guid PolicyId { get; private set; }

    public Guid OrganizationId { get; private set; }

    public PolicyStatus FromStatus { get; private set; }

    public PolicyStatus ToStatus { get; private set; }

    public DateTimeOffset OccurredAtUtc { get; private set; }

    public string ActorSubject { get; private set; }

    public string? Reason { get; private set; }

    public string CorrelationId { get; private set; }

    internal static PolicyTransition Create(
        Policy policy,
        PolicyStatus fromStatus,
        PolicyStatus toStatus,
        string? actorSubject,
        string? reason,
        string? correlationId,
        DateTimeOffset occurredAtUtc)
    {
        var validatedActorSubject = ValidateRequiredText(
            actorSubject,
            MaxActorSubjectLength,
            "actor_subject_invalid",
            "Actor subject is required and must fit the supported length.");
        var validatedCorrelationId = ValidateRequiredText(
            correlationId,
            MaxCorrelationIdLength,
            "correlation_id_invalid",
            "Correlation identifier is required and must fit the supported length.");

        if (occurredAtUtc.Offset != TimeSpan.Zero)
        {
            throw new DomainRuleViolationException(
                "transition_timestamp_must_be_utc",
                "Transition timestamp must use the UTC offset.");
        }

        return new PolicyTransition(
            Guid.NewGuid(),
            policy.Id,
            policy.OrganizationId,
            fromStatus,
            toStatus,
            occurredAtUtc,
            validatedActorSubject,
            reason,
            validatedCorrelationId);
    }

    private static string ValidateRequiredText(
        string? value,
        int maximumLength,
        string code,
        string message)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainRuleViolationException(code, message);
        }

        var trimmedValue = value.Trim();

        if (trimmedValue.Length > maximumLength)
        {
            throw new DomainRuleViolationException(code, message);
        }

        return trimmedValue;
    }
}
