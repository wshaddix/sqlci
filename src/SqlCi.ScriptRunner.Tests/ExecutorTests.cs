using SqlCi.ScriptRunner;
using SqlCi.ScriptRunner.Exceptions;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Assertions.AssertConditions.Throws;
using TUnit.Core;

namespace SqlCi.ScriptRunner.Tests;

public class ExecutorTests
{
    [Test]
    public async Task ExecuteAsync_WithNullConfiguration_ThrowsArgumentNullException()
    {
        var executor = new Executor();

        await Assert.That(async () => { await executor.ExecuteAsync(null!, "local"); })
            .Throws<ArgumentNullException>()
            .WithParameterName("configuration");
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

        await Assert.That(async () => { await executor.ExecuteAsync(config, ""); })
            .Throws<ConfigurationException>();
    }
}
