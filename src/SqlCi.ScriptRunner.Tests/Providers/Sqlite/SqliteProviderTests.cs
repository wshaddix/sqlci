using Microsoft.Data.Sqlite;
using SqlCi.ScriptRunner.Providers;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace SqlCi.ScriptRunner.Tests.Providers.Sqlite;

/// <summary>
/// Tests for the Sqlite provider using in-memory databases.
/// These do not require Docker or external servers.
/// </summary>
public class SqliteProviderTests
{
    private readonly IDatabaseProvider _provider = new SqliteProvider();

    [Test]
    public async Task EnsureTrackingTableExistsAsync_CreatesTable()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        const string tableName = "Test_ScriptTable";

        await _provider.EnsureTrackingTableExistsAsync(connection, tableName);

        using var cmd = connection.CreateCommand();
        cmd.CommandText = $"SELECT name FROM sqlite_master WHERE type='table' AND name='{tableName}'";
        var exists = await cmd.ExecuteScalarAsync() as string;

        await Assert.That(exists).IsEqualTo(tableName);
    }

    [Test]
    public async Task TrackingTableExistsAsync_ReturnsCorrectResult()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        const string tableName = "Test_TrackingExists";

        // Should return false before creation
        var before = await _provider.TrackingTableExistsAsync(connection, tableName);
        await Assert.That(before).IsFalse();

        await _provider.EnsureTrackingTableExistsAsync(connection, tableName);

        // Should return true after creation
        var after = await _provider.TrackingTableExistsAsync(connection, tableName);
        await Assert.That(after).IsTrue();
    }

    [Test]
    public async Task RecordScriptRunAsync_And_GetAppliedScriptsAsync_WorkTogether()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        const string tableName = "Test_AppliedScripts";

        await _provider.EnsureTrackingTableExistsAsync(connection, tableName);

        var now = DateTime.UtcNow;

        await _provider.RecordScriptRunAsync(connection, tableName, "20250528120000001", "001_init.sql", "1.0.0", now);
        await _provider.RecordScriptRunAsync(connection, tableName, "20250528120000002", "002_add_users.sql", "1.0.0", now.AddMinutes(1));

        var applied = await _provider.GetAppliedScriptsAsync(connection, tableName);

        await Assert.That(applied).HasCount().EqualTo(2);
        await Assert.That(applied[0]).IsEqualTo("001_init.sql");
        await Assert.That(applied[1]).IsEqualTo("002_add_users.sql");
    }

    [Test]
    public async Task ExecuteScriptAsync_CanRunMultipleStatements()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        const string sql = @"
            CREATE TABLE TestTable (Id INTEGER PRIMARY KEY, Name TEXT);
            INSERT INTO TestTable (Name) VALUES ('First');
            INSERT INTO TestTable (Name) VALUES ('Second');
        ";

        await _provider.ExecuteScriptAsync(connection, sql);

        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM TestTable";
        var count = Convert.ToInt32(await cmd.ExecuteScalarAsync());

        await Assert.That(count).IsEqualTo(2);
    }

    [Test]
    public async Task GetScriptExecutionHistoryAsync_ReturnsRecords()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        const string tableName = "Test_History";

        await _provider.EnsureTrackingTableExistsAsync(connection, tableName);

        var appliedOn = new DateTime(2025, 5, 28, 12, 0, 0, DateTimeKind.Utc);

        await _provider.RecordScriptRunAsync(connection, tableName, "20250528120000010", "010_baseline.sql", "2.1.0", appliedOn);

        var history = await _provider.GetScriptExecutionHistoryAsync(connection, tableName);

        await Assert.That(history).HasCount().EqualTo(1);
        var record = history[0];
        await Assert.That(record.Id).IsEqualTo("20250528120000010");
        await Assert.That(record.Script).IsEqualTo("010_baseline.sql");
        await Assert.That(record.Release).IsEqualTo("2.1.0");
        await Assert.That(record.AppliedOnUtc).IsEqualTo(appliedOn);
    }
}
