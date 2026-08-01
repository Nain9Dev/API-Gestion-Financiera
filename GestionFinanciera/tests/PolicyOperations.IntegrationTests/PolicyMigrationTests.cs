using System.Diagnostics.CodeAnalysis;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PolicyOperations.Infrastructure.Persistence;
using Xunit;

namespace PolicyOperations.IntegrationTests;

[SuppressMessage(
    "Design",
    "CA1001:Types that own disposable fields should be disposable",
    Justification = "xUnit invokes IAsyncLifetime.DisposeAsync after every test instance.")]
public sealed class PolicyMigrationTests : IAsyncLifetime
{
    private const string DatabaseNamePrefix = "PolicyOperationsMigrationTests_";
    private const string InitialMigration = "20260228225832_InitialCreate";
    private const string LifecycleMigration = "20260801213622_PolicyLifecycleFoundation";
    private const string OrganizationMigration = "20260801221724_OrganizationLifecycleSecurity";

    private PolicyOperationsDbContext? _dbContext;
    private string? _databaseName;

    public Task InitializeAsync()
    {
        var baseConnectionString = Environment.GetEnvironmentVariable(
            SqlServerFactAttribute.ConnectionStringEnvironmentVariable);

        if (string.IsNullOrWhiteSpace(baseConnectionString))
        {
            return Task.CompletedTask;
        }

        var connectionStringBuilder = new SqlConnectionStringBuilder(baseConnectionString)
        {
            InitialCatalog = DatabaseNamePrefix + Guid.NewGuid().ToString("N")
        };
        _databaseName = connectionStringBuilder.InitialCatalog;

        var options = new DbContextOptionsBuilder<PolicyOperationsDbContext>()
            .UseSqlServer(connectionStringBuilder.ConnectionString)
            .Options;
        _dbContext = new PolicyOperationsDbContext(options);

        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        if (_dbContext is null || string.IsNullOrWhiteSpace(_databaseName))
        {
            return;
        }

        if (!_databaseName.StartsWith(DatabaseNamePrefix, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "Refusing to delete a database outside the migration-test namespace.");
        }

        await _dbContext.Database.EnsureDeletedAsync();
        await _dbContext.DisposeAsync();
    }

    [SqlServerFact]
    public async Task LifecycleMigrationPreservesLegacyRows()
    {
        var dbContext = DbContext;
        var migrator = dbContext.Database.GetService<IMigrator>();
        await migrator.MigrateAsync(InitialMigration);

        var activeId = Guid.NewGuid();
        var cancelledId = Guid.NewGuid();
        var activeIssueDate = new DateTime(2026, 1, 10, 8, 30, 0, DateTimeKind.Unspecified);
        var cancelledIssueDate = new DateTime(2026, 2, 20, 9, 45, 0, DateTimeKind.Unspecified);

        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""
            INSERT INTO [Policies] ([Id], [PolicyNumber], [InsuredAmount], [IsActive], [IssueDate])
            VALUES
                ({activeId}, {" legacy-active "}, {1000m}, {true}, {activeIssueDate}),
                ({cancelledId}, {"legacy-cancelled"}, {2000m}, {false}, {cancelledIssueDate});
            """);

        await migrator.MigrateAsync(LifecycleMigration);
        var rows = await ReadLifecycleRowsAsync(dbContext);

        Assert.Equal(2, rows.Length);
        Assert.Equal("LEGACY-ACTIVE", rows[0].NormalizedPolicyNumber);
        Assert.Equal("Active", rows[0].Status);
        Assert.Equal(new DateTimeOffset(activeIssueDate, TimeSpan.Zero), rows[0].CreatedAtUtc);
        Assert.Equal("LEGACY-CANCELLED", rows[1].NormalizedPolicyNumber);
        Assert.Equal("Cancelled", rows[1].Status);
        Assert.Equal(new DateTimeOffset(cancelledIssueDate, TimeSpan.Zero), rows[1].CreatedAtUtc);
    }

    [SqlServerFact]
    public async Task LifecycleMigrationStopsBeforeLosingDuplicatePolicyNumbers()
    {
        var dbContext = DbContext;
        var migrator = dbContext.Database.GetService<IMigrator>();
        await migrator.MigrateAsync(InitialMigration);

        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""
            INSERT INTO [Policies] ([Id], [PolicyNumber], [InsuredAmount], [IsActive], [IssueDate])
            VALUES
                ({Guid.NewGuid()}, {"DUPLICATE-001"}, {1000m}, {true}, {new DateTime(2026, 1, 1)}),
                ({Guid.NewGuid()}, {" duplicate-001 "}, {2000m}, {true}, {new DateTime(2026, 1, 2)});
            """);

