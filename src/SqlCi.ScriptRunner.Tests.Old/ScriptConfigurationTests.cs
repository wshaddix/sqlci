using SqlCi.ScriptRunner.Exceptions;
using Xunit;

namespace SqlCi.ScriptRunner.Tests
{
    public class ScriptConfigurationTests
    {
        [Fact]
        public void MissingConnectionStringThrowsException()
        {
            Assert.Throws<ConfigurationException>(() =>
                {
                    new Configuration()
                        .Verify();
                });
        }

        [Fact]
        public void MissingEnvironmentThrowsException()
        {
            Assert.Throws<MissingEnvironmentException>(() =>
            {
                new Configuration()
                    .WithConnectionString("blah")
                    .WithScriptsFolder(".")
                    .WithReleaseNumber("1.0")
                    .WithScriptTable("ScriptTable")
                    .Verify();
            });
        }

        [Fact]
        public void MissingReleaseNumberThrowsException()
        {
            Assert.Throws<MissingReleaseNumberException>(() =>
            {
                new Configuration()
                    .WithConnectionString("blah")
                    .WithScriptsFolder(".")
                    .Verify();
            });
        }

        [Fact]
        public void MissingResetFolderThrowsException()
        {
            Assert.Throws<MissingResetFolderException>(() =>
            {
                new Configuration()
                    .WithConnectionString("blah")
                    .WithScriptsFolder(".")
                    .WithResetDatabase(true)
                    .Verify();
            });
        }

        [Fact]
        public void MissingScriptsFolderThrowsException()
        {
            Assert.Throws<MissingScriptsFolderException>(() =>
            {
                new Configuration()
                    .WithConnectionString("blah")
                    .Verify();
            });
        }

        [Fact]
        public void MissingScriptTableThrowsException()
        {
            Assert.Throws<MissingScriptTableException>(() =>
            {
                new Configuration()
                    .WithConnectionString("blah")
                    .WithScriptsFolder(".")
                    .WithReleaseNumber("1.0")
                    .Verify();
            });
        }

        [Fact]
        public void NonExistantResetScriptFolderThrowsException()
        {
            Assert.Throws<ResetFolderDoesNotExistException>(() =>
            {
                new Configuration()
                    .WithConnectionString("blah")
                    .WithScriptsFolder(".")
                    .WithReleaseNumber("1.0")
                    .WithResetDatabase(true)
                    .WithResetFolder("blah")
                    .WithScriptTable("ScriptsTable")
                    .Verify();
            });
        }

        [Fact]
        public void NonExistantScriptFolderThrowsException()
        {
            Assert.Throws<ScriptsFolderDoesNotExistException>(() =>
            {
                new Configuration()
                    .WithConnectionString("blah")
                    .WithScriptsFolder("blah")
                    .WithReleaseNumber("1.0")
                    .Verify();
            });
        }
    }
}