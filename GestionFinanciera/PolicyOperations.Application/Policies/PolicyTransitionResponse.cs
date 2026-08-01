using PolicyOperations.Domain.Policies;

namespace PolicyOperations.Application.Policies;

public sealed record PolicyTransitionResponse(
    Guid Id,
    Guid PolicyId,
    PolicyStatus FromStatus,
    PolicyStatus ToStatus,
    DateTimeOffset OccurredAtUtc,
    string ActorSubject,
    string? Reason,
    string CorrelationId);
