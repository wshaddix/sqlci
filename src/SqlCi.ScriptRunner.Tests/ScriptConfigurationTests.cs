using SqlCi.ScriptRunner;
using SqlCi.ScriptRunner.Exceptions;
using System.IO;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Assertions.AssertConditions.Throws;
using TUnit.Core;

namespace SqlCi.ScriptRunner.Tests;

public class ScriptConfigurationTests
{
    [Test]
    public async Task Verify_WithMissingScriptTable_ThrowsConfigurationException()
    {
        var config = new Configuration
        {
            Version = "1.0",
            ScriptsFolder = ".",
            ResetScriptsFolder = ".",
            ScriptTable = "" // missing
        };

        await Assert.That(() => config.Verify())
            .Throws<ConfigurationException>()
            .WithMessageContaining("Script table cannot be blank");
    }

    [Test]
    public async Task Verify_WithInvalidScriptTableName_ThrowsConfigurationException()
    {
        var config = new Configuration
        {
            ScriptTable = "bad table; DROP",
            Version = "1.0",
            ScriptsFolder = ".",
            ResetScriptsFolder = "."
        };

        await Assert.That(() => config.Verify())
            .Throws<ConfigurationException>()
            .WithMessageContaining("Script table must start with");
    }

    [Test]
    public async Task Verify_WithMissingVersion_ThrowsConfigurationException()
    {
        var config = new Configuration
        {
            ScriptTable = "ScriptTable",
            ScriptsFolder = ".",
            ResetScriptsFolder = ".",
            Version = "" // missing
        };

        await Assert.That(() => config.Verify())
            .Throws<ConfigurationException>()
            .WithMessageContaining("Release number cannot be blank");
    }

    [Test]
    public async Task Verify_WithNonExistentScriptsFolder_ThrowsConfigurationException()
    {
        var config = new Configuration
        {
            ScriptTable = "ScriptTable",
            Version = "1.0",
            ScriptsFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")),
            ResetScriptsFolder = "."
        };

        await Assert.That(() => config.Verify())
            .Throws<ConfigurationException>()
            .WithMessageContaining("Scripts folder does not exist");
    }

    [Test]
    public async Task VerifyEnvironment_MissingEnvironmentName_ThrowsConfigurationException()
    {
        var config = new Configuration
        {
            ScriptTable = "ScriptTable",
            Version = "1.0",
            ScriptsFolder = ".",
            ResetScriptsFolder = "."
        };

        await Assert.That(() => config.Verify(""))
            .Throws<ConfigurationException>()
            .WithMessageContaining("Environment cannot be blank");
    }

    [Test]
    public async Task VerifyEnvironment_UnknownEnvironment_ThrowsConfigurationException()
    {
        var config = new Configuration
        {
            ScriptTable = "ScriptTable",
            Version = "1.0",
            ScriptsFolder = ".",
            ResetScriptsFolder = ".",
            Environments = [new EnvironmentConfiguration { Name = "local", ConnectionString = "server=." }]
        };

        await Assert.That(() => config.Verify("production"))
            .Throws<ConfigurationException>()
            .WithMessageContaining("does not exist in config.json");
    }
}
