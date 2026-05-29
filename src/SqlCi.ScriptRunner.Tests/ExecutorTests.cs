using SqlCi.ScriptRunner;
using SqlCi.ScriptRunner.Exceptions;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace SqlCi.ScriptRunner.Tests;

public class ExecutorTests
{
    [Test]
    public async Task ExecuteAsync_WithNullConfiguration_ThrowsArgumentNullException()
    {
        var executor = new Executor();

        try
        {
            await executor.ExecuteAsync(null!, "local");
            Assert.Fail("Expected ArgumentNullException");
        }
        catch (ArgumentNullException ex)
        {
            await Assert.That(ex.ParamName).IsEqualTo("configuration");
        }
    }

    [Test]
    public async Task ExecuteAsync_WithMissingEnvironment_ThrowsConfigurationException()
    {
        var config = new Configuration
        {
            ScriptTable = "ScriptTable",
            Version = "1.0",
            ScriptsFolder = ".",
            ResetScriptsFolder = "."
        };

        var executor = new Executor();

        try
        {
            await executor.ExecuteAsync(config, "");
            Assert.Fail("Expected ConfigurationException");
        }
        catch (ConfigurationException)
        {
            // success
        }
    }
}