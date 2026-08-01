using PolicyOperations.Domain.Policies;

namespace PolicyOperations.Application.Policies;

public sealed record PolicyResponse(
    Guid Id,
    Guid OrganizationId,
    string PolicyNumber,
    decimal InsuredAmount,
    string Currency,
    string? InsuredPartyReference,
    DateOnly? CoverageStartDate,
    DateOnly? CoverageEndDate,
    PolicyStatus Status,
    DateTimeOffset CreatedAtUtc,
    string Version);
