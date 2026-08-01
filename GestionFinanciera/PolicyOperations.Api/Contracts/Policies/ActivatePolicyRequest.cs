namespace PolicyOperations.Api.Contracts.Policies;

public sealed record ActivatePolicyRequest(
    string? InsuredPartyReference,
    DateOnly CoverageStartDate,
    DateOnly CoverageEndDate);
