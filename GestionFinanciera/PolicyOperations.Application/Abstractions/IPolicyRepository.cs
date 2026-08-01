using PolicyOperations.Domain.Policies;

namespace PolicyOperations.Application.Abstractions;

public interface IPolicyRepository
{
    Task AddAsync(Policy policy, CancellationToken cancellationToken);

    Task AddTransitionAsync(PolicyTransition transition, CancellationToken cancellationToken);

    Task<bool> ExistsByNormalizedNumberAsync(
        Guid organizationId,
        string normalizedPolicyNumber,
        CancellationToken cancellationToken);

    Task<Policy?> GetByIdAsync(
        Guid organizationId,
        Guid policyId,
        bool trackChanges,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<Policy>> ListAsync(
        Guid organizationId,
        int skip,
        int take,
        CancellationToken cancellationToken);

    Task<int> CountAsync(Guid organizationId, CancellationToken cancellationToken);

    Task<IReadOnlyList<PolicyTransition>> ListTransitionsAsync(
        Guid organizationId,
        Guid policyId,
        CancellationToken cancellationToken);

    void SetExpectedVersion(Policy policy, byte[] expectedVersion);
}
