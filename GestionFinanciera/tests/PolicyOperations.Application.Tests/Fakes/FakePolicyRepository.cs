using PolicyOperations.Application.Abstractions;
using PolicyOperations.Domain.Policies;

namespace PolicyOperations.Application.Tests.Fakes;

/// <summary>
/// In-memory stand-in for <see cref="IPolicyRepository"/>. It reproduces the organization scoping
/// and ordering contract of the SQL implementation so application rules can be tested without a
/// database; SQL-specific behaviour stays covered by the integration suite.
/// </summary>
internal sealed class FakePolicyRepository : IPolicyRepository
{
    private readonly List<Policy> _policies = [];
    private readonly List<PolicyTransition> _transitions = [];

    public IReadOnlyList<Policy> Policies => _policies;

    public IReadOnlyList<PolicyTransition> Transitions => _transitions;

    public byte[]? LastExpectedVersion { get; private set; }

    public void Seed(Policy policy)
    {
        _policies.Add(policy);
    }

    public Task AddAsync(Policy policy, CancellationToken cancellationToken)
    {
        _policies.Add(policy);
        return Task.CompletedTask;
    }

    public Task AddTransitionAsync(PolicyTransition transition, CancellationToken cancellationToken)
    {
        _transitions.Add(transition);
        return Task.CompletedTask;
    }

    public Task<bool> ExistsByNormalizedNumberAsync(
        Guid organizationId,
        string normalizedPolicyNumber,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(_policies.Any(policy =>
            policy.OrganizationId == organizationId &&
            policy.NormalizedPolicyNumber == normalizedPolicyNumber));
    }

    public Task<Policy?> GetByIdAsync(
        Guid organizationId,
        Guid policyId,
        bool trackChanges,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(_policies.SingleOrDefault(policy =>
            policy.OrganizationId == organizationId && policy.Id == policyId));
    }

    public Task<IReadOnlyList<Policy>> ListAsync(
        Guid organizationId,
        int skip,
        int take,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<Policy> page = _policies
            .Where(policy => policy.OrganizationId == organizationId)
            .OrderBy(policy => policy.PolicyNumber, StringComparer.Ordinal)
            .ThenBy(policy => policy.Id)
            .Skip(skip)
            .Take(take)
            .ToArray();

        return Task.FromResult(page);
    }

    public Task<int> CountAsync(Guid organizationId, CancellationToken cancellationToken)
    {
        return Task.FromResult(_policies.Count(policy => policy.OrganizationId == organizationId));
    }

    public Task<IReadOnlyList<PolicyTransition>> ListTransitionsAsync(
        Guid organizationId,
        Guid policyId,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<PolicyTransition> ordered = _transitions
            .Where(transition =>
                transition.OrganizationId == organizationId &&
                transition.PolicyId == policyId)
            .OrderBy(transition => transition.OccurredAtUtc)
            .ThenBy(transition => transition.Id)
            .ToArray();

        return Task.FromResult(ordered);
    }

    public void SetExpectedVersion(Policy policy, byte[] expectedVersion)
    {
        LastExpectedVersion = expectedVersion;
    }
}
