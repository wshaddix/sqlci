using Npgsql;
using System.Data;

namespace SqlCi.ScriptRunner.Providers;

public class PostgreSqlProvider : IDatabaseProvider
{
    public IDbConnection CreateConnection(string connectionString)
        => new NpgsqlConnection(connectionString);

    public async Task EnsureTrackingTableExistsAsync(IDbConnection connection, string tableName)
    {
        if (connection is not NpgsqlConnection pgConnection)
            throw new ArgumentException("Connection must be a NpgsqlConnection for PostgreSqlProvider.");

        // PostgreSQL uses information_schema and is generally case-sensitive with unquoted identifiers.
        // We use lowercase for the table name by convention unless quoted.
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
        if (connection is not NpgsqlConnection pgConnection)
            throw new ArgumentException("Connection must be a NpgsqlConnection.");

        // Postgres information_schema is reliable here
        var sql = $@"SELECT 1 FROM information_schema.tables WHERE table_name = '{tableName}'";

        await using var cmd = new NpgsqlCommand(sql, pgConnection);
        var result = await cmd.ExecuteScalarAsync();
        return result != null;
    }

    public async Task<IReadOnlyList<string>> GetAppliedScriptsAsync(IDbConnection connection, string tableName)
    {
        if (connection is not NpgsqlConnection pgConnection)
            throw new ArgumentException("Connection must be a NpgsqlConnection.");

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

    public async Task RecordScriptRunAsync(IDbConnection connection, string tableName, string id, string scriptName, string release, DateTime appliedOnUtc)
    {
        if (connection is not NpgsqlConnection pgConnection)
            throw new ArgumentException("Connection must be a NpgsqlConnection.");

        var sql = $@"
INSERT INTO ""{tableName}"" (""Id"", ""Script"", ""Release"", ""AppliedOnUtc"")
VALUES (@Id, @Script, @Release, @AppliedOnUtc)";

        await using var cmd = new NpgsqlCommand(sql, pgConnection);
        cmd.Parameters.AddWithValue("Id", id);
        cmd.Parameters.AddWithValue("Script", scriptName);
        cmd.Parameters.AddWithValue("Release", release);
        cmd.Parameters.AddWithValue("AppliedOnUtc", appliedOnUtc);

        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<IReadOnlyList<ScriptExecutionRecord>> GetScriptExecutionHistoryAsync(IDbConnection connection, string tableName)
    {
        if (connection is not NpgsqlConnection pgConnection)
            throw new ArgumentException("Connection must be a NpgsqlConnection.");

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

    public async Task ExecuteScriptAsync(IDbConnection connection, string sql)
    {
        if (connection is not NpgsqlConnection pgConnection)
            throw new ArgumentException("Connection must be a NpgsqlConnection.");

        // PostgreSQL: Npgsql can execute multiple statements in one command.
        // We simply execute the entire script content.
        await using var cmd = new NpgsqlCommand(sql, pgConnection);
        await cmd.ExecuteNonQueryAsync();
    }
}
