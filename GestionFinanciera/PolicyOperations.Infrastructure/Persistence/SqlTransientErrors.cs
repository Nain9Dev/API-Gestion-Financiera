using Microsoft.Data.SqlClient;

namespace PolicyOperations.Infrastructure.Persistence;

/// <summary>
/// Recognises the SQL Server and Azure SQL conditions that mean "not available yet, try again"
/// rather than "the request was wrong". Azure SQL Serverless resumes from <c>AutoPause</c> on the
/// first connection, so these are expected operating conditions of the free-tier deployment.
/// </summary>
public static class SqlTransientErrors
{
    private const int CommandTimeoutErrorNumber = -2;

    private static readonly HashSet<int> TransientErrorNumbers =
    [
        CommandTimeoutErrorNumber,
        20, // The instance did not return a response.
        64, // A transport-level connection error occurred.
        233, // No process is on the other end of the pipe.
        926, // Database is marked suspect and cannot be opened yet.
        927, // Database is in the middle of a restore or is coming online.
        945, // Database cannot be opened due to inaccessible files.
        4060, // Cannot open the requested database.
        10053, // Transport-level error while receiving results.
        10054, // Existing connection was forcibly closed.
        10060, // Network-related or instance-specific connection error.
        10928, // Resource limit reached.
        10929, // Server is too busy.
        40197, // The service encountered an error processing the request.
        40501, // The service is currently busy.
        40613, // Database is not currently available.
        41301, // Dependency failure in the transaction.
        41302, // Transaction updated a row updated since it started.
        41305, // Repeatable-read validation failure.
        41325, // Serializable validation failure.
        42108, // Serverless database is waking up from auto-pause.
        42109, // Serverless database is currently starting.
        49918, // Cannot process the request; not enough resources.
        49919, // Cannot process the create or update request.
        49920 // Cannot process the request; too many operations in progress.
    ];

    /// <summary>
    /// Returns <see langword="true"/> when <paramref name="exception"/>, or anything it wraps,
    /// reports a transient database condition.
    /// </summary>
    public static bool IsTransient(Exception? exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current is TimeoutException)
            {
                return true;
            }

            if (current is SqlException sqlException && ContainsTransientError(sqlException))
            {
                return true;
            }
        }

        return false;
    }

    private static bool ContainsTransientError(SqlException exception)
    {
        foreach (SqlError error in exception.Errors)
        {
            if (TransientErrorNumbers.Contains(error.Number))
            {
                return true;
            }
        }

        return TransientErrorNumbers.Contains(exception.Number);
    }
}
