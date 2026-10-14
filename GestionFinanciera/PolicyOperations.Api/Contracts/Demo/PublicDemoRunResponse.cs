using PolicyOperations.Application.Policies;

namespace PolicyOperations.Api.Contracts.Demo;

public sealed record PublicDemoRunResponse(
    string RunId,
    DateTimeOffset ExecutedAtUtc,
    int DataRetentionHours,
    long TotalDurationMs,
    bool CleanupCompleted,
    IReadOnlyList<PublicDemoStepResponse> Steps,
    PolicyResponse Policy,
    IReadOnlyList<PolicyTransitionResponse> Transitions);

public sealed record PublicDemoStepResponse(
    string Operation,
    string Method,
    string Path,
    int Status,
    string Result,
    string? ResourceStatus,
    string? Etag,
    string? ErrorCode,
    long DurationMs);
