using Microsoft.Data.Sqlite;
using System.Data;
using System.Globalization;

namespace SqlCi.ScriptRunner.Providers;

public class SqliteProvider : IDatabaseProvider
{
    public IDbConnection CreateConnection(string connectionString)
        => new SqliteConnection(connectionString);

    public async Task EnsureTrackingTableExistsAsync(IDbConnection connection, string tableName)
    {
        if (connection is not SqliteConnection sqliteConnection)
            throw new ArgumentException("Connection must be a SqliteConnection for SqliteProvider.");

        var sql = $@"
CREATE TABLE IF NOT EXISTS ""{tableName}"" (
    ""Id"" TEXT PRIMARY KEY,
    ""Script"" TEXT NOT NULL,
    ""Release"" TEXT NOT NULL,
    ""AppliedOnUtc"" TEXT NOT NULL
);";

        await using var cmd = new SqliteCommand(sql, sqliteConnection);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<bool> TrackingTableExistsAsync(IDbConnection connection, string tableName)
    {
        if (connection is not SqliteConnection sqliteConnection)
            throw new ArgumentException("Connection must be a SqliteConnection.");

        var sql = $@"SELECT 1 FROM sqlite_master WHERE type='table' AND name='{tableName}'";

        await using var cmd = new SqliteCommand(sql, sqliteConnection);
        var result = await cmd.ExecuteScalarAsync();
        return result != null;
    }

    public async Task<IReadOnlyList<string>> GetAppliedScriptsAsync(IDbConnection connection, string tableName)
    {
        if (connection is not SqliteConnection sqliteConnection)
            throw new ArgumentException("Connection must be a SqliteConnection.");

        var scripts = new List<string>();
        var sql = $@"SELECT ""Script"" FROM ""{tableName}"" ORDER BY ""Id""";

        await using var cmd = new SqliteCommand(sql, sqliteConnection);
        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            if (!reader.IsDBNull(0))
                scripts.Add(reader.GetString(0));
        }

        return scripts;
    }

    public async Task RecordScriptRunAsync(IDbConnection connection, string tableName, string id, string scriptName, string release, DateTime appliedOnUtc)
    {
        if (connection is not SqliteConnection sqliteConnection)
            throw new ArgumentException("Connection must be a SqliteConnection.");

        var sql = $@"
INSERT INTO ""{tableName}"" (""Id"", ""Script"", ""Release"", ""AppliedOnUtc"")
VALUES (@Id, @Script, @Release, @AppliedOnUtc)";

        await using var cmd = new SqliteCommand(sql, sqliteConnection);
        cmd.Parameters.AddWithValue("@Id", id);
        cmd.Parameters.AddWithValue("@Script", scriptName);
        cmd.Parameters.AddWithValue("@Release", release);
        cmd.Parameters.AddWithValue("@AppliedOnUtc", appliedOnUtc.ToString("O")); // ISO 8601

        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<IReadOnlyList<ScriptExecutionRecord>> GetScriptExecutionHistoryAsync(IDbConnection connection, string tableName)
    {
        if (connection is not SqliteConnection sqliteConnection)
            throw new ArgumentException("Connection must be a SqliteConnection.");

        var records = new List<ScriptExecutionRecord>();
        var sql = $@"SELECT ""Id"", ""Script"", ""Release"", ""AppliedOnUtc"" FROM ""{tableName}"" ORDER BY ""Id""";

        await using var cmd = new SqliteCommand(sql, sqliteConnection);
        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            records.Add(new ScriptExecutionRecord(
                reader["Id"] as string ?? "",
                reader["Script"] as string ?? "",
                reader["Release"] as string ?? "",
                DateTime.Parse(reader["AppliedOnUtc"] as string ?? "", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind)
            ));
        }

        return records;
    }

    public async Task ExecuteScriptAsync(IDbConnection connection, string sql)
    {
        if (connection is not SqliteConnection sqliteConnection)
            throw new ArgumentException("Connection must be a SqliteConnection.");

        // SQLite: Execute the entire script. Multiple statements are supported.
        await using var cmd = new SqliteCommand(sql, sqliteConnection);
        await cmd.ExecuteNonQueryAsync();
    }
}
