using SqlCi.ScriptRunner.Providers;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace SqlCi.ScriptRunner.Tests.Providers;

public class DatabaseProviderFactoryTests
{
    [Test]
    [Arguments("SqlServer")]
    [Arguments("sqlserver")]
    [Arguments("mssql")]
    public async Task Create_SqlServer_Variants_ReturnsSqlServerProvider(string name)
    {
        var provider = DatabaseProviderFactory.Create(name);
        await Assert.That(provider).IsTypeOf<SqlServerProvider>();
    }

    [Test]
    [Arguments("PostgreSql")]
    [Arguments("postgres")]
    [Arguments("pgsql")]
    public async Task Create_PostgreSql_Variants_ReturnsPostgreSqlProvider(string name)
    {
        var provider = DatabaseProviderFactory.Create(name);
        await Assert.That(provider).IsTypeOf<PostgreSqlProvider>();
    }

    [Test]
    [Arguments("Sqlite")]
    [Arguments("sqlite")]
    public async Task Create_Sqlite_Variants_ReturnsSqliteProvider(string name)
    {
        var provider = DatabaseProviderFactory.Create(name);
        await Assert.That(provider).IsTypeOf<SqliteProvider>();
    }

    [Test]
    public void Create_InvalidProvider_ThrowsNotSupportedException()
    {
        Assert.Throws<NotSupportedException>(() => DatabaseProviderFactory.Create("Oracle"));
    }
}
