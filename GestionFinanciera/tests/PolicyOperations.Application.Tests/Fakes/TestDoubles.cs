using System.Reflection;
using PolicyOperations.Application.Abstractions;
using PolicyOperations.Domain.Policies;

namespace PolicyOperations.Application.Tests.Fakes;

internal sealed class FakeCurrencyCatalog : ICurrencyCatalog
{
    private readonly HashSet<string> _supported;

    public FakeCurrencyCatalog(params string[] supported)
    {
        _supported = new HashSet<string>(supported, StringComparer.Ordinal);
    }

    public bool IsSupported(string currency)
    {
        return _supported.Contains(currency);
    }
}

internal sealed class FakeUnitOfWork : IUnitOfWork
{
    public int SaveCount { get; private set; }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        SaveCount++;
        return Task.CompletedTask;
    }
}

internal sealed class FixedTimeProvider : TimeProvider
{
    private DateTimeOffset _utcNow;

    public FixedTimeProvider(DateTimeOffset utcNow)
    {
        _utcNow = utcNow;
    }

    public override DateTimeOffset GetUtcNow()
    {
        return _utcNow;
    }

    public void Advance(TimeSpan amount)
    {
        _utcNow = _utcNow.Add(amount);
    }
}

internal static class PolicyTestFactory
{
    /// <summary>
    /// Assigns the rowversion that SQL Server would generate. The domain deliberately keeps
    /// <see cref="Policy.Version"/> read-only, so tests set it the way the provider does.
    /// </summary>
    public static Policy WithVersion(this Policy policy, params byte[] version)
    {
        var property = typeof(Policy).GetProperty(
            nameof(Policy.Version),
            BindingFlags.Instance | BindingFlags.Public)
            ?? throw new InvalidOperationException("Policy.Version is no longer discoverable.");

        property.SetValue(policy, version);
        return policy;
    }
}
