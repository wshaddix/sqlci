using Microsoft.Data.Sqlite;
using SqlCi.ScriptRunner;
using SqlCi.ScriptRunner.Providers;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Assertions.AssertConditions.Throws;
using TUnit.Core;

namespace SqlCi.ScriptRunner.Tests;

/// <summary>
/// End-to-end tests that drive <see cref="Executor"/> against a real (file-based) SQLite
/// database. These require no Docker and exercise the full deploy + tracking + transaction flow.
/// </summary>
public class ExecutorIntegrationTests
{
    private static (Configuration config, string scriptsFolder, string dbPath, string connectionString) CreateScenario()
    {
        var root = Path.Combine(Path.GetTempPath(), "sqlci-tests", Guid.NewGuid().ToString("N"));
        var scriptsFolder = Path.Combine(root, "Scripts");
        var resetFolder = Path.Combine(root, "ResetScripts");
        Directory.CreateDirectory(scriptsFolder);
        Directory.CreateDirectory(resetFolder);

        var dbPath = Path.Combine(root, "test.db");
        var connectionString = $"Data Source={dbPath}";

        var config = new Configuration
        {
            ScriptTable = "ScriptTable",
            Version = "1.0.0",
            ScriptsFolder = scriptsFolder,
            ResetScriptsFolder = resetFolder,
            Environments =
            [
                new EnvironmentConfiguration
                {
                    Name = "local",
                    ConnectionString = connectionString,
                    DbProvider = "Sqlite",
                    ResetDatabase = false
                }
            ]
        };

        return (config, scriptsFolder, dbPath, connectionString);
    }

    private static void WriteScript(string folder, string fileName, string sql)
        => File.WriteAllText(Path.Combine(folder, fileName), sql);

    private static async Task<int> CountRowsAsync(string connectionString, string sql)
    {
        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync();
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        return Convert.ToInt32(await cmd.ExecuteScalarAsync());
    }

    private static void Cleanup(string dbPath)
    {
        try
        {
            SqliteConnection.ClearAllPools();
            var dir = Path.GetDirectoryName(Path.GetDirectoryName(dbPath));
            if (dir is not null && Directory.Exists(dir))
                Directory.Delete(dir, recursive: true);
        }
        catch
        {
            // best-effort cleanup
        }
    }

    [Test]
    public async Task ExecuteAsync_HappyPath_AppliesAllScriptsAndRecordsThem()
    {
        var (config, scriptsFolder, dbPath, connectionString) = CreateScenario();

        try
        {
            WriteScript(scriptsFolder, "001_all_create_a.sql", "CREATE TABLE TableA (Id INTEGER PRIMARY KEY);");
            WriteScript(scriptsFolder, "002_all_create_b.sql", "CREATE TABLE TableB (Id INTEGER PRIMARY KEY);");

            using var executor = new Executor(new SqliteProvider());
            var result = await executor.ExecuteAsync(config, "local");

            await Assert.That(result.WasSuccessful).IsTrue();

            var applied = await CountRowsAsync(connectionString, "SELECT COUNT(*) FROM ScriptTable");
            await Assert.That(applied).IsEqualTo(2);

            var tables = await CountRowsAsync(connectionString,
                "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name IN ('TableA','TableB')");
            await Assert.That(tables).IsEqualTo(2);
        }
        finally
        {
            Cleanup(dbPath);
        }
    }

    [Test]
    public async Task ExecuteAsync_RunTwice_SkipsAlreadyAppliedScripts()
    {
        var (config, scriptsFolder, dbPath, connectionString) = CreateScenario();

        try
        {
            // CREATE TABLE without IF NOT EXISTS would fail if the script were applied a second time,
            // so a successful second run proves the script was skipped.
            WriteScript(scriptsFolder, "001_all_create_a.sql", "CREATE TABLE TableA (Id INTEGER PRIMARY KEY);");

            using (var executor1 = new Executor(new SqliteProvider()))
            {
                await executor1.ExecuteAsync(config, "local");
            }

            using (var executor2 = new Executor(new SqliteProvider()))
            {
                var result = await executor2.ExecuteAsync(config, "local");
                await Assert.That(result.WasSuccessful).IsTrue();
            }

            var applied = await CountRowsAsync(connectionString, "SELECT COUNT(*) FROM ScriptTable");
            await Assert.That(applied).IsEqualTo(1);
        }
        finally
        {
            Cleanup(dbPath);
        }
    }

    [Test]
    public async Task ExecuteAsync_NewScriptAddedLater_AppliesOnlyTheNewScript()
    {
        var (config, scriptsFolder, dbPath, connectionString) = CreateScenario();

        try
        {
            WriteScript(scriptsFolder, "001_all_create_a.sql", "CREATE TABLE TableA (Id INTEGER PRIMARY KEY);");

            using (var executor1 = new Executor(new SqliteProvider()))
            {
                await executor1.ExecuteAsync(config, "local");
            }

            WriteScript(scriptsFolder, "002_all_create_b.sql", "CREATE TABLE TableB (Id INTEGER PRIMARY KEY);");

            using (var executor2 = new Executor(new SqliteProvider()))
            {
                await executor2.ExecuteAsync(config, "local");
            }

            var applied = await CountRowsAsync(connectionString, "SELECT COUNT(*) FROM ScriptTable");
            await Assert.That(applied).IsEqualTo(2);
        }
        finally
        {
            Cleanup(dbPath);
        }
    }

    [Test]
    public async Task ExecuteAsync_FailingScript_RollsBackAndDoesNotRecordIt()
    {
        var (config, scriptsFolder, dbPath, connectionString) = CreateScenario();

        try
        {
            WriteScript(scriptsFolder, "001_all_good.sql", "CREATE TABLE GoodTable (Id INTEGER PRIMARY KEY);");
            // First statement succeeds, second is invalid -> whole script transaction must roll back.
            WriteScript(scriptsFolder, "002_all_bad.sql",
                "CREATE TABLE PartialTable (Id INTEGER PRIMARY KEY); THIS IS NOT VALID SQL;");

            using var executor = new Executor(new SqliteProvider());

            await Assert.That(async () => { await executor.ExecuteAsync(config, "local"); })
                .Throws<SqliteException>();

            // The good script committed and was recorded.
            var applied = await CountRowsAsync(connectionString, "SELECT COUNT(*) FROM ScriptTable");
            await Assert.That(applied).IsEqualTo(1);

            // The failing script's partial effects were rolled back.
            var partial = await CountRowsAsync(connectionString,
                "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='PartialTable'");
            await Assert.That(partial).IsEqualTo(0);

            // And it was not recorded as applied.
            var badRecorded = await CountRowsAsync(connectionString,
                "SELECT COUNT(*) FROM ScriptTable WHERE Script = '002_all_bad.sql'");
            await Assert.That(badRecorded).IsEqualTo(0);
        }
        finally
        {
            Cleanup(dbPath);
        }
    }
}
