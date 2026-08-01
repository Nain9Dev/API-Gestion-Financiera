namespace PolicyOperations.Application.Policies;

public sealed record PolicyCommandContext(
    Guid OrganizationId,
    string ActorSubject,
    string CorrelationId);
