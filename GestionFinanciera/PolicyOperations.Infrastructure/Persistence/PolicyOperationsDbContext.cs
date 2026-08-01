using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using PolicyOperations.Application.Abstractions;
using PolicyOperations.Application.Policies;
using PolicyOperations.Domain.Policies;

namespace PolicyOperations.Infrastructure.Persistence;

public sealed class PolicyOperationsDbContext : DbContext, IUnitOfWork
{
    public PolicyOperationsDbContext(DbContextOptions<PolicyOperationsDbContext> options)
        : base(options)
    {
    }

    public DbSet<Policy> Policies => Set<Policy>();

    public DbSet<PolicyTransition> PolicyTransitions => Set<PolicyTransition>();

    public override async Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await base.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new PolicyConcurrencyException(exception);
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is SqlException { Number: 2601 or 2627 })
        {
            throw new PolicyNumberConflictException(exception);
        }
    }

    async Task IUnitOfWork.SaveChangesAsync(CancellationToken cancellationToken)
    {
        await SaveChangesAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PolicyOperationsDbContext).Assembly);
    }
}
