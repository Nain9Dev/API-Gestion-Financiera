namespace PolicyOperations.Api.Configuration;

/// <summary>
/// Deployment-level tuning for SQL Server connection resilience. The defaults absorb an Azure SQL
/// Serverless resume from <c>AutoPause</c>, which is the documented behaviour of the free-tier
/// public demo and can take tens of seconds on the first request after an idle period.
/// </summary>
public sealed class SqlResilienceOptions
{
    public const string SectionName = "PolicyOperations:SqlResilience";

    public int MaxRetryCount { get; init; } = 10;

    public int MaxRetryDelaySeconds { get; init; } = 15;

    public int CommandTimeoutSeconds { get; init; } = 60;

    /// <summary>
    /// Seconds advertised in <c>Retry-After</c> when the database reports a transient condition.
    /// </summary>
    public int RetryAfterSeconds { get; init; } = 20;
}
