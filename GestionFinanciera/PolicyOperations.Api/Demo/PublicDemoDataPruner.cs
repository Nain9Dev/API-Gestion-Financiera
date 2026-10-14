using Microsoft.EntityFrameworkCore;
using PolicyOperations.Infrastructure.Persistence;

namespace PolicyOperations.Api.Demo;

/// <summary>
/// Opportunistic housekeeping for the public demo organization. Pruning is best effort: it keeps
/// the free-tier database small, but it is not part of the evidence a visitor came to see, so a
/// failure degrades the run instead of failing it.
/// </summary>
public sealed partial class PublicDemoDataPruner
{
    private readonly PolicyOperationsDbContext _dbContext;
    private readonly ILogger<PublicDemoDataPruner> _logger;

    public PublicDemoDataPruner(
        PolicyOperationsDbContext dbContext,
        ILogger<PublicDemoDataPruner> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Deletes expired synthetic rows of one organization. Returns <see langword="false"/> when the
    /// attempt failed and was skipped; the caller is expected to continue.
    /// </summary>
    public async Task<bool> TryPruneAsync(
        Guid organizationId,
        DateTimeOffset createdBeforeUtc,
        CancellationToken cancellationToken)
    {
        try
        {
            await PruneAsync(organizationId, createdBeforeUtc, cancellationToken);
            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            LogPruneSkipped(_logger, organizationId, exception);
            return false;
        }
    }

    private async Task PruneAsync(
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

    [LoggerMessage(
        EventId = 2100,
        Level = LogLevel.Warning,
        Message = "Public demo cleanup was skipped for organization {OrganizationId}. The run continues.")]
    private static partial void LogPruneSkipped(
        ILogger logger,
        Guid organizationId,
        Exception exception);
}