        var exception = await Assert.ThrowsAsync<SqlException>(
            () => migrator.MigrateAsync());

        Assert.Equal(51001, exception.Number);
        var appliedMigrations = await dbContext.Database.GetAppliedMigrationsAsync();
        Assert.Equal(new[] { InitialMigration }, appliedMigrations);
    }

    [SqlServerFact]
    public async Task OrganizationMigrationRefusesToInventOwnershipOrCurrency()
    {
        var dbContext = DbContext;
        var migrator = dbContext.Database.GetService<IMigrator>();
        await migrator.MigrateAsync(LifecycleMigration);

        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""
            INSERT INTO [Policies]
                ([Id], [PolicyNumber], [NormalizedPolicyNumber], [InsuredAmount], [Status], [CreatedAtUtc])
            VALUES
                ({Guid.NewGuid()}, {"LEGACY-OWNERSHIP"}, {"LEGACY-OWNERSHIP"}, {1000m}, {"Draft"}, {DateTimeOffset.UtcNow});
            """);

        var exception = await Assert.ThrowsAsync<SqlException>(
            () => migrator.MigrateAsync());

        Assert.Equal(51003, exception.Number);
        var appliedMigrations = await dbContext.Database.GetAppliedMigrationsAsync();
        Assert.Equal(new[] { InitialMigration, LifecycleMigration }, appliedMigrations);
    }

    [SqlServerFact]
    public async Task OrganizationDownMigrationRefusesDataLoss()
    {
        var dbContext = DbContext;
        var migrator = dbContext.Database.GetService<IMigrator>();
        await migrator.MigrateAsync();

        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""
            INSERT INTO [Policies]
                ([Id], [OrganizationId], [PolicyNumber], [NormalizedPolicyNumber], [InsuredAmount], [Currency], [Status], [CreatedAtUtc])
            VALUES
                ({Guid.NewGuid()}, {Guid.NewGuid()}, {"CURRENT-001"}, {"CURRENT-001"}, {1000m}, {"EUR"}, {"Draft"}, {DateTimeOffset.UtcNow});
            """);

        var exception = await Assert.ThrowsAsync<SqlException>(
            () => migrator.MigrateAsync(LifecycleMigration));

        Assert.Equal(51004, exception.Number);
        var appliedMigrations = await dbContext.Database.GetAppliedMigrationsAsync();
        Assert.Contains(OrganizationMigration, appliedMigrations);
    }

    private static async Task<LifecycleRow[]> ReadLifecycleRowsAsync(
        PolicyOperationsDbContext dbContext)
    {
        var connectionString = dbContext.Database.GetConnectionString() ??
            throw new InvalidOperationException("The migration test connection string is missing.");
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT [NormalizedPolicyNumber], [Status], [CreatedAtUtc]
            FROM [Policies]
            ORDER BY [NormalizedPolicyNumber];
            """;
        await using var reader = await command.ExecuteReaderAsync();
        var rows = new List<LifecycleRow>();

        while (await reader.ReadAsync())
        {
            rows.Add(new LifecycleRow(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetFieldValue<DateTimeOffset>(2)));
        }

        return rows.ToArray();
    }

    private PolicyOperationsDbContext DbContext =>
        _dbContext ?? throw new InvalidOperationException("The migration test database is not initialized.");

    private sealed record LifecycleRow(
        string NormalizedPolicyNumber,
        string Status,
        DateTimeOffset CreatedAtUtc);
}
