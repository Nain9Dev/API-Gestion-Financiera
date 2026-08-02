using Microsoft.EntityFrameworkCore;
using PolicyOperations.Infrastructure.Persistence;

namespace PolicyOperations.Api.Demo;

public sealed class PublicDemoDataPruner
{
    private readonly PolicyOperationsDbContext _dbContext;

    public PublicDemoDataPruner(PolicyOperationsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task PruneAsync(
        Guid organizationId,
        DateTimeOffset createdBeforeUtc,
        CancellationToken cancellationToken)
    {
        var executionStrategy = _dbContext.Database.CreateExecutionStrategy();

        await executionStrategy.ExecuteAsync(async () =>
        {
            var expiredPolicyIds = _dbContext.Policies
                .Where(policy =>
                    policy.OrganizationId == organizationId &&
                    policy.CreatedAtUtc < createdBeforeUtc)
                .Select(policy => policy.Id);

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(
                cancellationToken);

            await _dbContext.PolicyTransitions
                .Where(transition =>
                    transition.OrganizationId == organizationId &&
                    expiredPolicyIds.Contains(transition.PolicyId))
                .ExecuteDeleteAsync(cancellationToken);

            await _dbContext.Policies
                .Where(policy =>
                    policy.OrganizationId == organizationId &&
                    policy.CreatedAtUtc < createdBeforeUtc)
                .ExecuteDeleteAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);
        });
    }
}
