using System.Data;

namespace SqlCi.ScriptRunner.Providers;

/// <summary>
/// Abstraction over database-specific behavior for script execution and tracking table management.
/// </summary>
public interface IDatabaseProvider
{
    /// <summary>
    /// Creates a new connection for this provider.
    /// </summary>
    IDbConnection CreateConnection(string connectionString);

    /// <summary>
    /// Ensures the script tracking table exists. Creates it if necessary using provider-appropriate DDL.
    /// </summary>
    Task EnsureTrackingTableExistsAsync(IDbConnection connection, string tableName);

    /// <summary>
    /// Checks whether the script tracking table already exists in the database.
    /// </summary>
    Task<bool> TrackingTableExistsAsync(IDbConnection connection, string tableName);

    /// <summary>
    /// Returns the list of already-applied script names from the tracking table.
    /// Results are ordered by the textual <c>Id</c> (the script's sequence prefix). Correct
    /// chronological ordering therefore relies on fixed-width, zero-padded sequence prefixes
    /// such as those produced by the CLI's script generator (a 17-digit UTC timestamp).
    /// </summary>
    Task<IReadOnlyList<string>> GetAppliedScriptsAsync(IDbConnection connection, string tableName);

    /// <summary>
    /// Records that a script has been successfully executed.
    /// When <paramref name="transaction"/> is supplied the insert participates in that transaction.
    /// </summary>
    Task RecordScriptRunAsync(IDbConnection connection, string tableName, string id, string scriptName, string release, DateTime appliedOnUtc, IDbTransaction? transaction = null);

    /// <summary>
    /// Executes a migration script.
    /// The implementation decides how to handle batching (e.g. GO for SQL Server, multiple statements for others).
    /// When <paramref name="transaction"/> is supplied the script participates in that transaction.
    /// </summary>
    Task ExecuteScriptAsync(IDbConnection connection, string sql, IDbTransaction? transaction = null);

    /// <summary>
    /// Retrieves full script execution history records, ordered by the textual <c>Id</c>
    /// (see remarks on <see cref="GetAppliedScriptsAsync"/> regarding fixed-width prefixes).
    /// </summary>
    Task<IReadOnlyList<ScriptExecutionRecord>> GetScriptExecutionHistoryAsync(IDbConnection connection, string tableName);
}

/// <summary>
/// Represents a record of a script that was executed.
/// </summary>
public record ScriptExecutionRecord(
    string Id,
    string Script,
    string Release,
    DateTime AppliedOnUtc
);

