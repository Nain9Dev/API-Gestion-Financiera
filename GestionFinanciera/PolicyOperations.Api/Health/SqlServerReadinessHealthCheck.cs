using Microsoft.Extensions.Diagnostics.HealthChecks;
using PolicyOperations.Infrastructure.Persistence;

namespace PolicyOperations.Api.Health;

public sealed class SqlServerReadinessHealthCheck : IHealthCheck
{
    private readonly PolicyOperationsDbContext _dbContext;

    public SqlServerReadinessHealthCheck(PolicyOperationsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _dbContext.Database.CanConnectAsync(cancellationToken)
                ? HealthCheckResult.Healthy("SQL Server is reachable.")
                : HealthCheckResult.Unhealthy("SQL Server is not reachable.");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy(
                "SQL Server readiness check failed.",
                exception);
        }
    }
}
