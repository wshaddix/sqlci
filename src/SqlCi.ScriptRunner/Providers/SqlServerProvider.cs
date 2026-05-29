using Microsoft.Data.SqlClient;
using System.Data;
using System.Text.RegularExpressions;

namespace SqlCi.ScriptRunner.Providers;

public class SqlServerProvider : IDatabaseProvider
{
    public IDbConnection CreateConnection(string connectionString)
        => new SqlConnection(connectionString);

    public async Task EnsureTrackingTableExistsAsync(IDbConnection connection, string tableName)
    {
        if (connection is not SqlConnection sqlConnection)
            throw new ArgumentException("Connection must be a SqlConnection for SqlServerProvider.");

        var sql = $@"
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '{tableName}')
BEGIN
    CREATE TABLE [{tableName}] (
        Id NVARCHAR(50) NOT NULL CONSTRAINT PK_{tableName} PRIMARY KEY CLUSTERED,
        Script NVARCHAR(255) NOT NULL,
        Release NVARCHAR(25) NOT NULL,
        AppliedOnUtc DATETIME NOT NULL
    );
END";

        await using var cmd = new SqlCommand(sql, sqlConnection);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<bool> TrackingTableExistsAsync(IDbConnection connection, string tableName)
    {
        if (connection is not SqlConnection sqlConnection)
            throw new ArgumentException("Connection must be a SqlConnection.");

        var sql = $@"SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '{tableName}'";

        await using var cmd = new SqlCommand(sql, sqlConnection);
        var result = await cmd.ExecuteScalarAsync();
        return result != null;
    }

    public async Task<IReadOnlyList<string>> GetAppliedScriptsAsync(IDbConnection connection, string tableName)
    {
        if (connection is not SqlConnection sqlConnection)
            throw new ArgumentException("Connection must be a SqlConnection.");

        var scripts = new List<string>();
        var sql = $"SELECT Script FROM [{tableName}] ORDER BY Id";

        await using var cmd = new SqlCommand(sql, sqlConnection);
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
        if (connection is not SqlConnection sqlConnection)
            throw new ArgumentException("Connection must be a SqlConnection.");

        var sql = $@"
INSERT INTO [{tableName}] (Id, Script, Release, AppliedOnUtc)
VALUES (@Id, @Script, @Release, @AppliedOnUtc)";

        await using var cmd = new SqlCommand(sql, sqlConnection);
        cmd.Parameters.AddWithValue("@Id", id);
        cmd.Parameters.AddWithValue("@Script", scriptName);
        cmd.Parameters.AddWithValue("@Release", release);
        cmd.Parameters.AddWithValue("@AppliedOnUtc", appliedOnUtc);

        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<IReadOnlyList<ScriptExecutionRecord>> GetScriptExecutionHistoryAsync(IDbConnection connection, string tableName)
    {
        if (connection is not SqlConnection sqlConnection)
            throw new ArgumentException("Connection must be a SqlConnection.");

        var records = new List<ScriptExecutionRecord>();
        var sql = $"SELECT Id, Script, Release, AppliedOnUtc FROM [{tableName}] ORDER BY Id";

        await using var cmd = new SqlCommand(sql, sqlConnection);
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
        if (connection is not SqlConnection sqlConnection)
            throw new ArgumentException("Connection must be a SqlConnection.");

        // SQL Server specific: split on GO (same behavior as before)
        var regex = new Regex(@"\s*GO\s*$", RegexOptions.IgnoreCase | RegexOptions.Multiline);
        var batches = regex.Split(sql);

        foreach (var batch in batches)
        {
            var trimmed = batch.Trim();
            if (string.IsNullOrWhiteSpace(trimmed))
                continue;

            await using var cmd = new SqlCommand(trimmed, sqlConnection);
            await cmd.ExecuteNonQueryAsync();
        }
    }
}
