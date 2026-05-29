using Npgsql;
using System.Data;

namespace SqlCi.ScriptRunner.Providers;

public class PostgreSqlProvider : IDatabaseProvider
{
    public IDbConnection CreateConnection(string connectionString)
        => new NpgsqlConnection(connectionString);

    public async Task EnsureTrackingTableExistsAsync(IDbConnection connection, string tableName)
    {
        var pgConnection = ProviderHelpers.Cast<NpgsqlConnection>(connection);
        ProviderHelpers.ValidateTableName(tableName);

        // PostgreSQL uses information_schema and is generally case-sensitive with unquoted identifiers.
        // We quote the validated identifier to preserve casing.
        var sql = $@"
CREATE TABLE IF NOT EXISTS ""{tableName}"" (
    ""Id"" VARCHAR(50) PRIMARY KEY,
    ""Script"" VARCHAR(255) NOT NULL,
    ""Release"" VARCHAR(25) NOT NULL,
    ""AppliedOnUtc"" TIMESTAMP NOT NULL
);";

        await using var cmd = new NpgsqlCommand(sql, pgConnection);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<bool> TrackingTableExistsAsync(IDbConnection connection, string tableName)
    {
        var pgConnection = ProviderHelpers.Cast<NpgsqlConnection>(connection);
        ProviderHelpers.ValidateTableName(tableName);

        var sql = "SELECT 1 FROM information_schema.tables WHERE table_name = @TableName";

        await using var cmd = new NpgsqlCommand(sql, pgConnection);
        cmd.Parameters.AddWithValue("TableName", tableName);
        var result = await cmd.ExecuteScalarAsync();
        return result != null;
    }

    public async Task<IReadOnlyList<string>> GetAppliedScriptsAsync(IDbConnection connection, string tableName)
    {
        var pgConnection = ProviderHelpers.Cast<NpgsqlConnection>(connection);
        ProviderHelpers.ValidateTableName(tableName);

        var scripts = new List<string>();
        var sql = $@"SELECT ""Script"" FROM ""{tableName}"" ORDER BY ""Id""";

        await using var cmd = new NpgsqlCommand(sql, pgConnection);
        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            if (!reader.IsDBNull(0))
                scripts.Add(reader.GetString(0));
        }

        return scripts;
    }

    public async Task RecordScriptRunAsync(IDbConnection connection, string tableName, string id, string scriptName, string release, DateTime appliedOnUtc, IDbTransaction? transaction = null)
    {
        var pgConnection = ProviderHelpers.Cast<NpgsqlConnection>(connection);
        ProviderHelpers.ValidateTableName(tableName);

        var sql = $@"
INSERT INTO ""{tableName}"" (""Id"", ""Script"", ""Release"", ""AppliedOnUtc"")
VALUES (@Id, @Script, @Release, @AppliedOnUtc)";

        await using var cmd = new NpgsqlCommand(sql, pgConnection);
        cmd.Transaction = transaction as NpgsqlTransaction;
        cmd.Parameters.AddWithValue("Id", id);
        cmd.Parameters.AddWithValue("Script", scriptName);
        cmd.Parameters.AddWithValue("Release", release);
        cmd.Parameters.AddWithValue("AppliedOnUtc", appliedOnUtc);

        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<IReadOnlyList<ScriptExecutionRecord>> GetScriptExecutionHistoryAsync(IDbConnection connection, string tableName)
    {
        var pgConnection = ProviderHelpers.Cast<NpgsqlConnection>(connection);
        ProviderHelpers.ValidateTableName(tableName);

        var records = new List<ScriptExecutionRecord>();
        var sql = $@"SELECT ""Id"", ""Script"", ""Release"", ""AppliedOnUtc"" FROM ""{tableName}"" ORDER BY ""Id""";

        await using var cmd = new NpgsqlCommand(sql, pgConnection);
        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            records.Add(new ScriptExecutionRecord(
                reader["Id"] as string ?? "",
                reader["Script"] as string ?? "",
                reader["Release"] as string ?? "",
                (DateTime)reader["AppliedOnUtc"]
            ));
        }

        return records;
    }

    public async Task ExecuteScriptAsync(IDbConnection connection, string sql, IDbTransaction? transaction = null)
    {
        var pgConnection = ProviderHelpers.Cast<NpgsqlConnection>(connection);

        // PostgreSQL: Npgsql can execute multiple statements in one command.
        await using var cmd = new NpgsqlCommand(sql, pgConnection);
        cmd.Transaction = transaction as NpgsqlTransaction;
        await cmd.ExecuteNonQueryAsync();
    }
}
