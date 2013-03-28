using SqlCi.ScriptRunner.Exceptions;
using Xunit;

namespace SqlCi.ScriptRunner.Tests
{
    public class ScriptConfigurationTests
    {
        [Fact]
        public void MissingConnectionStringThrowsException()
        {
            Assert.Throws<MissingConnectionStringException>(() =>
                {
                    new ScriptConfiguration()
                        .Verify();
                });
        }

        [Fact]
        public void MissingScriptsFolderThrowsException()
        {
            Assert.Throws<MissingScriptsFolderException>(() =>
            {
                new ScriptConfiguration()
                    .WithConnectionString("blah")
                    .Verify();
            });
        }

        [Fact]
        public void MissingResetFolderThrowsException()
        {
            Assert.Throws<MissingResetFolderException>(() =>
            {
                new ScriptConfiguration()
                    .WithConnectionString("blah")
                    .WithScriptsFolder(".")
                    .ResetDatabase(true)
                    .Verify();
            });
        }

        [Fact]
        public void MissingReleaseNumberThrowsException()
        {
            Assert.Throws<MissingReleaseNumberException>(() =>
            {
                new ScriptConfiguration()
                    .WithConnectionString("blah")
                    .WithScriptsFolder(".")
                    .Verify();
            });
        }

        [Fact]
        public void MissingScriptTableThrowsException()
        {
            Assert.Throws<MissingScriptTableException>(() =>
            {
                new ScriptConfiguration()
                    .WithConnectionString("blah")
                    .WithScriptsFolder(".")
                    .WithReleaseNumber("1.0")
                    .Verify();
            });
        }

        [Fact]
        public void NonExistantScriptFolderThrowsException()
        {
            Assert.Throws<ScriptsFolderDoesNotExistException>(() =>
            {
                new ScriptConfiguration()
                    .WithConnectionString("blah")
                    .WithScriptsFolder("blah")
                    .WithReleaseNumber("1.0")
                    .Verify();
            });
        }

        [Fact]
        public void NonExistantResetScriptFolderThrowsException()
        {
            Assert.Throws<ResetFolderDoesNotExistException>(() =>
            {
                new ScriptConfiguration()
                    .WithConnectionString("blah")
                    .WithScriptsFolder(".")
                    .WithReleaseNumber("1.0")
                    .ResetDatabase(true)
                    .ResetFolder("blah")
                    .WithScriptTable("ScriptsTable")
                    .Verify();
            });
        }
    }
}
