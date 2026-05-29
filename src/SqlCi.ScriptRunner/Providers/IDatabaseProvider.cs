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
    /// </summary>
    Task<IReadOnlyList<string>> GetAppliedScriptsAsync(IDbConnection connection, string tableName);

    /// <summary>
    /// Records that a script has been successfully executed.
    /// </summary>
    Task RecordScriptRunAsync(IDbConnection connection, string tableName, string id, string scriptName, string release, DateTime appliedOnUtc);

    /// <summary>
    /// Executes a migration script. 
    /// The implementation decides how to handle batching (e.g. GO for SQL Server, multiple statements for others).
    /// </summary>
    Task ExecuteScriptAsync(IDbConnection connection, string sql);

    /// <summary>
    /// Retrieves full script execution history records.
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

