using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PolicyOperations.Infrastructure.Persistence;

public sealed class PolicyOperationsDbContextFactory
    : IDesignTimeDbContextFactory<PolicyOperationsDbContext>
{
    public PolicyOperationsDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable(
            "POLICY_OPERATIONS_MIGRATIONS_SQLSERVER");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "POLICY_OPERATIONS_MIGRATIONS_SQLSERVER must identify an explicit development database.");
        }

        var optionsBuilder = new DbContextOptionsBuilder<PolicyOperationsDbContext>();
        optionsBuilder.UseSqlServer(connectionString);

        return new PolicyOperationsDbContext(optionsBuilder.Options);
    }
}
