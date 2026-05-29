using Npgsql;
using SqlCi.ScriptRunner.Providers;
using SqlCi.ScriptRunner.Tests.Fixtures;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace SqlCi.ScriptRunner.Tests.Providers.PostgreSql;

/// <summary>
/// Integration tests for <see cref="PostgreSqlProvider"/> running against a real PostgreSQL
/// instance provided by Testcontainers (when Docker Desktop is running).
/// </summary>
[ClassDataSource<PostgreSqlContainerFixture>(Shared = SharedType.Keyed, Key = "postgres")]
public class PostgreSqlProviderTests(PostgreSqlContainerFixture fixture)
{
    private IDatabaseProvider Provider => new PostgreSqlProvider();

    [Test]
    public async Task EnsureTrackingTableExistsAsync_CreatesTable()
    {
        await fixture.EnsureStartedAsync();

        if (!fixture.IsAvailable || string.IsNullOrWhiteSpace(fixture.ConnectionString))
        {
            Skip.Test("PostgreSQL container not available (Docker Desktop not running or container failed to start)");
            return;
        }

        await using var connection = new NpgsqlConnection(fixture.ConnectionString!);
        await connection.OpenAsync();

        const string tableName = "Postgres_ScriptTable_Test";

        await Provider.EnsureTrackingTableExistsAsync(connection, tableName);

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = $"SELECT 1 FROM information_schema.tables WHERE table_name = '{tableName}'";
        var exists = await cmd.ExecuteScalarAsync();

        await Assert.That(exists).IsNotNull();
    }

    [Test]
    public async Task TrackingTableExistsAsync_ReturnsCorrectResult()
    {
        await fixture.EnsureStartedAsync();

        if (!fixture.IsAvailable || string.IsNullOrWhiteSpace(fixture.ConnectionString))
        {
            Skip.Test("PostgreSQL container not available (Docker Desktop not running or container failed to start)");
            return;
        }

        await using var connection = new NpgsqlConnection(fixture.ConnectionString!);
        await connection.OpenAsync();

        const string tableName = "Postgres_TrackingExists_Test";

        // Should return false before creation
        var before = await Provider.TrackingTableExistsAsync(connection, tableName);
        await Assert.That(before).IsFalse();

        await Provider.EnsureTrackingTableExistsAsync(connection, tableName);

        // Should return true after creation
        var after = await Provider.TrackingTableExistsAsync(connection, tableName);
        await Assert.That(after).IsTrue();
    }

    [Test]
    public async Task RecordScriptRunAsync_And_GetAppliedScriptsAsync_WorkTogether()
    {
        await fixture.EnsureStartedAsync();

        if (!fixture.IsAvailable || string.IsNullOrWhiteSpace(fixture.ConnectionString))
        {
            Skip.Test("PostgreSQL container not available (Docker Desktop not running or container failed to start)");
            return;
        }

        await using var connection = new NpgsqlConnection(fixture.ConnectionString!);
        await connection.OpenAsync();

        const string tableName = "Postgres_AppliedScripts_Test";

        await Provider.EnsureTrackingTableExistsAsync(connection, tableName);

        var now = DateTime.UtcNow;

        await Provider.RecordScriptRunAsync(
            connection, tableName,
            "20250528120000001", "001_init.sql", "1.0.0", now);

        await Provider.RecordScriptRunAsync(
            connection, tableName,
            "20250528120000002", "002_add_users.sql", "1.0.0", now.AddMinutes(1));

        var applied = await Provider.GetAppliedScriptsAsync(connection, tableName);

        await Assert.That(applied).HasCount().EqualTo(2);
        await Assert.That(applied[0]).IsEqualTo("001_init.sql");
        await Assert.That(applied[1]).IsEqualTo("002_add_users.sql");
    }

    [Test]
    public async Task ExecuteScriptAsync_CanRunMultipleStatements()
    {
        await fixture.EnsureStartedAsync();

        if (!fixture.IsAvailable || string.IsNullOrWhiteSpace(fixture.ConnectionString))
        {
            Skip.Test("PostgreSQL container not available (Docker Desktop not running or container failed to start)");
            return;
        }

        await using var connection = new NpgsqlConnection(fixture.ConnectionString!);
        await connection.OpenAsync();

        const string sql = @"
            CREATE TEMP TABLE TempTest (Id INT PRIMARY KEY, Name TEXT);
            INSERT INTO TempTest (Id, Name) VALUES (1, 'First');
            INSERT INTO TempTest (Id, Name) VALUES (2, 'Second');
        ";

        await Provider.ExecuteScriptAsync(connection, sql);

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM TempTest";
        var count = Convert.ToInt32(await cmd.ExecuteScalarAsync());

        await Assert.That(count).IsEqualTo(2);
    }

    [Test]
    public async Task GetScriptExecutionHistoryAsync_ReturnsRecords()
    {
        await fixture.EnsureStartedAsync();

        if (!fixture.IsAvailable || string.IsNullOrWhiteSpace(fixture.ConnectionString))
        {
            Skip.Test("PostgreSQL container not available (Docker Desktop not running or container failed to start)");
            return;
        }

        await using var connection = new NpgsqlConnection(fixture.ConnectionString!);
        await connection.OpenAsync();

        const string tableName = "Postgres_History_Test";

        await Provider.EnsureTrackingTableExistsAsync(connection, tableName);

        var appliedOn = new DateTime(2025, 5, 28, 12, 0, 0, DateTimeKind.Utc);

        await Provider.RecordScriptRunAsync(connection, tableName, "20250528120000010", "010_baseline.sql", "2.1.0", appliedOn);

        var history = await Provider.GetScriptExecutionHistoryAsync(connection, tableName);

        await Assert.That(history).HasCount().EqualTo(1);
        var record = history[0];
        await Assert.That(record.Id).IsEqualTo("20250528120000010");
        await Assert.That(record.Script).IsEqualTo("010_baseline.sql");
        await Assert.That(record.Release).IsEqualTo("2.1.0");
        await Assert.That(record.AppliedOnUtc).IsEqualTo(appliedOn);
    }
}
