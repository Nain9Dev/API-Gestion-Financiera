using Microsoft.EntityFrameworkCore;
using PolicyOperations.Application.Abstractions;
using PolicyOperations.Domain.Policies;

namespace PolicyOperations.Infrastructure.Persistence;

public sealed class EfPolicyRepository : IPolicyRepository
{
    private readonly PolicyOperationsDbContext _dbContext;

    public EfPolicyRepository(PolicyOperationsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(Policy policy, CancellationToken cancellationToken)
    {
        await _dbContext.Policies.AddAsync(policy, cancellationToken);
    }

    public async Task AddTransitionAsync(
        PolicyTransition transition,
        CancellationToken cancellationToken)
    {
        await _dbContext.PolicyTransitions.AddAsync(transition, cancellationToken);
    }

    public Task<bool> ExistsByNormalizedNumberAsync(
        Guid organizationId,
        string normalizedPolicyNumber,
        CancellationToken cancellationToken)
    {
        return _dbContext.Policies.AnyAsync(
            policy =>
                policy.OrganizationId == organizationId &&
                policy.NormalizedPolicyNumber == normalizedPolicyNumber,
            cancellationToken);
    }

    public Task<Policy?> GetByIdAsync(
        Guid organizationId,
        Guid policyId,
        bool trackChanges,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.Policies.Where(
            policy => policy.OrganizationId == organizationId && policy.Id == policyId);

        if (!trackChanges)
        {
            query = query.AsNoTracking();
        }

        return query.SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Policy>> ListAsync(
        Guid organizationId,
        int skip,
        int take,
        CancellationToken cancellationToken)
    {
        return await _dbContext.Policies
            .AsNoTracking()
            .Where(policy => policy.OrganizationId == organizationId)
            .OrderBy(policy => policy.PolicyNumber)
            .ThenBy(policy => policy.Id)
            .Skip(skip)
            .Take(take)
            .ToArrayAsync(cancellationToken);
    }

    public Task<int> CountAsync(Guid organizationId, CancellationToken cancellationToken)
    {
        return _dbContext.Policies.CountAsync(
            policy => policy.OrganizationId == organizationId,
            cancellationToken);
    }

    public async Task<IReadOnlyList<PolicyTransition>> ListTransitionsAsync(
        Guid organizationId,
        Guid policyId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.PolicyTransitions
            .AsNoTracking()
            .Where(transition =>
                transition.OrganizationId == organizationId &&
                transition.PolicyId == policyId)
            .OrderBy(transition => transition.OccurredAtUtc)
            .ThenBy(transition => transition.Id)
            .ToArrayAsync(cancellationToken);
    }

    public void SetExpectedVersion(Policy policy, byte[] expectedVersion)
    {
        _dbContext.Entry(policy)
            .Property(entity => entity.Version)
            .OriginalValue = expectedVersion;
    }
}
