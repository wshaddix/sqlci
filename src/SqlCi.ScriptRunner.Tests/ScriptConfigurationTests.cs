using SqlCi.ScriptRunner;
using SqlCi.ScriptRunner.Exceptions;
using System.IO;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
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

        try
        {
            config.Verify();
            Assert.Fail("Expected ConfigurationException");
        }
        catch (ConfigurationException ex)
        {
            await Assert.That(ex.Message).Contains("Script table cannot be blank");
        }
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

        try
        {
            config.Verify();
            Assert.Fail("Expected ConfigurationException");
        }
        catch (ConfigurationException ex)
        {
            await Assert.That(ex.Message).Contains("Release number cannot be blank");
        }
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

        try
        {
            config.Verify();
            Assert.Fail("Expected ConfigurationException");
        }
        catch (ConfigurationException ex)
        {
            await Assert.That(ex.Message).Contains("Scripts folder does not exist");
        }
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

        try
        {
            config.Verify("");
            Assert.Fail("Expected ConfigurationException");
        }
        catch (ConfigurationException ex)
        {
            await Assert.That(ex.Message).Contains("Environment cannot be blank");
        }
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

        try
        {
            config.Verify("production");
            Assert.Fail("Expected ConfigurationException");
        }
        catch (ConfigurationException ex)
        {
            await Assert.That(ex.Message).Contains("does not exist in config.json");
        }
    }
}