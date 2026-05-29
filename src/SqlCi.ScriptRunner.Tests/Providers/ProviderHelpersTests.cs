using System.Data;
using Microsoft.Data.Sqlite;
using SqlCi.ScriptRunner.Providers;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Assertions.AssertConditions.Throws;
using TUnit.Core;

namespace SqlCi.ScriptRunner.Tests.Providers;

public class ProviderHelpersTests
{
    [Test]
    [Arguments("ScriptTable")]
    [Arguments("_private")]
    [Arguments("Schema_Migrations")]
    [Arguments("t1")]
    public async Task IsValidIdentifier_ValidNames_ReturnTrue(string name)
    {
        await Assert.That(ProviderHelpers.IsValidIdentifier(name)).IsTrue();
    }

    [Test]
    [Arguments("")]
    [Arguments(" ")]
    [Arguments("1table")]
    [Arguments("my table")]
    [Arguments("table;DROP")]
    [Arguments("table]")]
    [Arguments("table--")]
    public async Task IsValidIdentifier_InvalidNames_ReturnFalse(string name)
    {
        await Assert.That(ProviderHelpers.IsValidIdentifier(name)).IsFalse();
    }

    [Test]
    public async Task ValidateTableName_InvalidName_ThrowsArgumentException()
    {
        await Assert.That(() => ProviderHelpers.ValidateTableName("bad name;"))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task Provider_RejectsMaliciousTableName()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var provider = new SqliteProvider();

        await Assert.That(async () => { await provider.EnsureTrackingTableExistsAsync(connection, "Foo]; DROP TABLE Bar; --"); })
            .Throws<ArgumentException>();
    }
}
